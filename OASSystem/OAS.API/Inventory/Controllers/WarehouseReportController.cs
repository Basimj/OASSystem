using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Inventory.Reports;
using OAS.Contracts.Inventory.Reports;

namespace OAS.API.Inventory.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/inventory/reports/warehouses")]
public sealed class WarehouseReportController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WarehouseReportDto>> Get([FromQuery] string mode,
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] Guid? warehouseId = null, [FromQuery] Guid? variantId = null,
        [FromQuery] Guid? productId = null, [FromQuery] Guid? categoryId = null,
        [FromQuery] bool current = false, CancellationToken cancellationToken = default)
    {
        if (mode is not ("summary" or "details" or "qty" or "cost" or "transfers" or "counts") ||
            fromDate == DateOnly.MinValue || toDate == DateOnly.MaxValue || fromDate > toDate ||
            (current && (mode is not ("summary" or "details") || toDate != DateOnly.FromDateTime(DateTime.UtcNow))))
            return BadRequest("نوع التقرير أو الفترة غير صالحين.");
        return Ok(await sender.Send(new GetWarehouseReportQuery(mode, fromDate, toDate, warehouseId, variantId, productId, categoryId, current), cancellationToken));
    }
}
