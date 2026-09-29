using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.PurchaseOrders.Queries;
using OAS.Application.Purchasing.PurchaseRequests.Commands;
using OAS.Application.Purchasing.PurchaseRequests.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseRequests;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/requests")]
public sealed class PurchaseRequestsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseRequestDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] PurchaseRequestStatus? status,
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseRequestsQuery(request, status, warehouseId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseRequestByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PurchaseRequestDto>> Create(
        [FromBody] CreatePurchaseRequestRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreatePurchaseRequestCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPurchaseRequestByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestDto>> Update(
        Guid id,
        [FromBody] UpdatePurchaseRequestRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdatePurchaseRequestCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseRequestByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<PurchaseRequestDto>> Submit(
        Guid id,
        [FromBody] SetPurchaseRequestStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitPurchaseRequestCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseRequestByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<PurchaseRequestDto>> Approve(
        Guid id,
        [FromBody] SetPurchaseRequestStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ApprovePurchaseRequestCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseRequestByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<PurchaseRequestDto>> Reject(
        Guid id,
        [FromBody] SetPurchaseRequestStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RejectPurchaseRequestCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseRequestByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseRequestDto>> Cancel(
        Guid id,
        [FromBody] SetPurchaseRequestStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CancelPurchaseRequestCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseRequestByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/create-purchase-order")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder(
        Guid id,
        [FromBody] CreatePurchaseOrderFromRequestRequest request,
        CancellationToken cancellationToken)
    {
        var purchaseOrderId = await sender.Send(new CreatePurchaseOrderFromRequestCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetPurchaseOrderByIdQuery(purchaseOrderId), cancellationToken);
        return Created($"/api/purchasing/orders/{purchaseOrderId}", dto);
    }
}
