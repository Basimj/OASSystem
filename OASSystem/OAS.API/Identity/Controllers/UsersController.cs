using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Identity.Users.Commands.CreateUser;
using OAS.Application.Identity.Users.Commands.ResetUserPassword;
using OAS.Application.Identity.Users.Commands.SetUserRoles;
using OAS.Application.Identity.Users.Commands.SetUserProfileImage;
using OAS.Application.Identity.Users.Commands.RemoveUserProfileImage;
using OAS.Application.Identity.Users.Commands.SetUserStatus;
using OAS.Application.Identity.Users.Commands.UnlockUser;
using OAS.Application.Identity.Users.Commands.UpdateUser;
using OAS.Application.Identity.Users.Queries.GetUserById;
using OAS.Application.Identity.Users.Queries.GetUsers;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Users;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] bool? isActive,
        [FromQuery] Guid? roleId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetUsersQuery(request, isActive, roleId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailsDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CreateUserResultDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var outcome = await sender.Send(new CreateUserCommand(request), cancellationToken);
        var user = await sender.Send(new GetUserByIdQuery(outcome.UserId), cancellationToken);
        PreventSensitiveResponseCaching();
        return Created($"api/identity/users/{outcome.UserId}", new CreateUserResultDto(user, outcome.TemporaryPassword));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDetailsDto>> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateUserCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<UserDetailsDto>> SetStatus(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetUserStatusCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<UserDetailsDto>> SetRoles(Guid id, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SetUserRolesCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ResetUserPasswordResultDto>> ResetPassword(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        var temporaryPassword = await sender.Send(new ResetUserPasswordCommand(id, request), cancellationToken);
        var user = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        PreventSensitiveResponseCaching();
        return Ok(new ResetUserPasswordResultDto(temporaryPassword, user.RowVersion));
    }


    [HttpPut("{id:guid}/profile-image")]
    [RequestSizeLimit(2_600_000)]
    public async Task<IActionResult> SetProfileImage(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > 2_500_000) return BadRequest();
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        await sender.Send(new SetUserProfileImageCommand(id, file.ContentType, memory.ToArray()), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/profile-image")]
    public async Task<IActionResult> RemoveProfileImage(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveUserProfileImageCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<ActionResult<UserDetailsDto>> Unlock(Guid id, UnlockUserRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UnlockUserCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetUserByIdQuery(id), cancellationToken));
    }
    private void PreventSensitiveResponseCaching()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
    }
}
