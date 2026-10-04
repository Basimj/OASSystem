using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.OpticalJobs.Commands;
using OAS.Application.Sales.OpticalJobs.Queries;
using OAS.Contracts.Sales.OpticalJobs;

namespace OAS.API.Optical.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/optical/jobs")]
public sealed class OpticalJobsController(ISender sender) : ControllerBase
{
    [HttpGet("work-queue")]
    public async Task<ActionResult<IReadOnlyList<OpticalJobWorkQueueDto>>> GetWorkQueue(
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetOpticalJobWorkQueueQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OpticalJobDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetOpticalJobQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<OpticalJobDetailsDto>> Create(
        [FromBody] CreateOpticalJobRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateOpticalJobCommand(request), cancellationToken);
        var dto = await sender.Send(new GetOpticalJobQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<OpticalJobDetailsDto>> Start(
        Guid id,
        [FromBody] OpticalJobActionRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new StartOpticalJobCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetOpticalJobQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/ready")]
    public async Task<ActionResult<OpticalJobDetailsDto>> MarkReady(
        Guid id,
        [FromBody] OpticalJobActionRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new MarkOpticalJobReadyCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetOpticalJobQuery(id), cancellationToken));
    }
}
