using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Abstractions;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/template")]
public sealed class EmployeesTemplateController(
    IEmployeeExcelTemplateGenerator templateGenerator)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    [Produces(
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public IActionResult Download()
    {
        var file = templateGenerator.Generate();

        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Employees_Template.xlsx");
    }
}