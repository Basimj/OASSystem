using MediatR;using Microsoft.AspNetCore.RateLimiting;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
using OAS.Application.Accounting.Customers.Commands.CreateCustomer;using OAS.Application.Accounting.Customers.Commands.ReserveCustomerCode;using OAS.Application.Accounting.Customers.Commands.SetCustomerStatus;using OAS.Application.Accounting.Customers.Commands.UpdateCustomer;
using OAS.Application.Accounting.Customers.Queries.GetCustomerAccountParents;using OAS.Application.Accounting.Customers.Queries.GetCustomerById;using OAS.Application.Accounting.Customers.Queries.GetCustomers;using OAS.Application.Accounting.Customers.Queries.LookupCustomers;
using OAS.Contracts.Accounting.Customers;using OAS.Contracts.Common.Pagination;
namespace OAS.API.Accounting.Controllers;
[ApiController,Authorize,EnableRateLimiting("api"),Route("api/accounting/customers")]
public sealed class CustomersController(ISender sender):ControllerBase
{
 [HttpGet] public async Task<ActionResult<PagedResult<CustomerDto>>> Get([FromQuery]PageRequest request,[FromQuery]string? filter,CancellationToken ct)=>Ok(await sender.Send(new GetCustomersQuery(request,filter),ct));
 [HttpGet("{id:guid}")] public async Task<ActionResult<CustomerDto>> GetById(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetCustomerByIdQuery(id),ct));
 [HttpGet("lookup")] public async Task<ActionResult<IReadOnlyList<CustomerLookupDto>>> Lookup([FromQuery]string? search,[FromQuery]int take=30,CancellationToken ct=default)=>Ok(await sender.Send(new LookupCustomersQuery(search,take),ct));
 [HttpGet("account-parents")] public async Task<ActionResult<IReadOnlyList<CustomerAccountParentDto>>> Parents([FromQuery]string? search,[FromQuery]int take=50,CancellationToken ct=default)=>Ok(await sender.Send(new GetCustomerAccountParentsQuery(search,take),ct));
 [HttpPost("code/reserve")] public async Task<ActionResult<CustomerCodeReservationDto>> Reserve(CancellationToken ct)=>Ok(await sender.Send(new ReserveCustomerCodeCommand(),ct));
 [HttpPost] public async Task<ActionResult<CustomerDto>> Create([FromBody]CreateCustomerRequest request,CancellationToken ct){var id=await sender.Send(new CreateCustomerCommand(request),ct);return Created($"api/accounting/customers/{id}",await sender.Send(new GetCustomerByIdQuery(id),ct));}
 [HttpPut("{id:guid}")] public async Task<ActionResult<CustomerDto>> Update(Guid id,[FromBody]UpdateCustomerRequest request,CancellationToken ct){await sender.Send(new UpdateCustomerCommand(id,request),ct);return Ok(await sender.Send(new GetCustomerByIdQuery(id),ct));}
 [HttpPost("{id:guid}/status")] public async Task<ActionResult<CustomerDto>> Status(Guid id,[FromBody]SetCustomerStatusRequest request,CancellationToken ct){await sender.Send(new SetCustomerStatusCommand(id,request),ct);return Ok(await sender.Send(new GetCustomerByIdQuery(id),ct));}
}
