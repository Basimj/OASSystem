using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Compensation.SalaryComponents;
using OAS.Application.Features.Employees.Compensation.SalaryStructures;
using OAS.Contracts.Features.Employees.Compensation;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeeCompensationController(ISender sender) : ControllerBase
{
    [HttpGet("salary-components")]
    public async Task<ActionResult<IReadOnlyList<SalaryComponentDto>>> GetComponents([FromQuery] bool activeOnly, CancellationToken ct) => Ok(await sender.Send(new GetSalaryComponentsQuery(activeOnly), ct));

    [HttpGet("salary-components/{id:guid}")]
    public async Task<ActionResult<SalaryComponentDto>> GetComponent(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetSalaryComponentByIdQuery(id), ct));

    [HttpPost("salary-components")]
    public async Task<ActionResult<SalaryComponentDto>> CreateComponent([FromBody] CreateSalaryComponentRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateSalaryComponentCommand(request), ct);
        return Created($"api/employees/salary-components/{id:D}", await sender.Send(new GetSalaryComponentByIdQuery(id), ct));
    }

    [HttpPut("salary-components/{id:guid}")]
    public async Task<ActionResult<SalaryComponentDto>> UpdateComponent(Guid id, [FromBody] UpdateSalaryComponentRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateSalaryComponentCommand(id, request), ct);
        return Ok(await sender.Send(new GetSalaryComponentByIdQuery(id), ct));
    }

    [HttpGet("{employeeId:guid}/salary-structures")]
    public async Task<ActionResult<IReadOnlyList<EmployeeSalaryStructureDto>>> GetStructures(Guid employeeId, CancellationToken ct) => Ok(await sender.Send(new GetEmployeeSalaryStructuresQuery(employeeId), ct));

    [HttpGet("salary-structures/{id:guid}")]
    public async Task<ActionResult<EmployeeSalaryStructureDto>> GetStructure(Guid id, CancellationToken ct) => Ok(await sender.Send(new GetEmployeeSalaryStructureByIdQuery(id), ct));

    [HttpPost("{employeeId:guid}/salary-structures")]
    public async Task<ActionResult<EmployeeSalaryStructureDto>> CreateStructure(Guid employeeId, [FromBody] CreateEmployeeSalaryStructureRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateEmployeeSalaryStructureCommand(employeeId, request), ct);
        return Created($"api/employees/salary-structures/{id:D}", await sender.Send(new GetEmployeeSalaryStructureByIdQuery(id), ct));
    }

    [HttpPut("salary-structures/{id:guid}")]
    public async Task<ActionResult<EmployeeSalaryStructureDto>> UpdateStructure(Guid id, [FromBody] UpdateEmployeeSalaryStructureRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateEmployeeSalaryStructureCommand(id, request), ct);
        return Ok(await sender.Send(new GetEmployeeSalaryStructureByIdQuery(id), ct));
    }

    [HttpPost("salary-structures/{id:guid}/activate")]
    public async Task<ActionResult<EmployeeSalaryStructureDto>> ActivateStructure(Guid id, [FromBody] SalaryStructureLifecycleRequest request, CancellationToken ct)
    {
        await sender.Send(new ActivateEmployeeSalaryStructureCommand(id, request), ct);
        return Ok(await sender.Send(new GetEmployeeSalaryStructureByIdQuery(id), ct));
    }

    [HttpPost("salary-structures/{id:guid}/cancel")]
    public async Task<ActionResult<EmployeeSalaryStructureDto>> CancelStructure(Guid id, [FromBody] SalaryStructureLifecycleRequest request, CancellationToken ct)
    {
        await sender.Send(new CancelEmployeeSalaryStructureCommand(id, request), ct);
        return Ok(await sender.Send(new GetEmployeeSalaryStructureByIdQuery(id), ct));
    }
}
