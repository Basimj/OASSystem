using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Organization.Departments;
using OAS.Contracts.Features.Employees.Departments;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/departments")]
public sealed class DepartmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> Get([FromQuery] bool activeOnly, CancellationToken ct) => Ok(await sender.Send(new GetDepartmentsQuery(activeOnly), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DepartmentDto>> GetById(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetDepartmentByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateDepartmentCommand(request), ct);
        return Created($"api/departments/{id:D}", await sender.Send(new GetDepartmentByIdQuery(id), ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DepartmentDto>> Update(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateDepartmentCommand(id, request), ct);
        return Ok(await sender.Send(new GetDepartmentByIdQuery(id), ct));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<DepartmentDto>> SetStatus(Guid id, [FromBody] SetDepartmentStatusRequest request, CancellationToken ct)
    {
        await sender.Send(new SetDepartmentStatusCommand(id, request), ct);
        return Ok(await sender.Send(new GetDepartmentByIdQuery(id), ct));
    }
}
