using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Inventory.Spreadsheets;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/spreadsheets/{section}")]
public sealed class InventorySpreadsheetsController(InventorySpreadsheetService service) : ControllerBase
{
    private const string Mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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
}
