using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Identity.Users.Commands;
using OAS.Application.Identity.Users.Models;
using OAS.Application.Identity.Users.Queries;
using OAS.Contracts.Common.Errors;
using OAS.Contracts.Identity.Users;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetUsersQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateUserCommand(request), cancellationToken);
        if (result.Succeeded && result.User is not null)
            return Created($"api/identity/users/{result.User.Id}", result.User);

        var error = new ApiError
        {
            Code = result.ErrorCode ?? "identity_create_user_failed",
            Message = "The create-user request could not be completed.",
            Errors = result.Errors
        };

        return result.FailureKind switch
        {
            CreateUserFailureKind.Validation => BadRequest(error with { Status = StatusCodes.Status400BadRequest }),
            CreateUserFailureKind.Conflict => Conflict(error with { Status = StatusCodes.Status409Conflict }),
            CreateUserFailureKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, error with { Status = StatusCodes.Status403Forbidden }),
            _ => BadRequest(error with { Status = StatusCodes.Status400BadRequest })
        };
    }
}
