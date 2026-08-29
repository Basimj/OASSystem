using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Identity.Roles.Queries;
using OAS.Contracts.Identity.Roles;

namespace OAS.API.Identity.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/roles")]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetRolesQuery(), cancellationToken));
}
