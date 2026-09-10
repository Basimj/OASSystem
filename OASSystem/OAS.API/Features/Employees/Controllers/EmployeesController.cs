using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Commands.CreateEmployee;
using OAS.Application.Features.Employees.Commands.SetEmployeeStatus;
using OAS.Application.Features.Employees.Commands.ReserveEmployeeNumber;
using OAS.Application.Features.Employees.Commands.UpdateEmployee;
using OAS.Application.Features.Employees.Queries.GetEmployeeById;
using OAS.Application.Features.Employees.Queries.GetEmployees;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmployeesQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmployeeByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("number/reserve")]
    public async Task<ActionResult<EmployeeNumberReservationDto>> ReserveNumber(CancellationToken cancellationToken)
    {
        var reservation = await sender.Send(new ReserveEmployeeNumberCommand(), cancellationToken);
        return Ok(reservation);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateEmployeeCommand(request), cancellationToken);
        var employee = await sender.Send(new GetEmployeeByIdQuery(id), cancellationToken);
        return Created($"api/employees/{id}", employee);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateEmployeeCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetEmployeeByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<EmployeeDto>> SetStatus(
        Guid id,
        [FromBody] SetEmployeeStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetEmployeeStatusCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetEmployeeByIdQuery(id), cancellationToken));
    }
}
