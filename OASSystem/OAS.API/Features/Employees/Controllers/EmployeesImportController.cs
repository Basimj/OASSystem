using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Commands.ImportEmployees;
using OAS.Application.Features.Employees.Commands.ValidateEmployeesImport;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/import")]
public sealed class EmployeesImportController(
    ISender sender) : ControllerBase
{
    [HttpPost("validate")]
    [Authorize(Roles = "Administrator")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<EmployeeImportPreviewDto>>
        Validate(
            IFormFile file,
            CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(
                new
                {
                    code = "empty_file",
                    message = "ملف Excel فارغ."
                });
        }

        if (!file.FileName.EndsWith(
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                new
                {
                    code = "invalid_file_type",
                    message = "يجب اختيار ملف Excel بصيغة .xlsx"
                });
        }

        await using var stream =
            file.OpenReadStream();

        using var memoryStream =
            new MemoryStream();

        await stream.CopyToAsync(
            memoryStream,
            cancellationToken);

        var result =
            await sender.Send(
                new ValidateEmployeesImportCommand(
                    memoryStream.ToArray()),
                cancellationToken);

        return Ok(result);
    }


    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<EmployeeImportResultDto>>
        Import(
            IFormFile file,
            CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(
                new
                {
                    code = "empty_file",
                    message = "ملف Excel فارغ."
                });
        }

        if (!file.FileName.EndsWith(
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                new
                {
                    code = "invalid_file_type",
                    message = "يجب اختيار ملف Excel بصيغة .xlsx"
                });
        }

        await using var stream =
            file.OpenReadStream();

        using var memoryStream =
            new MemoryStream();

        await stream.CopyToAsync(
            memoryStream,
            cancellationToken);

        var result =
            await sender.Send(
                new ImportEmployeesCommand(
                    memoryStream.ToArray()),
                cancellationToken);

        return Ok(result);
    }
}