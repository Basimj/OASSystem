using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.PurchaseReturns.Commands;
using OAS.Application.Purchasing.PurchaseReturns.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/returns")]
public sealed class PurchaseReturnsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseReturnDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] PurchaseReturnStatus? status,
        [FromQuery] Guid? purchaseReceiptId,
        [FromQuery] Guid? purchaseInvoiceId,
        [FromQuery] Guid? supplierId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPurchaseReturnsQuery(request, status, purchaseReceiptId, purchaseInvoiceId, supplierId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseReturnDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPurchaseReturnByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PurchaseReturnDto>> Create([FromBody] CreatePurchaseReturnRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreatePurchaseReturnCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, await sender.Send(new GetPurchaseReturnByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<PurchaseReturnDto>> Confirm(Guid id, [FromBody] PurchaseReturnActionRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ConfirmPurchaseReturnCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<PurchaseReturnPostingResultDto>> Post(Guid id, [FromBody] PurchaseReturnActionRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PostPurchaseReturnCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseReturnDto>> Cancel(Guid id, [FromBody] CancelPurchaseReturnRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelPurchaseReturnCommand(id, request), cancellationToken));
}
