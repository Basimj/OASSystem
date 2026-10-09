using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.PurchaseInvoices.Commands;
using OAS.Application.Purchasing.PurchaseInvoices.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/invoices")]
public sealed class PurchaseInvoicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseInvoiceDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery] PurchaseInvoiceStatus? status,
        [FromQuery] Guid? supplierId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseInvoicesQuery(request, status, supplierId), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PurchaseInvoiceDto>> Create(
        [FromBody] CreatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreatePurchaseInvoiceCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> Update(
        Guid id,
        [FromBody] UpdatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdatePurchaseInvoiceCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/match")]
    public async Task<ActionResult<PurchaseMatchResultDto>> Match(
        Guid id,
        [FromBody] MatchPurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new MatchPurchaseInvoiceCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<PurchaseInvoiceDto>> Confirm(
        Guid id,
        [FromBody] ConfirmPurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmPurchaseInvoiceCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/variances/approve")]
    public async Task<ActionResult<PurchaseInvoiceDto>> ApproveVariance(
        Guid id,
        [FromBody] ApprovePurchaseVarianceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ApprovePurchaseVarianceCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/variances/reject")]
    public async Task<ActionResult<PurchaseInvoiceDto>> RejectVariance(
        Guid id,
        [FromBody] RejectPurchaseVarianceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RejectPurchaseVarianceCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<PurchaseInvoicePostResultDto>> Post(
        Guid id,
        [FromBody] PostPurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new PostPurchaseInvoiceCommand(id, request), cancellationToken));

    [HttpGet("{id:guid}/payment-summary")]
    public async Task<ActionResult<PurchaseInvoicePaymentSummaryDto>> GetPaymentSummary(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPurchaseInvoicePaymentSummaryQuery(id), cancellationToken));

    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<PayPurchaseInvoiceResultDto>> Pay(
        Guid id,
        [FromBody] PayPurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new PayPurchaseInvoiceCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseInvoiceDto>> Cancel(
        Guid id,
        [FromBody] CancelPurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CancelPurchaseInvoiceCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken));
    }
}
