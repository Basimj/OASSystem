using MediatR;using Microsoft.AspNetCore.RateLimiting;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
using OAS.Application.Accounting.Suppliers.Commands.CreateSupplier;using OAS.Application.Accounting.Suppliers.Commands.ReserveSupplierCode;using OAS.Application.Accounting.Suppliers.Commands.SetSupplierStatus;using OAS.Application.Accounting.Suppliers.Commands.UpdateSupplier;
using OAS.Application.Accounting.Suppliers.Queries.GetSupplierAccountParents;using OAS.Application.Accounting.Suppliers.Queries.GetSupplierById;using OAS.Application.Accounting.Suppliers.Queries.GetSuppliers;using OAS.Application.Accounting.Suppliers.Queries.LookupSuppliers;
using OAS.Contracts.Accounting.Suppliers;using OAS.Contracts.Common.Pagination;
namespace OAS.API.Accounting.Controllers;
[ApiController,Authorize,EnableRateLimiting("api"),Route("api/accounting/suppliers")]
public sealed class SuppliersController(ISender sender):ControllerBase
{
 [HttpGet] public async Task<ActionResult<PagedResult<SupplierDto>>> Get([FromQuery]PageRequest request,[FromQuery]string? filter,CancellationToken ct)=>Ok(await sender.Send(new GetSuppliersQuery(request,filter),ct));
 [HttpGet("{id:guid}")] public async Task<ActionResult<SupplierDto>> GetById(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetSupplierByIdQuery(id),ct));
 [HttpGet("lookup")] public async Task<ActionResult<IReadOnlyList<SupplierLookupDto>>> Lookup([FromQuery]string? search,[FromQuery]int take=30,CancellationToken ct=default)=>Ok(await sender.Send(new LookupSuppliersQuery(search,take),ct));
 [HttpGet("account-parents")] public async Task<ActionResult<IReadOnlyList<SupplierAccountParentDto>>> Parents([FromQuery]string? search,[FromQuery]int take=50,CancellationToken ct=default)=>Ok(await sender.Send(new GetSupplierAccountParentsQuery(search,take),ct));
 [HttpPost("code/reserve")] public async Task<ActionResult<SupplierCodeReservationDto>> Reserve(CancellationToken ct)=>Ok(await sender.Send(new ReserveSupplierCodeCommand(),ct));
 [HttpPost] public async Task<ActionResult<SupplierDto>> Create([FromBody]CreateSupplierRequest request,CancellationToken ct){var id=await sender.Send(new CreateSupplierCommand(request),ct);return Created($"api/accounting/suppliers/{id}",await sender.Send(new GetSupplierByIdQuery(id),ct));}
 [HttpPut("{id:guid}")] public async Task<ActionResult<SupplierDto>> Update(Guid id,[FromBody]UpdateSupplierRequest request,CancellationToken ct){await sender.Send(new UpdateSupplierCommand(id,request),ct);return Ok(await sender.Send(new GetSupplierByIdQuery(id),ct));}
 [HttpPost("{id:guid}/status")] public async Task<ActionResult<SupplierDto>> Status(Guid id,[FromBody]SetSupplierStatusRequest request,CancellationToken ct){await sender.Send(new SetSupplierStatusCommand(id,request),ct);return Ok(await sender.Send(new GetSupplierByIdQuery(id),ct));}
}
