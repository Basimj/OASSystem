using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceById;
using OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceByWarehouseAndVariant;
using OAS.Application.Inventory.Balances.Queries.GetInventoryBalances;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/balances")]
public sealed class InventoryBalancesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryBalanceDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetInventoryBalancesQuery(request, warehouseId, productVariantId),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryBalanceDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryBalanceByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-warehouse-variant")]
    public async Task<ActionResult<InventoryBalanceDto?>> GetByWarehouseAndVariant(
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid productVariantId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetInventoryBalanceByWarehouseAndVariantQuery(warehouseId, productVariantId),
            cancellationToken);
        return Ok(result);
    }
}
