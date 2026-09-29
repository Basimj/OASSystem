using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.PurchaseOrders.Commands;
using OAS.Application.Purchasing.PurchaseOrders.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseOrders;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/orders")]
public sealed class PurchaseOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseOrdersQuery(request, status, supplierId, warehouseId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreatePurchaseOrderCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> Update(
        Guid id,
        [FromBody] UpdatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdatePurchaseOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<PurchaseOrderDto>> Submit(Guid id, [FromBody] SubmitPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitPurchaseOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<PurchaseOrderDto>> Approve(Guid id, [FromBody] ApprovePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ApprovePurchaseOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<PurchaseOrderDto>> Reject(Guid id, [FromBody] RejectPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RejectPurchaseOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<PurchaseOrderDto>> Send(Guid id, [FromBody] SendPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SendPurchaseOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(Guid id, [FromBody] CancelPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelPurchaseOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<PurchaseOrderDto>> Close(Guid id, [FromBody] ClosePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ClosePurchaseOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken));
    }
}
