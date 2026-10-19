using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.API.Sales.Reports;
using OAS.Application.Sales.Reports;
using OAS.Contracts.Sales.Reports;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/reports")]
public sealed class SalesReportsController(ISender sender) : ControllerBase
{
    private const int MaximumExportDocuments = 20_000;

    [HttpGet]
    public async Task<ActionResult<SalesReportDto>> Get(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] SalesReportGroupBy groupBy = SalesReportGroupBy.Day,
        [FromQuery] Guid? customerId = null, [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? productVariantId = null, [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetSalesReportQuery(new(
            fromDate, toDate, groupBy, customerId, employeeId,
            productVariantId, categoryId, warehouseId, pageNumber, pageSize)), ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] SalesReportGroupBy groupBy = SalesReportGroupBy.Day,
        [FromQuery] Guid? customerId = null, [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? productVariantId = null, [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? warehouseId = null, [FromQuery] string format = "xlsx",
        CancellationToken ct = default)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("صيغة التصدير المدعومة CSV أو XLSX فقط.");

        var filter = new SalesReportFilterDto(fromDate, toDate, groupBy,
            customerId, employeeId, productVariantId, categoryId, warehouseId, 1, 100);
        // All requests go through the same authorized query, and its snapshots
        // drive both the screen and downloaded file. Never rerun report arithmetic.
        var first = await sender.Send(new GetSalesReportQuery(filter), ct);
        if (first.TotalDocumentCount > MaximumExportDocuments)
            return BadRequest($"التقرير كبير جدًا للتصدير دفعة واحدة. الحد {MaximumExportDocuments:N0} مستند؛ قلل المدة أو استخدم التصفية.");

        var rows = new List<SalesReportDocumentDto>(checked((int)first.TotalDocumentCount));
        rows.AddRange(first.Documents);
        for (var page = 2; rows.Count < first.TotalDocumentCount; page++)
        {
            ct.ThrowIfCancellationRequested();
            var next = await sender.Send(new GetSalesReportQuery(filter with { PageNumber = page }), ct);
            if (next.TotalDocumentCount != first.TotalDocumentCount)
                return Conflict("تغير عدد المستندات أثناء التصدير. أعد المحاولة للحصول على بيانات متسقة.");
            if (next.Documents.Count == 0) break;
            rows.AddRange(next.Documents);
        }
        var csv = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);
        var bytes = csv ? SalesReportExportFormatter.Csv(rows) : SalesReportExportFormatter.Excel(rows);
        var ext = csv ? "csv" : "xlsx";
        var contentType = csv ? "text/csv; charset=utf-8" :
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(bytes, contentType, $"SalesReport_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.{ext}");
    }
}
