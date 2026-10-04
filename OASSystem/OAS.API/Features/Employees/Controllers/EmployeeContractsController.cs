using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Employment.Contracts;
using OAS.Contracts.Features.Employees.Contracts;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeeContractsController(ISender sender) : ControllerBase
{
    [HttpGet("{employeeId:guid}/contracts")]
    public async Task<ActionResult<IReadOnlyList<EmployeeContractDto>>> GetForEmployee(Guid employeeId, CancellationToken ct) => Ok(await sender.Send(new GetEmployeeContractsQuery(employeeId), ct));

    [HttpGet("contracts/{contractId:guid}")]
    public async Task<ActionResult<EmployeeContractDto>> GetById(Guid contractId, CancellationToken ct) => Ok(await sender.Send(new GetEmployeeContractByIdQuery(contractId), ct));

    [HttpPost("{employeeId:guid}/contracts")]
    public async Task<ActionResult<EmployeeContractDto>> Create(Guid employeeId, [FromBody] CreateEmployeeContractRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateEmployeeContractCommand(employeeId, request), ct);
        return Created($"api/employees/contracts/{id:D}", await sender.Send(new GetEmployeeContractByIdQuery(id), ct));
    }

    [HttpPut("contracts/{contractId:guid}")]
    public async Task<ActionResult<EmployeeContractDto>> Update(Guid contractId, [FromBody] UpdateEmployeeContractRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateEmployeeContractCommand(contractId, request), ct);
        return Ok(await sender.Send(new GetEmployeeContractByIdQuery(contractId), ct));
    }

    [HttpPost("contracts/{contractId:guid}/activate")]
    public async Task<ActionResult<EmployeeContractDto>> Activate(Guid contractId, [FromBody] ContractLifecycleRequest request, CancellationToken ct)
    {
        await sender.Send(new ActivateEmployeeContractCommand(contractId, request), ct);
        return Ok(await sender.Send(new GetEmployeeContractByIdQuery(contractId), ct));
    }

    [HttpPost("contracts/{contractId:guid}/terminate")]
    public async Task<ActionResult<EmployeeContractDto>> Terminate(Guid contractId, [FromBody] TerminateEmployeeContractRequest request, CancellationToken ct)
    {
        await sender.Send(new TerminateEmployeeContractCommand(contractId, request), ct);
        return Ok(await sender.Send(new GetEmployeeContractByIdQuery(contractId), ct));
    }

    [HttpPost("contracts/{contractId:guid}/cancel")]
    public async Task<ActionResult<EmployeeContractDto>> Cancel(Guid contractId, [FromBody] ContractLifecycleRequest request, CancellationToken ct)
    {
        await sender.Send(new CancelEmployeeContractCommand(contractId, request), ct);
        return Ok(await sender.Send(new GetEmployeeContractByIdQuery(contractId), ct));
    }
}
