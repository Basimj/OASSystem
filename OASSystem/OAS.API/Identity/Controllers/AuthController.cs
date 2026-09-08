using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.API.Security;
using OAS.Application.Database.Abstractions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Authentication.Commands;
using OAS.Application.Identity.Authentication.Models;
using OAS.Application.Identity.Authentication.Queries;
using OAS.Contracts.Common.Errors;
using OAS.Contracts.Identity.Authentication;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Route("api/identity/auth")]
public sealed class AuthController(
    ISender sender,
    IDatabaseProfileSelection databaseProfileSelection,
    IIdentityRepository identityRepository) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request), cancellationToken);
        if (!result.Succeeded || result.User is null)
        {
            var error = new ApiError
            {
                Code = result.ErrorCode ?? "identity_invalid_credentials",
                Message = "The login request could not be completed.",
                Errors = result.Errors
            };
            return result.FailureKind switch
            {
                LoginFailureKind.Validation => BadRequest(error with { Status = StatusCodes.Status400BadRequest }),
                _ => Unauthorized(error with { Status = StatusCodes.Status401Unauthorized })
            };
        }

        await SignInUserAsync(result.User, new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            AllowRefresh = true,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
        });
        return Ok(result.User);
    }

    [Authorize]
    [HttpPost("complete-password-setup")]
    public async Task<ActionResult<CurrentUserDto>> CompletePasswordSetup(
        CompletePasswordSetupRequest request,
        CancellationToken cancellationToken)
    {
        var user = await sender.Send(new CompletePasswordSetupCommand(request), cancellationToken);
        var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = authentication.Properties ?? new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true,
            IssuedUtc = DateTimeOffset.UtcNow
        };
        await SignInUserAsync(user, properties);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCurrentUserQuery(), cancellationToken));

    private async Task SignInUserAsync(CurrentUserDto user, AuthenticationProperties properties)
    {
        var record = await identityRepository.GetUserAsync(user.Id, false, HttpContext.RequestAborted)
            ?? throw new InvalidOperationException("Cannot create an authentication session for a missing user account.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(IdentityClaimTypes.DisplayName, user.DisplayName),
            new(IdentityClaimTypes.IsSuperAdmin, user.IsSuperAdmin ? "true" : "false"),
            new(IdentityClaimTypes.MustChangePassword, user.MustChangePassword ? "true" : "false"),
            new(IdentityClaimTypes.PasswordVersion, IdentityPasswordVersion.Create(record.User.PasswordHash)),
            new(OAS.API.Database.HttpDatabaseProfileSelection.ClaimName, databaseProfileSelection.ProfileKey ?? "Default")
        };
        if (!string.IsNullOrWhiteSpace(user.Email)) claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }
}
