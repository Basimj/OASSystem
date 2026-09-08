using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Queries.ExportEmployees;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/export")]
public sealed class EmployeesExportController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    [Produces(
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export(
        CancellationToken cancellationToken)
    {
        var file = await sender.Send(
            new ExportEmployeesQuery(),
            cancellationToken);

        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Employees.xlsx");
    }
}