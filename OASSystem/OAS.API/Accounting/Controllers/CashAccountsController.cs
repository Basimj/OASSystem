using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;
using OAS.Application.Accounting.CashAccounts.Commands.ReserveCashAccountCode;
using OAS.Application.Accounting.CashAccounts.Commands.SetCashAccountStatus;
using OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;
using OAS.Application.Accounting.CashAccounts.Queries.GetCashAccountById;
using OAS.Application.Accounting.CashAccounts.Queries.GetCashAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/cash-accounts")]
public sealed class CashAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CashAccountDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetCashAccountsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CashAccountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCashAccountByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("code/reserve")]
    public async Task<ActionResult<CashAccountCodeReservationDto>> ReserveCode(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReserveCashAccountCodeCommand(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CashAccountDto>> Create(
        [FromBody] CreateCashAccountRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateCashAccountCommand(request), cancellationToken);
        var dto = await sender.Send(new GetCashAccountByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CashAccountDto>> Update(
        Guid id,
        [FromBody] UpdateCashAccountRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCashAccountCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetCashAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<CashAccountDto>> SetStatus(
        Guid id,
        [FromBody] SetCashAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetCashAccountStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetCashAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
