using MediatR;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.RateLimiting;using OAS.Application.Accounting.Currencies.Commands.CreateCurrency;using OAS.Application.Accounting.Currencies.Commands.UpdateCurrency;using OAS.Application.Accounting.Currencies.Queries.GetCurrencies;using OAS.Application.Accounting.Currencies.Queries.GetCurrencyById;using OAS.Contracts.Accounting.Currencies;using OAS.Contracts.Common.Pagination;
namespace OAS.API.Accounting.Controllers;
[ApiController,Authorize,EnableRateLimiting("api"),Route("api/accounting/currencies")]
public sealed class CurrenciesController(ISender sender):ControllerBase
{
 [HttpGet] public async Task<ActionResult<PagedResult<CurrencyDto>>> Get([FromQuery]PageRequest request,[FromQuery(Name="searchTerm")]string? searchTerm,CancellationToken ct){request=AccountingPageRequestCompatibility.Apply(request,searchTerm);return Ok(await sender.Send(new GetCurrenciesQuery(request),ct));}
 [HttpGet("{id:guid}")] public async Task<ActionResult<CurrencyDto>> GetById(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetCurrencyByIdQuery(id),ct));
 [HttpPost] public async Task<ActionResult<CurrencyDto>> Create(CreateCurrencyRequest request,CancellationToken ct){var id=await sender.Send(new CreateCurrencyCommand(request),ct);return CreatedAtAction(nameof(GetById),new{id},await sender.Send(new GetCurrencyByIdQuery(id),ct));}
 [HttpPut("{id:guid}")] public async Task<ActionResult<CurrencyDto>> Update(Guid id,UpdateCurrencyRequest request,CancellationToken ct){await sender.Send(new UpdateCurrencyCommand(id,request),ct);return Ok(await sender.Send(new GetCurrencyByIdQuery(id),ct));}
}
