using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Inventory.StockCounts.Commands.ApproveStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CancelStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CompleteStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CreateStockCount;
using OAS.Application.Inventory.StockCounts.Commands.PostStockCount;
using OAS.Application.Inventory.StockCounts.Commands.RecordStockCount;
using OAS.Application.Inventory.StockCounts.Commands.StartStockCount;
using OAS.Application.Inventory.StockCounts.Queries.GetStockCountById;
using OAS.Application.Inventory.StockCounts.Queries.GetStockCountLines;
using OAS.Application.Inventory.StockCounts.Queries.GetStockCounts;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using ContractStatus = OAS.Contracts.Enums.Inventory.StockCountStatus;
using DomainStatus = OAS.Domain.Enums.Inventory.StockCountStatus;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/stock-counts")]
public sealed class StockCountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<StockCountDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] Guid? warehouseId,
        [FromQuery] ContractStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetStockCountsQuery(
                request,
                warehouseId,
                status.HasValue ? (DomainStatus)(int)status.Value : null,
                fromDate,
                toDate),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockCountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStockCountByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/lines")]
    public async Task<ActionResult<IReadOnlyList<StockCountLineDto>>> GetLines(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStockCountLinesQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<StockCountDto>> Create(
        [FromBody] CreateStockCountRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateStockCountCommand(request), cancellationToken);
        var dto = await sender.Send(new GetStockCountByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<StockCountDto>> Start(
        Guid id,
        [FromBody] StartStockCountRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        await sender.Send(new StartStockCountCommand(id), cancellationToken);
        return Ok(await sender.Send(new GetStockCountByIdQuery(id), cancellationToken));
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}/count")]
    public async Task<ActionResult<StockCountDto>> RecordCount(
        Guid id,
        Guid lineId,
        [FromBody] RecordStockCountRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RecordStockCountCommand(id, lineId, request), cancellationToken);
        return Ok(await sender.Send(new GetStockCountByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<StockCountDto>> Complete(
        Guid id,
        [FromBody] CompleteStockCountRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        await sender.Send(new CompleteStockCountCommand(id), cancellationToken);
        return Ok(await sender.Send(new GetStockCountByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<StockCountDto>> Approve(
        Guid id,
        [FromBody] ApproveStockCountRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        await sender.Send(new ApproveStockCountCommand(id), cancellationToken);
        return Ok(await sender.Send(new GetStockCountByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<StockCountDto>> Post(
        Guid id,
        [FromBody] PostStockCountRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        await sender.Send(new PostStockCountCommand(id), cancellationToken);
        return Ok(await sender.Send(new GetStockCountByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<StockCountDto>> Cancel(
        Guid id,
        [FromBody] CancelStockCountRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        await sender.Send(new CancelStockCountCommand(id), cancellationToken);
        return Ok(await sender.Send(new GetStockCountByIdQuery(id), cancellationToken));
    }
}
