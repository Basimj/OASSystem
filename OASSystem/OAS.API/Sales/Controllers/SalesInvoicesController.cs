using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.PriceOverrides.Commands;
using OAS.Application.Sales.SalesInvoices.Commands;
using OAS.Application.Sales.SalesInvoices.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.PriceOverrides;
using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/invoices")]
public sealed class SalesInvoicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesInvoiceDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSalesInvoicesQuery(request), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSalesInvoiceByIdQuery(id), cancellationToken));

    [HttpPost("code/reserve")]
    public async Task<ActionResult<SalesCodeReservationDto>> ReserveCode(
        [FromQuery] DateOnly invoiceDate,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ReserveSalesInvoiceCodeCommand(invoiceDate), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SalesInvoiceDto>> Create(
        [FromBody] CreateSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateSalesInvoiceCommand(request), cancellationToken);
        var dto = await sender.Send(new GetSalesInvoiceByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPost("from-order/{orderId:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> CreateFromOrder(
        Guid orderId,
        [FromBody] CreateSalesInvoiceFromOrderRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new CreateSalesInvoiceFromOrderCommand(orderId, request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> Update(
        Guid id,
        [FromBody] UpdateSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateSalesInvoiceCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetSalesInvoiceByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<SalesInvoiceDto>> Confirm(
        Guid id,
        [FromBody] ConfirmSalesInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ConfirmSalesInvoiceCommand(id, request), cancellationToken));

    [HttpGet("{id:guid}/posting-prevalidation")]
    public async Task<ActionResult<SalesPostingPreValidationDto>> PreValidatePosting(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new PreValidateSalesInvoicePostingQuery(id), cancellationToken));

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<SalesInvoicePostingResultDto>> Post(
        Guid id,
        [FromBody] PostSalesInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new PostSalesInvoiceCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SalesInvoiceDto>> Cancel(
        Guid id,
        [FromBody] CancelSalesInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new CancelSalesInvoiceCommand(id, request), cancellationToken));

    [HttpGet("{id:guid}/payment-summary")]
    public async Task<ActionResult<SalesInvoicePaymentSummaryDto>> GetPaymentSummary(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSalesInvoicePaymentSummaryQuery(id), cancellationToken));

    [HttpPost("{id:guid}/price-overrides")]
    public async Task<ActionResult<SalesPriceOverrideDto>> RequestPriceOverride(
        Guid id,
        [FromBody] RequestSalesPriceOverrideRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new RequestSalesPriceOverrideCommand(id, request), cancellationToken));

    [HttpPost("price-overrides/{overrideId:guid}/approve")]
    public async Task<ActionResult<SalesPriceOverrideDto>> ApprovePriceOverride(
        Guid overrideId,
        [FromBody] ApproveSalesPriceOverrideRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ApproveSalesPriceOverrideCommand(overrideId, request), cancellationToken));

    [HttpPost("price-overrides/{overrideId:guid}/reject")]
    public async Task<ActionResult<SalesPriceOverrideDto>> RejectPriceOverride(
        Guid overrideId,
        [FromBody] RejectSalesPriceOverrideRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new RejectSalesPriceOverrideCommand(overrideId, request), cancellationToken));

    [HttpPost("price-overrides/{overrideId:guid}/cancel")]
    public async Task<ActionResult<SalesPriceOverrideDto>> CancelPriceOverride(
        Guid overrideId,
        [FromBody] CancelSalesPriceOverrideRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new CancelSalesPriceOverrideCommand(overrideId, request), cancellationToken));
}
