using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Documents;
using OAS.Contracts.Features.Employees.Documents;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeeDocumentsController(ISender sender) : ControllerBase
{
    private const long MaximumUploadBytes = 10 * 1024 * 1024;

    [HttpGet("{employeeId:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<EmployeeDocumentDto>>> Get(Guid employeeId, CancellationToken ct) => Ok(await sender.Send(new GetEmployeeDocumentsQuery(employeeId), ct));

    [HttpPost("{employeeId:guid}/documents")]
    [RequestSizeLimit(MaximumUploadBytes + 256_000)]
    public async Task<IActionResult> Upload(Guid employeeId, [FromForm] byte documentType, [FromForm] string title, [FromForm] DateOnly? issueDate, [FromForm] DateOnly? expiryDate, [FromForm] string? notes, IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > MaximumUploadBytes) return BadRequest();
        await using var memory = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memory, ct);
        var id = await sender.Send(new UploadEmployeeDocumentCommand(employeeId, documentType, title, issueDate, expiryDate, notes, file.FileName, file.ContentType, memory.ToArray()), ct);
        return Created($"api/employees/documents/{id:D}/file", id);
    }

    [HttpPut("documents/{documentId:guid}")]
    public async Task<IActionResult> Update(Guid documentId, [FromBody] UpdateEmployeeDocumentRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateEmployeeDocumentCommand(documentId, request), ct);
        return NoContent();
    }

    [HttpPost("documents/{documentId:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid documentId, [FromBody] SetEmployeeDocumentStatusRequest request, CancellationToken ct)
    {
        await sender.Send(new SetEmployeeDocumentStatusCommand(documentId, request), ct);
        return NoContent();
    }

    [HttpGet("documents/{documentId:guid}/file")]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken ct)
    {
        var file = await sender.Send(new GetEmployeeDocumentFileQuery(documentId), ct);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }
}
