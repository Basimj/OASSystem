using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Images.Commands.RemoveEmployeeImage;
using OAS.Application.Features.Employees.Images.Commands.SetEmployeeImage;
using OAS.Application.Features.Employees.Images.Queries.GetEmployeeImage;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/{employeeId:guid}/image")]
public sealed class EmployeeImagesController(ISender sender) : ControllerBase
{
    private const long MaximumUploadBytes = 2_500_000;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken cancellationToken)
    {
        var image = await sender.Send(new GetEmployeeImageQuery(employeeId), cancellationToken);
        if (image is null) return NotFound();
        return File(image.Content, image.ContentType, enableRangeProcessing: false);
    }

    [HttpPut]
    [RequestSizeLimit(MaximumUploadBytes + 128_000)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Set(Guid employeeId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > MaximumUploadBytes)
            return BadRequest();

        await using var memory = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memory, cancellationToken);
        await sender.Send(new SetEmployeeImageCommand(employeeId, file.ContentType, memory.ToArray()), cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid employeeId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveEmployeeImageCommand(employeeId), cancellationToken);
        return NoContent();
    }
}
