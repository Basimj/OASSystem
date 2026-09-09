using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.API.Security;
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
    IIdentityCookieSessionService sessionService) : ControllerBase
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

        await sessionService.SignInAsync(HttpContext, result.User, new AuthenticationProperties
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
        await sessionService.SignInAsync(HttpContext, user, properties);
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


}
