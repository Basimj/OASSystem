using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.JobTitles.Commands.CreateJobTitle;
using OAS.Application.Features.Employees.JobTitles.Commands.UpdateJobTitle;
using OAS.Application.Features.Employees.JobTitles.Queries.GetJobTitleById;
using OAS.Application.Features.Employees.JobTitles.Queries.GetJobTitles;
using OAS.Contracts.Features.Employees.JobTitles;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/job-titles")]
public sealed class JobTitlesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobTitleDto>>> Get(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetJobTitlesQuery(activeOnly), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobTitleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetJobTitleByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<JobTitleDto>> Create(
        CreateJobTitleRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateJobTitleCommand(request), cancellationToken);
        var item = await sender.Send(new GetJobTitleByIdQuery(id), cancellationToken);
        return Created($"api/job-titles/{id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobTitleDto>> Update(
        Guid id,
        UpdateJobTitleRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateJobTitleCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetJobTitleByIdQuery(id), cancellationToken));
    }
}
