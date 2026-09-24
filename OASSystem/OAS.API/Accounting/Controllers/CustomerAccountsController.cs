using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.CustomerAccounts.Commands.CreateCustomerAccount;
using OAS.Application.Accounting.CustomerAccounts.Commands.SetCustomerAccountStatus;
using OAS.Application.Accounting.CustomerAccounts.Commands.UpdateCustomerAccount;
using OAS.Application.Accounting.CustomerAccounts.Queries.GetCustomerAccountById;
using OAS.Application.Accounting.CustomerAccounts.Queries.GetCustomerAccounts;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/customer-accounts")]
public sealed class CustomerAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerAccountDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetCustomerAccountsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerAccountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerAccountByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerAccountDto>> Create(
        [FromBody] CreateCustomerAccountRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateCustomerAccountCommand(request), cancellationToken);
        var dto = await sender.Send(new GetCustomerAccountByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerAccountDto>> Update(
        Guid id,
        [FromBody] UpdateCustomerAccountRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCustomerAccountCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetCustomerAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<CustomerAccountDto>> SetStatus(
        Guid id,
        [FromBody] SetCustomerAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetCustomerAccountStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetCustomerAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
