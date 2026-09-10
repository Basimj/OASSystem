using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Identity.Roles.Commands.CreateRole;
using OAS.Application.Identity.Roles.Commands.ImportRoles;
using OAS.Application.Identity.Roles.Commands.UpdateRoleDisplayName;
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

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateRoleCommand(request), cancellationToken);
        var roles = await sender.Send(new GetRolesQuery(), cancellationToken);
        var role = roles.First(x => x.Id == id);
        return Ok(role);
    }

    [HttpPut("{id:guid}/display-name")]
    public async Task<ActionResult<RoleDto>> UpdateDisplayName(
        Guid id,
        UpdateRoleDisplayNameRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateRoleDisplayNameCommand(id, request), cancellationToken);
        var roles = await sender.Send(new GetRolesQuery(), cancellationToken);
        var role = roles.FirstOrDefault(x => x.Id == id);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportRolesResultDto>> Import(ImportRolesRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ImportRolesCommand(request), cancellationToken));
}
