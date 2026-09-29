using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.PurchaseReceipts.Commands;
using OAS.Application.Purchasing.PurchaseReceipts.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseReceipts;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/receipts")]
public sealed class PurchaseReceiptsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseReceiptDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] PurchaseReceiptStatus? status,
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] Guid? supplierId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseReceiptsQuery(request, status, purchaseOrderId, supplierId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseReceiptDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseReceiptByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PurchaseReceiptDto>> Create(
        [FromBody] CreatePurchaseReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreatePurchaseReceiptCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPurchaseReceiptByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseReceiptDto>> Update(
        Guid id,
        [FromBody] UpdatePurchaseReceiptRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdatePurchaseReceiptCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseReceiptByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<PurchaseReceiptDto>> Confirm(
        Guid id,
        [FromBody] ConfirmPurchaseReceiptRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmPurchaseReceiptCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseReceiptByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<PurchaseReceiptPostResultDto>> Post(
        Guid id,
        [FromBody] PostPurchaseReceiptRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new PostPurchaseReceiptCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseReceiptDto>> Cancel(
        Guid id,
        [FromBody] CancelPurchaseReceiptRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CancelPurchaseReceiptCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseReceiptByIdQuery(id), cancellationToken));
    }
}
