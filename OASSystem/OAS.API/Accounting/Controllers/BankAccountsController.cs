using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.BankAccounts.Commands.CreateBankAccount;
using OAS.Application.Accounting.BankAccounts.Commands.SetBankAccountStatus;
using OAS.Application.Accounting.BankAccounts.Commands.UpdateBankAccount;
using OAS.Application.Accounting.BankAccounts.Queries.GetBankAccountById;
using OAS.Application.Accounting.BankAccounts.Queries.GetBankAccounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/bank-accounts")]
public sealed class BankAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<BankAccountDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetBankAccountsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankAccountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBankAccountByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create(
        [FromBody] CreateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateBankAccountCommand(request), cancellationToken);
        var dto = await sender.Send(new GetBankAccountByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BankAccountDto>> Update(
        Guid id,
        [FromBody] UpdateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateBankAccountCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetBankAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<BankAccountDto>> SetStatus(
        Guid id,
        [FromBody] SetBankAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetBankAccountStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetBankAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
