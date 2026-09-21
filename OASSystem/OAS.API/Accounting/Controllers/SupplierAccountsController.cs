using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.SupplierAccounts.Commands.CreateSupplierAccount;
using OAS.Application.Accounting.SupplierAccounts.Commands.SetSupplierAccountStatus;
using OAS.Application.Accounting.SupplierAccounts.Commands.UpdateSupplierAccount;
using OAS.Application.Accounting.SupplierAccounts.Queries.GetSupplierAccountById;
using OAS.Application.Accounting.SupplierAccounts.Queries.GetSupplierAccounts;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/supplier-accounts")]
public sealed class SupplierAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierAccountDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetSupplierAccountsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierAccountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSupplierAccountByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierAccountDto>> Create(
        [FromBody] CreateSupplierAccountRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateSupplierAccountCommand(request), cancellationToken);
        var dto = await sender.Send(new GetSupplierAccountByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierAccountDto>> Update(
        Guid id,
        [FromBody] UpdateSupplierAccountRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateSupplierAccountCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetSupplierAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<SupplierAccountDto>> SetStatus(
        Guid id,
        [FromBody] SetSupplierAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetSupplierAccountStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetSupplierAccountByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
