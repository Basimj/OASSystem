using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Inventory.Ledger.Queries.GetInventoryLedger;
using OAS.Application.Inventory.Ledger.Queries.GetInventoryLedgerById;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using ContractMovementType = OAS.Contracts.Enums.Inventory.InventoryMovementType;
using DomainMovementType = OAS.Domain.Enums.Inventory.InventoryMovementType;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/ledger")]
public sealed class InventoryLedgerController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryLedgerDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] Guid? transactionId,
        [FromQuery] ContractMovementType? movementType,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetInventoryLedgerQuery(
                request,
                warehouseId,
                productVariantId,
                transactionId,
                movementType.HasValue ? (DomainMovementType)(int)movementType.Value : null,
                fromDate,
                toDate),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryLedgerDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryLedgerByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}
