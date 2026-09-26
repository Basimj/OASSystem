using MediatR;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.RateLimiting;using OAS.Application.Accounting.EmployeeAccounts.Commands.ActivateEmployeeAccount;using OAS.Application.Accounting.EmployeeAccounts.Commands.SetEmployeeAccountStatus;using OAS.Application.Accounting.EmployeeAccounts.Queries.GetEmployeeAccountById;using OAS.Application.Accounting.EmployeeAccounts.Queries.GetEmployeeAccounts;using OAS.Contracts.Accounting.EmployeeAccounts;using OAS.Contracts.Common.Pagination;
namespace OAS.API.Accounting.Controllers;
[ApiController,Authorize,EnableRateLimiting("api"),Route("api/accounting/employee-accounts")]
public sealed class EmployeeAccountsController(ISender sender):ControllerBase
{
 [HttpGet] public async Task<ActionResult<PagedResult<EmployeeAccountDto>>> Get([FromQuery]PageRequest request,CancellationToken ct)=>Ok(await sender.Send(new GetEmployeeAccountsQuery(request),ct));
 [HttpGet("{id:guid}")] public async Task<ActionResult<EmployeeAccountDto>> GetById(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetEmployeeAccountByIdQuery(id),ct));
 [HttpPost("activate")] public async Task<ActionResult<EmployeeAccountDto>> Activate(ActivateEmployeeAccountRequest request,CancellationToken ct){var id=await sender.Send(new ActivateEmployeeAccountCommand(request),ct);return CreatedAtAction(nameof(GetById),new{id},await sender.Send(new GetEmployeeAccountByIdQuery(id),ct));}
 [HttpPost("{id:guid}/status")] public async Task<ActionResult<EmployeeAccountDto>> Status(Guid id,SetEmployeeAccountStatusRequest request,CancellationToken ct){await sender.Send(new SetEmployeeAccountStatusCommand(id,request),ct);return Ok(await sender.Send(new GetEmployeeAccountByIdQuery(id),ct));}
}
