using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Identity.Authentication.Commands;
using OAS.Application.Identity.Authentication.Queries;
using OAS.Application.Identity.Authentication.Models;
using OAS.Application.Database.Abstractions;
using OAS.Contracts.Identity.Authentication;
using OAS.Contracts.Common.Errors;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Route("api/identity/auth")]
public sealed class AuthController(ISender sender, IDatabaseProfileSelection databaseProfileSelection) : ControllerBase
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

        var user = result.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("display_name", user.DisplayName),
            new("is_super_admin", user.IsSuperAdmin ? "true" : "false"),
            new(OAS.API.Database.HttpDatabaseProfileSelection.ClaimName, databaseProfileSelection.ProfileKey ?? "Default")
        };
        if (!string.IsNullOrWhiteSpace(user.Email)) claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            AllowRefresh = true,
            IssuedUtc = DateTimeOffset.UtcNow
        };
        if (request.RememberMe) properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<CurrentUserDto>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangePasswordCommand(request), cancellationToken);
        if (result.Succeeded) return Ok(result.User);

        var error = new ApiError
        {
            Code = result.ErrorCode ?? "identity_password_change_failed",
            Message = "The password change request could not be completed.",
            Errors = result.Errors
        };

        return result.FailureKind switch
        {
            ChangePasswordFailureKind.Validation => BadRequest(error with { Status = StatusCodes.Status400BadRequest }),
            ChangePasswordFailureKind.CurrentPasswordInvalid => BadRequest(error with { Status = StatusCodes.Status400BadRequest }),
            ChangePasswordFailureKind.PasswordReused => Conflict(error with { Status = StatusCodes.Status409Conflict }),
            ChangePasswordFailureKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, error with { Status = StatusCodes.Status403Forbidden }),
            _ => BadRequest(error with { Status = StatusCodes.Status400BadRequest })
        };
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
}
