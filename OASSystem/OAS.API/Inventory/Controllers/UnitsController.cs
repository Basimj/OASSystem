using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Products;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/units")]
public sealed class UnitsController(
    ICrudApplicationService<Guid, UnitDto, CreateUnitRequest, UpdateUnitRequest> service,
    IPermissionChecker permissionChecker) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.Units.View, cancellationToken);
        return Ok(await service.GetPageAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.Units.View, cancellationToken);
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create(
        [FromBody] CreateUnitRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.Units.Create, cancellationToken);
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitDto>> Update(
        Guid id,
        [FromBody] UpdateUnitRequest request,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(InventoryPermissions.Units.Edit, cancellationToken);
        return Ok(await service.UpdateAsync(id, request, cancellationToken));
    }

    private async Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken)
    {
        if (!await permissionChecker.HasPermissionAsync(permission, cancellationToken))
            throw new ForbiddenException();
    }
}
