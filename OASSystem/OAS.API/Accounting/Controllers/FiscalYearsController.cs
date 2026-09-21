using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.FiscalYears.Commands.CreateFiscalYear;
using OAS.Application.Accounting.FiscalYears.Commands.SetFiscalYearStatus;
using OAS.Application.Accounting.FiscalYears.Commands.UpdateFiscalYear;
using OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYearById;
using OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYears;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/fiscal-years")]
public sealed class FiscalYearsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<FiscalYearDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFiscalYearsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FiscalYearDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFiscalYearByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<FiscalYearDto>> Create(
        [FromBody] CreateFiscalYearRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateFiscalYearCommand(request), cancellationToken);
        var dto = await sender.Send(new GetFiscalYearByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FiscalYearDto>> Update(
        Guid id,
        [FromBody] UpdateFiscalYearRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateFiscalYearCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetFiscalYearByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetFiscalYearStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetFiscalYearStatusCommand(id, request), cancellationToken);
        return NoContent();
    }
}
