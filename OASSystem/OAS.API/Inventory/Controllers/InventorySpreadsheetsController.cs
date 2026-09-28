using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Inventory.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Spreadsheets;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/spreadsheets/{section}")]
public sealed class InventorySpreadsheetsController(InventorySpreadsheetService service) : ControllerBase
{
    private const string Mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const long MaximumUploadBytes = 10 * 1024 * 1024;

    [HttpGet("template")]
    public async Task<IActionResult> Template(string section, CancellationToken cancellationToken)
    {
        var bytes = await service.TemplateAsync(section, cancellationToken);
        return File(bytes, Mime, $"inventory-{section}-template.xlsx");
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        string section,
        [FromQuery] PageRequest request,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] Guid? transactionId,
        [FromQuery] int? transactionType,
        [FromQuery] int? transactionStatus,
        [FromQuery] int? movementType,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int? stockCountStatus,
        CancellationToken cancellationToken)
    {
        var bytes = await service.ExportAsync(
            section,
            request,
            warehouseId,
            productVariantId,
            transactionId,
            transactionType,
            transactionStatus,
            movementType,
            fromDate,
            toDate,
            stockCountStatus,
            cancellationToken);

        return File(bytes, Mime, $"inventory-{section}.xlsx");
    }

    [HttpPost("preview")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public Task<ActionResult<SpreadsheetPreview>> Preview(
        string section,
        IFormFile file,
        CancellationToken cancellationToken) =>
        Upload(section, file, confirm: false, cancellationToken);

    [HttpPost("import")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public Task<ActionResult<SpreadsheetPreview>> Import(
        string section,
        IFormFile file,
        CancellationToken cancellationToken) =>
        Upload(section, file, confirm: true, cancellationToken);

    private async Task<ActionResult<SpreadsheetPreview>> Upload(
        string section,
        IFormFile file,
        bool confirm,
        CancellationToken cancellationToken)
    {
        if (file is null ||
            file.Length <= 0 ||
            file.Length > MaximumUploadBytes ||
            !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                code = "invalid_file",
                message = "اختر ملف xlsx غير فارغ بحجم لا يتجاوز 10 MB."
            });
        }

        await using var memory = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        await file.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();

        var result = confirm
            ? await service.ImportAsync(section, bytes, cancellationToken)
            : await service.PreviewAsync(section, bytes, cancellationToken);

        return Ok(result);
    }
}
