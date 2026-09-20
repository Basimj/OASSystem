using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.CashShifts.Commands.ApproveCashShift;
using OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;
using OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;
using OAS.Application.Accounting.CashShifts.Commands.SetCashShiftStatus;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShiftById;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShifts;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/cash-shifts")]
public sealed class CashShiftsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CashShiftDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCashShiftsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CashShiftDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCashShiftByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CashShiftDto>> Create(
        [FromBody] CreateCashShiftRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateCashShiftCommand(request), cancellationToken);
        var dto = await sender.Send(new GetCashShiftByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<CashShiftDto>> Close(
        Guid id,
        [FromBody] SetCashShiftClosingRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CloseCashShiftCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetCashShiftByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<CashShiftDto>> Approve(
        Guid id,
        [FromQuery] string? rowVersion,
        [FromBody] string? bodyRowVersion,
        CancellationToken cancellationToken)
    {
        var version = !string.IsNullOrWhiteSpace(rowVersion) ? rowVersion : (bodyRowVersion ?? string.Empty);
        await sender.Send(new ApproveCashShiftCommand(id, version), cancellationToken);
        var dto = await sender.Send(new GetCashShiftByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetCashShiftStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetCashShiftStatusCommand(id, request), cancellationToken);
        return NoContent();
    }
}
