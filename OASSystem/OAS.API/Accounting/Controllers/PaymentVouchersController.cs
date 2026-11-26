using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Commands.SetPaymentVoucherStatus;
using OAS.Application.Accounting.PaymentVouchers.Commands.UpdatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVouchers;
using OAS.Application.Accounting.PaymentVouchers.Commands.ReservePaymentVoucherNumber;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/payment-vouchers")]
public sealed class PaymentVouchersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentVoucherDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetPaymentVouchersQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<PaymentVoucherBalanceDto>> GetBalance(
        [FromQuery] Guid definitionId, [FromQuery] bool isBank, [FromQuery] DateOnly date, CancellationToken cancellationToken)
        => Ok(await sender.Send(new OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherBalanceQuery(definitionId, isBank, date), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentVoucherDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentVoucherByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("number/reserve")]
    public async Task<ActionResult<OAS.Contracts.Accounting.Common.AccountingNumberReservationDto>> ReserveNumber([FromQuery] DateOnly voucherDate, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ReservePaymentVoucherNumberCommand(voucherDate), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PaymentVoucherDto>> Create(
        [FromBody] CreatePaymentVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreatePaymentVoucherCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPaymentVoucherByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PaymentVoucherDto>> Update(
        Guid id,
        [FromBody] UpdatePaymentVoucherRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdatePaymentVoucherCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetPaymentVoucherByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/reverse-preview")]
    public async Task<ActionResult<OAS.Contracts.Accounting.Reports.VoucherReversalPreview>> PreviewReverse(Guid id, [FromBody] OAS.Contracts.Accounting.Reports.ReverseVoucherRequest request, CancellationToken ct)
        => Ok(await sender.Send(new OAS.Application.Accounting.Reports.Queries.GetVoucherReport.PreviewVoucherReversalCommand(false,id,request),ct));

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<Guid>> Reverse(Guid id, [FromBody] OAS.Contracts.Accounting.Reports.ReverseVoucherRequest request, CancellationToken ct)
        => Ok(await sender.Send(new OAS.Application.Accounting.Reports.Queries.GetVoucherReport.ReverseVoucherCommand(false,id,request),ct));

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<PaymentVoucherDto>> SetStatus(
        Guid id,
        [FromBody] SetPaymentVoucherStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetPaymentVoucherStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetPaymentVoucherByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
