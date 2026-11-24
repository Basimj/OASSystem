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
[Route("api/inventory/reports/supplier-product-movement")]
public sealed class SupplierProductMovementReportController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SupplierProductMovementReportDto>> Get(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] Guid? variantId = null, [FromQuery] Guid? productId = null,
        [FromQuery] Guid? categoryId = null, [FromQuery] Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate == DateOnly.MinValue || toDate == DateOnly.MaxValue || fromDate > toDate)
            return BadRequest("الفترة المحددة غير صالحة.");
        return Ok(await sender.Send(new GetSupplierProductMovementReportQuery(fromDate,toDate,
            variantId,productId,categoryId,warehouseId), cancellationToken));
    }
}
