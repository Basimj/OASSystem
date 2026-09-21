using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.CostCenters.Commands.CreateCostCenter;
using OAS.Application.Accounting.CostCenters.Commands.SetCostCenterStatus;
using OAS.Application.Accounting.CostCenters.Commands.UpdateCostCenter;
using OAS.Application.Accounting.CostCenters.Queries.GetCostCenterById;
using OAS.Application.Accounting.CostCenters.Queries.GetCostCenters;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/cost-centers")]
public sealed class CostCentersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CostCenterDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCostCentersQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CostCenterDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCostCenterByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CostCenterDto>> Create(
        [FromBody] CreateCostCenterRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateCostCenterCommand(request), cancellationToken);
        var dto = await sender.Send(new GetCostCenterByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CostCenterDto>> Update(
        Guid id,
        [FromBody] UpdateCostCenterRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCostCenterCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetCostCenterByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetCostCenterStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetCostCenterStatusCommand(id, request), cancellationToken);
        return NoContent();
    }
}
