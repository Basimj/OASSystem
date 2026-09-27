using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Contracts.Printing;

namespace OAS.API.Printing.Controllers;

[ApiController]
[EnableRateLimiting("api")]
[Route("api/printing")]
public sealed class PrintJobsController(
    IPrintJobQueue queue,
    VoucherPrintPayloadFactory payloadFactory,
    DesktopPrintApiKeyValidator apiKeyValidator) : ControllerBase
{
    [Authorize]
    [HttpPost("jobs")]
    public async Task<ActionResult<PrintJobCreatedDto>> Create(
        [FromBody] CreatePrintJobRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DocumentId == Guid.Empty)
            return BadRequest("معرف المستند مطلوب.");

        if (request.Copies is < 1 or > 20)
            return BadRequest("عدد النسخ يجب أن يكون بين 1 و20.");

        if (request.DocumentType is not (PrintDocumentTypes.ReceiptVoucher or PrintDocumentTypes.PaymentVoucher))
            return BadRequest("نوع المستند غير مدعوم للطباعة.");

        var data = await payloadFactory.BuildAsync(
            request.DocumentType,
            request.DocumentId,
            cancellationToken);

        var job = new DesktopPrintJobDto(
            Guid.NewGuid(),
            request.DocumentType,
            request.TemplateCode,
            request.PrinterName,
            request.Copies,
            request.ShowPreview,
            JsonSerializer.SerializeToElement(data));

        return Ok(queue.Enqueue(request.WorkstationCode, job));
    }

    [AllowAnonymous]
    [HttpGet("desktop/jobs/next")]
    public ActionResult<DesktopPrintJobDto> Next([FromQuery] string? workstationCode)
    {
        if (!apiKeyValidator.IsAuthorized(Request))
            return Unauthorized();

        var job = queue.TryLeaseNext(workstationCode ?? "DEFAULT");
        return job is null ? NoContent() : Ok(job);
    }

    [AllowAnonymous]
    [HttpPost("desktop/jobs/{jobId:guid}/complete")]
    public IActionResult Complete(Guid jobId)
    {
        if (!apiKeyValidator.IsAuthorized(Request))
            return Unauthorized();

        return queue.Complete(jobId) ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [HttpPost("desktop/jobs/{jobId:guid}/failed")]
    public IActionResult Failed(Guid jobId, [FromBody] FailPrintJobRequest? request)
    {
        if (!apiKeyValidator.IsAuthorized(Request))
            return Unauthorized();

        return queue.Fail(jobId, request?.Error) ? NoContent() : NotFound();
    }
}
