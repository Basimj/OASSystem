using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.Reports.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Reports;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/reports/supplier-performance")]
public sealed class SupplierPerformanceReportsController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<SupplierPerformanceSummaryDto>> GetSummary(
        [FromQuery] SupplierPerformanceQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSupplierPerformanceSummaryQuery(request), cancellationToken));

    [HttpGet("lines")]
    public async Task<ActionResult<PagedResult<SupplierPerformanceLineDto>>> GetLines(
        [FromQuery] SupplierPerformanceQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSupplierPerformanceLinesQuery(request), cancellationToken));

    [HttpGet("lines/{purchaseOrderLineId:guid}/drilldown")]
    public async Task<ActionResult<SupplierPerformanceDrillDownDto>> GetDrillDown(
        Guid purchaseOrderLineId,
        [FromQuery] SupplierPerformanceQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSupplierPerformanceDrillDownQuery(purchaseOrderLineId, request), cancellationToken));

    [HttpGet("price-history")]
    public async Task<ActionResult<PagedResult<SupplierPriceHistoryItemDto>>> GetPriceHistory(
        [FromQuery] SupplierPerformanceQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSupplierPriceHistoryReportQuery(request), cancellationToken));

    [HttpGet("print")]
    public async Task<ActionResult<SupplierPerformancePrintDto>> GetPrintModel(
        [FromQuery] SupplierPerformanceQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSupplierPerformancePrintQuery(request), cancellationToken));

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] SupplierPerformanceQueryRequest request,
        [FromQuery] SupplierPerformanceExportFormat format = SupplierPerformanceExportFormat.Xlsx,
        CancellationToken cancellationToken = default)
    {
        var file = await sender.Send(new ExportSupplierPerformanceQuery(request, format), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("~/api/purchasing/suppliers/{supplierId:guid}/performance")]
    public async Task<ActionResult<SupplierPerformanceSummaryDto>> GetSupplierSummary(
        Guid supplierId,
        [FromQuery] SupplierPerformanceQueryRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = request with { SupplierId = supplierId };
        return Ok(await sender.Send(new GetSupplierPerformanceSummaryQuery(normalized), cancellationToken));
    }

    [HttpGet("~/api/purchasing/suppliers/{supplierId:guid}/performance/lines")]
    public async Task<ActionResult<PagedResult<SupplierPerformanceLineDto>>> GetSupplierLines(
        Guid supplierId,
        [FromQuery] SupplierPerformanceQueryRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = request with { SupplierId = supplierId };
        return Ok(await sender.Send(new GetSupplierPerformanceLinesQuery(normalized), cancellationToken));
    }
}
