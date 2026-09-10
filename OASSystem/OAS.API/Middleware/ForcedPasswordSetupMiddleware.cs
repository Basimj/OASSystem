using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OAS.API.Security;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Common.Errors;

namespace OAS.API.Middleware;

/// <summary>
/// Revalidates account security state for every authenticated API request.
/// It prevents stale cookies from bypassing account deactivation, role changes,
/// password resets, or the mandatory password-setup state.
/// </summary>
public sealed class ForcedPasswordSetupMiddleware(RequestDelegate next)
{
    private static readonly HashSet<PathString> SessionValidationBypassApiPaths =
    [
        new PathString("/api/identity/auth/login"),
        new PathString("/api/identity/auth/logout")
    ];

    private static readonly HashSet<PathString> PasswordSetupAllowedApiPaths =
    [
        new PathString("/api/identity/auth/me"),
        new PathString("/api/identity/auth/complete-password-setup"),
        new PathString("/api/identity/auth/logout")
    ];

    public async Task InvokeAsync(HttpContext context, IIdentityRepository identityRepository)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        // Database bootstrap must not depend on the current identity schema.
        // A browser can still carry an authenticated cookie while the database is
        // behind the application's EF model. Revalidating that cookie here would
        // query new columns before pending migrations can be applied, which makes
        // the bootstrap status/update endpoints unable to repair the database.
        if (context.Request.Path.StartsWithSegments("/api/database/bootstrap"))
        {
            await next(context);
            return;
        }

        // Login must remain callable even when the browser still carries a stale
        // cookie invalidated by an administrative password reset. Logout must also
        // remain callable so the stale cookie can always be cleared explicitly.
        if (SessionValidationBypassApiPaths.Contains(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await next(context);
            return;
        }

        var record = await identityRepository.GetUserAsync(userId, false, context.RequestAborted);
        if (record is null || !record.User.IsActive)
        {
            await RejectSessionAsync(
                context,
                "identity_account_inactive",
                "The account is no longer active.");
            return;
        }

        var cookiePasswordVersion = context.User.FindFirstValue(IdentityClaimTypes.PasswordVersion);
        var currentPasswordVersion = IdentityPasswordVersion.Create(record.User.PasswordHash);
        if (string.IsNullOrWhiteSpace(cookiePasswordVersion) ||
            !string.Equals(cookiePasswordVersion, currentPasswordVersion, StringComparison.Ordinal))
        {
            await RejectSessionAsync(
                context,
                "identity_session_expired",
                "The authentication session is no longer valid. Sign in again.");
            return;
        }

        RefreshSecurityClaims(context.User, record);

        if (record.User.MustChangePassword && !PasswordSetupAllowedApiPaths.Contains(context.Request.Path))
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status403Forbidden,
                "identity_password_setup_required",
                "Password setup must be completed before using the system.");
            return;
        }

        await next(context);
    }

    private static void RefreshSecurityClaims(ClaimsPrincipal principal, IdentityUserRecord record)
    {
        var identity = principal.Identities.FirstOrDefault(x => x.IsAuthenticated);
        if (identity is null)
            return;

        ReplaceClaims(identity, ClaimTypes.Name, [record.User.UserName]);
        ReplaceClaims(identity, ClaimTypes.Email, string.IsNullOrWhiteSpace(record.User.Email) ? [] : [record.User.Email]);
        ReplaceClaims(identity, ClaimTypes.Role, record.Roles.Select(role => role.Name));
        ReplaceClaims(identity, IdentityClaimTypes.IsSuperAdmin, [record.User.IsSuperAdmin ? "true" : "false"]);
        ReplaceClaims(identity, IdentityClaimTypes.MustChangePassword, [record.User.MustChangePassword ? "true" : "false"]);
        ReplaceClaims(identity, IdentityClaimTypes.DisplayName, [record.User.DisplayName]);
    }

    private static void ReplaceClaims(ClaimsIdentity identity, string claimType, IEnumerable<string> values)
    {
        foreach (var existing in identity.FindAll(claimType).ToArray())
            identity.RemoveClaim(existing);

        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase))
            identity.AddClaim(new Claim(claimType, value));
    }

    private static async Task RejectSessionAsync(HttpContext context, string code, string message)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, code, message);
    }

    private static Task WriteErrorAsync(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new ApiError
        {
            Status = status,
            Code = code,
            Message = message
        }, context.RequestAborted);
    }
}
