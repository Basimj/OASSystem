using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Identity.Profile.Queries.GetUserProfileImage;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/profile-images")]
public sealed class ProfileImagesController(ISender sender) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var image = await sender.Send(new GetUserProfileImageQuery(userId), cancellationToken);
        if (image is null) return NotFound();
        Response.Headers["Cache-Control"] = "private,max-age=300";
        return File(image.Content, image.ContentType);
    }
}
