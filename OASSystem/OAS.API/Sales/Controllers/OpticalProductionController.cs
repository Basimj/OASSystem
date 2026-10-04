using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Production.Commands;
using OAS.Application.Sales.Production.Queries;
using OAS.Contracts.Sales.Production;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/production")]
public sealed class OpticalProductionController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OpticalProductionJobDto>>> Get(
        [FromQuery] OpticalProductionStatus? status,
        [FromQuery] Guid? salesInvoiceId,
        CancellationToken ct) =>
        Ok(await sender.Send(new GetOpticalProductionJobsQuery(status, salesInvoiceId), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OpticalProductionJobDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetOpticalProductionJobQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<OpticalProductionJobDto>> Create(
        [FromBody] CreateOpticalProductionJobRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new CreateOpticalProductionJobCommand(request), ct));

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<OpticalProductionJobDto>> Release(
        Guid id,
        [FromBody] OpticalProductionActionRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new ReleaseOpticalProductionJobCommand(id, request), ct));

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<OpticalProductionJobDto>> Start(
        Guid id,
        [FromBody] OpticalProductionActionRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new StartOpticalProductionJobCommand(id, request), ct));

    [HttpPost("{id:guid}/issue-materials")]
    public async Task<ActionResult<OpticalProductionJobDto>> Issue(
        Guid id,
        [FromBody] OpticalProductionActionRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new IssueOpticalProductionMaterialsCommand(id, request), ct));

    [HttpPost("{id:guid}/quality-control")]
    public async Task<ActionResult<OpticalProductionJobDto>> QualityControl(
        Guid id,
        [FromBody] SubmitOpticalProductionQcRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new SubmitOpticalProductionQcCommand(id, request), ct));

    [HttpPost("{id:guid}/remake")]
    public async Task<ActionResult<OpticalProductionJobDto>> Remake(
        Guid id,
        [FromBody] CreateOpticalProductionRemakeRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new CreateOpticalProductionRemakeCommand(id, request), ct));

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<OpticalProductionJobDto>> Complete(
        Guid id,
        [FromBody] OpticalProductionActionRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new CompleteOpticalProductionJobCommand(id, request), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OpticalProductionJobDto>> Cancel(
        Guid id,
        [FromBody] OpticalProductionActionRequest request,
        CancellationToken ct) =>
        Ok(await sender.Send(new CancelOpticalProductionJobCommand(id, request), ct));
}
