using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.API.Security;
using OAS.Application.Identity.Profile.Commands.ChangeMyPassword;
using OAS.Application.Identity.Profile.Commands.RemoveMyProfileImage;
using OAS.Application.Identity.Profile.Commands.SetMyProfileImage;
using OAS.Application.Identity.Profile.Commands.UpdateMyProfile;
using OAS.Application.Identity.Profile.Queries.GetMyProfile;
using OAS.Contracts.Identity.Authentication;
using OAS.Contracts.Identity.Profile;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/profile")]
public sealed class ProfileController(ISender sender, IIdentityCookieSessionService sessionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MyProfileDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyProfileQuery(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<MyProfileDto>> Update(UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateMyProfileCommand(request), cancellationToken);
        return Ok(await sender.Send(new GetMyProfileQuery(), cancellationToken));
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<CurrentUserDto>> ChangePassword(ChangeMyPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new ChangeMyPasswordCommand(request), cancellationToken);
        var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await sessionService.SignInAsync(HttpContext, user, authentication.Properties);
        return Ok(user);
    }

    [HttpPut("image")]
    [RequestSizeLimit(2_600_000)]
    public async Task<IActionResult> SetImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > 2_500_000) return BadRequest();
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        await sender.Send(new SetMyProfileImageCommand(file.ContentType, memory.ToArray()), cancellationToken);
        return NoContent();
    }

    [HttpDelete("image")]
    public async Task<IActionResult> RemoveImage(CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveMyProfileImageCommand(), cancellationToken);
        return NoContent();
    }
}
