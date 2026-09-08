using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Commands.ValidateEmployeeBulk;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/bulk")]
public sealed class EmployeesBulkController(
    ISender sender) : ControllerBase
{
    [HttpPost("validate")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<EmployeeBulkValidationResultDto>>
        Validate(
            EmployeeBulkValidationRequest request,
            CancellationToken cancellationToken)
    {
        if (request is null ||
            request.Rows is null)
        {
            return BadRequest(new
            {
                code = "invalid_request",
                message = "بيانات التحقق غير صالحة."
            });
        }

        var result =
            await sender.Send(
                new ValidateEmployeeBulkCommand(request),
                cancellationToken);

        return Ok(result);
    }
}