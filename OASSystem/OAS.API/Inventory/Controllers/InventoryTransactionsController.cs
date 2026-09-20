using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;
using OAS.Application.Inventory.Transactions.Commands.PostInventoryTransaction;
using OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionById;
using OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionLines;
using OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using ContractStatus = OAS.Contracts.Enums.Inventory.InventoryTransactionStatus;
using ContractType = OAS.Contracts.Enums.Inventory.InventoryTransactionType;
using DomainStatus = OAS.Domain.Enums.Inventory.InventoryTransactionStatus;
using DomainType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/transactions")]
public sealed class InventoryTransactionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryTransactionDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] ContractType? transactionType,
        [FromQuery] ContractStatus? status,
        [FromQuery] Guid? warehouseId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetInventoryTransactionsQuery(
                request,
                transactionType.HasValue ? (DomainType)(int)transactionType.Value : null,
                status.HasValue ? (DomainStatus)(int)status.Value : null,
                warehouseId,
                fromDate,
                toDate),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryTransactionDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryTransactionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/lines")]
    public async Task<ActionResult<IReadOnlyList<InventoryTransactionLineDto>>> GetLines(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryTransactionLinesQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryTransactionDto>> Create(
        [FromBody] CreateInventoryTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateInventoryTransactionCommand(request), cancellationToken);
        var dto = await sender.Send(new GetInventoryTransactionByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<InventoryTransactionDto>> Post(
        Guid id,
        [FromBody] PostInventoryTransactionRequest? request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new PostInventoryTransactionCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetInventoryTransactionByIdQuery(id), cancellationToken));
    }
}
