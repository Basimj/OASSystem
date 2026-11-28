using MediatR;
using OAS.Application.Accounting.CashShifts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.CashShifts.Commands.ApproveCashShift;
using OAS.Application.Accounting.CashShifts.Commands.CloseCashShift;
using OAS.Application.Accounting.CashShifts.Commands.CreateCashShift;
using OAS.Application.Accounting.CashShifts.Commands.SettleCashShift;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShiftById;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShiftMovements;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShifts;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShiftOpeningBalance;
using OAS.Application.Accounting.CashShifts.Commands.ReserveCashShiftNumber;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/cash-shifts")]
public sealed class CashShiftsController(ISender sender) : ControllerBase
{
    [HttpGet("box/{boxId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CashShiftDto>>> GetBoxShifts(Guid boxId, CancellationToken ct)
        => Ok(await sender.Send(new GetCashBoxShiftsQuery(boxId), ct));

    [HttpPost("box/open")]
    public async Task<ActionResult<Guid>> OpenBox(OpenCashBoxShiftRequest request, CancellationToken ct)
        => Ok(await sender.Send(new OpenCashBoxShiftCommand(request), ct));

    [HttpPost("box/{id:guid}/close")]
    public async Task<ActionResult<bool>> CloseBox(Guid id, CloseCashBoxShiftRequest request, CancellationToken ct)
    { await sender.Send(new CloseCashBoxShiftCommand(id, request), ct); return Ok(true); }

    [HttpPost("box/{id:guid}/approve")]
    public async Task<ActionResult<bool>> ApproveBox(Guid id, ApproveCashBoxShiftRequest request, CancellationToken ct)
    { await sender.Send(new ApproveCashBoxShiftCommand(id, request), ct); return Ok(true); }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CashShiftDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
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

    [HttpGet("{id:guid}/closing-preview")]
    public async Task<ActionResult<decimal>> ClosingPreview(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetCashShiftClosingPreviewQuery(id), ct));

    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<CashShiftMovementsDto>> GetMovements(Guid id, [FromQuery] int page = 1, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetCashShiftMovementsQuery(id, page), cancellationToken));

    [HttpGet("opening-balance/{cashAccountId:guid}")]
    public async Task<ActionResult<CashShiftOpeningBalanceDto>> GetOpeningBalance(Guid cashAccountId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCashShiftOpeningBalanceQuery(cashAccountId), cancellationToken));

    [HttpPost("number/reserve")]
    public async Task<ActionResult<OAS.Contracts.Accounting.Common.AccountingNumberReservationDto>> ReserveNumber(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ReserveCashShiftNumberCommand(), cancellationToken));

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

    [HttpGet("{id:guid}/settlement-accounts")]
    public async Task<ActionResult<IReadOnlyList<CashShiftSettlementAccountDto>>> SettlementAccounts(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetCashShiftSettlementAccountsQuery(id), ct));

    [HttpPost("{id:guid}/settle")]
    public async Task<ActionResult<CashShiftDto>> Settle(Guid id, CashShiftSettlementRequest request, CancellationToken ct)
    {
        await sender.Send(new SettleCashShiftCommand(id, request), ct);
        return Ok(await sender.Send(new GetCashShiftByIdQuery(id), ct));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<CashShiftDto>> Approve(
        Guid id,
        [FromQuery] string rowVersion,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveCashShiftCommand(id, rowVersion), cancellationToken);
        var dto = await sender.Send(new GetCashShiftByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

}
