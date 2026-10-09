using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Contracts.Sales.Production;

namespace OAS.API.Sales.Controllers;

/// <summary>
/// Legacy compatibility endpoint. The active laboratory workflow is OpticalJob under /api/optical/jobs.
/// Keeping this controller avoids a hard 404 for older clients while preventing creation of a second,
/// parallel production workflow.
/// </summary>
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/production")]
[Obsolete("Use api/optical/jobs. OpticalJob is the single laboratory workflow.")]
public sealed class OpticalProductionController : ControllerBase
{
    private ObjectResult Gone() => Problem(
        statusCode: StatusCodes.Status410Gone,
        title: "Legacy optical production workflow is disabled.",
        detail: "استخدم مسار المعمل الموحد api/optical/jobs. تم إيقاف مسار sales/production لمنع إنشاء دورتين متوازيتين للإنتاج البصري.");

    [HttpGet]
    public ActionResult<IReadOnlyList<OpticalProductionJobDto>> Get(
        [FromQuery] OpticalProductionStatus? status,
        [FromQuery] Guid? salesInvoiceId) => Gone();

    [HttpGet("{id:guid}")]
    public ActionResult<OpticalProductionJobDto> GetById(Guid id) => Gone();

    [HttpPost]
    public ActionResult<OpticalProductionJobDto> Create([FromBody] CreateOpticalProductionJobRequest request) => Gone();

    [HttpPost("{id:guid}/release")]
    public ActionResult<OpticalProductionJobDto> Release(Guid id, [FromBody] OpticalProductionActionRequest request) => Gone();

    [HttpPost("{id:guid}/start")]
    public ActionResult<OpticalProductionJobDto> Start(Guid id, [FromBody] OpticalProductionActionRequest request) => Gone();

    [HttpPost("{id:guid}/issue-materials")]
    public ActionResult<OpticalProductionJobDto> Issue(Guid id, [FromBody] OpticalProductionActionRequest request) => Gone();

    [HttpPost("{id:guid}/quality-control")]
    public ActionResult<OpticalProductionJobDto> QualityControl(Guid id, [FromBody] SubmitOpticalProductionQcRequest request) => Gone();

    [HttpPost("{id:guid}/remake")]
    public ActionResult<OpticalProductionJobDto> Remake(Guid id, [FromBody] CreateOpticalProductionRemakeRequest request) => Gone();

    [HttpPost("{id:guid}/complete")]
    public ActionResult<OpticalProductionJobDto> Complete(Guid id, [FromBody] OpticalProductionActionRequest request) => Gone();

    [HttpPost("{id:guid}/cancel")]
    public ActionResult<OpticalProductionJobDto> Cancel(Guid id, [FromBody] OpticalProductionActionRequest request) => Gone();
}
