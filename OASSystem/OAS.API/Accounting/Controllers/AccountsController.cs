using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Accounts.Commands.CreateAccount;
using OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;
using OAS.Application.Accounting.Accounts.Commands.UpdateAccount;
using OAS.Application.Accounting.Accounts.Queries.GetAccountById;
using OAS.Application.Accounting.Accounts.Queries.GetAccounts;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/accounts")]
public sealed class AccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccountDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetAccountsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateAccountCommand(request), cancellationToken);
        var dto = await sender.Send(new GetAccountByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Update(
        Guid id,
        [FromBody] UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateAccountCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<AccountDto>> SetStatus(
        Guid id,
        [FromBody] SetAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetAccountStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
