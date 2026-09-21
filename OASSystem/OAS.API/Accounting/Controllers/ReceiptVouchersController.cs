using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.ReceiptVouchers.Commands.CreateReceiptVoucher;
using OAS.Application.Accounting.ReceiptVouchers.Commands.SetReceiptVoucherStatus;
using OAS.Application.Accounting.ReceiptVouchers.Commands.UpdateReceiptVoucher;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVouchers;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/receipt-vouchers")]
public sealed class ReceiptVouchersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReceiptVoucherDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetReceiptVouchersQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceiptVoucherDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetReceiptVoucherByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReceiptVoucherDto>> Create(
        [FromBody] CreateReceiptVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateReceiptVoucherCommand(request), cancellationToken);
        var dto = await sender.Send(new GetReceiptVoucherByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReceiptVoucherDto>> Update(
        Guid id,
        [FromBody] UpdateReceiptVoucherRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateReceiptVoucherCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetReceiptVoucherByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetReceiptVoucherStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetReceiptVoucherStatusCommand(id, request), cancellationToken);
        return NoContent();
    }
}
