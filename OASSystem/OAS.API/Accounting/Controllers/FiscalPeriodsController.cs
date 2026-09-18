using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.FiscalPeriods.Commands.CreateFiscalPeriod;
using OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodLocks;
using OAS.Application.Accounting.FiscalPeriods.Commands.SetFiscalPeriodStatus;
using OAS.Application.Accounting.FiscalPeriods.Commands.UpdateFiscalPeriod;
using OAS.Application.Accounting.FiscalPeriods.Queries.GetFiscalPeriodById;
using OAS.Application.Accounting.FiscalPeriods.Queries.GetFiscalPeriods;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/fiscal-periods")]
public sealed class FiscalPeriodsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<FiscalPeriodDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFiscalPeriodsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FiscalPeriodDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFiscalPeriodByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<FiscalPeriodDto>> Create(
        [FromBody] CreateFiscalPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateFiscalPeriodCommand(request), cancellationToken);
        var dto = await sender.Send(new GetFiscalPeriodByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FiscalPeriodDto>> Update(
        Guid id,
        [FromBody] UpdateFiscalPeriodRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateFiscalPeriodCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetFiscalPeriodByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetFiscalPeriodStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetFiscalPeriodStatusCommand(id, request), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/locks")]
    public async Task<ActionResult<FiscalPeriodDto>> SetLocks(
        Guid id,
        [FromBody] SetFiscalPeriodLocksRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetFiscalPeriodLocksCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetFiscalPeriodByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
