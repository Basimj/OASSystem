using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OAS.API.Database;
using OAS.Application.Database.Abstractions;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Authentication;

namespace OAS.API.Security;

public interface IIdentityCookieSessionService
{
    Task SignInAsync(HttpContext httpContext, CurrentUserDto user, AuthenticationProperties? properties = null);
}

public sealed class IdentityCookieSessionService(
    IIdentityRepository identityRepository,
    IDatabaseProfileSelection databaseProfileSelection) : IIdentityCookieSessionService
{
    public async Task SignInAsync(HttpContext httpContext, CurrentUserDto user, AuthenticationProperties? properties = null)
    {
        var record = await identityRepository.GetUserAsync(user.Id, false, httpContext.RequestAborted)
            ?? throw new InvalidOperationException("Cannot create an authentication session for a missing user account.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(IdentityClaimTypes.DisplayName, user.DisplayName),
            new(IdentityClaimTypes.IsSuperAdmin, user.IsSuperAdmin ? "true" : "false"),
            new(IdentityClaimTypes.MustChangePassword, user.MustChangePassword ? "true" : "false"),
            new(IdentityClaimTypes.PasswordVersion, IdentityPasswordVersion.Create(record.User.PasswordHash)),
            new(HttpDatabaseProfileSelection.ClaimName, databaseProfileSelection.ProfileKey ?? "Default")
        };
        if (!string.IsNullOrWhiteSpace(user.Email)) claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties ?? new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });
    }
}
