using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.PaymentVouchers.Commands.CreatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Commands.SetPaymentVoucherStatus;
using OAS.Application.Accounting.PaymentVouchers.Commands.UpdatePaymentVoucher;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVouchers;
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
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentVouchersQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentVoucherDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentVoucherByIdQuery(id), cancellationToken);
        return Ok(result);
    }

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

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetPaymentVoucherStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetPaymentVoucherStatusCommand(id, request), cancellationToken);
        return NoContent();
    }
}
