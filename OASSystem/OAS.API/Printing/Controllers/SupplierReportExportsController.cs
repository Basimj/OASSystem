using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.API.Printing;

namespace OAS.API.Printing.Controllers;

/// <summary>Exports the same authorized accounting DTOs used in the on-screen reports.</summary>
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/reports/suppliers")]
public sealed class SupplierReportExportsController(
    SupplierReportSnapshotService snapshots, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{supplierId:guid}/statement/export")]
    public async Task<IActionResult> Statement(Guid supplierId,
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] string format = "xlsx", CancellationToken ct = default)
    {
        if (fromDate > toDate) return BadRequest("تاريخ البداية أكبر من النهاية.");
        if (!ValidFormat(format)) return BadRequest("صيغة التصدير يجب أن تكون csv أو xlsx.");
        OAS.Contracts.Accounting.Reports.SupplierStatementDto report;
        try { report = await snapshots.StatementAsync(supplierId, fromDate, toDate, ct); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(ex.Message); }
        return Export(SupplierReportFileFormatter.Statement(report, BaseCurrency()),
            $"SupplierStatement-{supplierId:N}-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}", format);
    }

    [HttpGet("balances/export")]
    public async Task<IActionResult> Balances([FromQuery] DateOnly asOfDate,
        [FromQuery] string? search = null, [FromQuery] string format = "xlsx", CancellationToken ct = default)
    {
        if (!ValidFormat(format)) return BadRequest("صيغة التصدير يجب أن تكون csv أو xlsx.");
        OAS.Contracts.Accounting.Reports.SupplierBalancesDto report;
        try { report = await snapshots.BalancesAsync(asOfDate, search, ct); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(ex.Message); }
        return Export(SupplierReportFileFormatter.Balances(report, BaseCurrency()),
            $"SupplierBalances-{asOfDate:yyyyMMdd}", format);
    }

    [HttpGet("aging/export")]
    public async Task<IActionResult> Aging([FromQuery] DateOnly asOfDate,
        [FromQuery] Guid? supplierId = null, [FromQuery] string? search = null, [FromQuery] string format = "xlsx",
        CancellationToken ct = default)
    {
        if (!ValidFormat(format)) return BadRequest("صيغة التصدير يجب أن تكون csv أو xlsx.");
        OAS.Contracts.Accounting.Reports.SupplierAgingReportDto report;
        try { report = await snapshots.AgingAsync(asOfDate, supplierId, search, ct); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(ex.Message); }
        return Export(SupplierReportFileFormatter.Aging(report, BaseCurrency()),
            $"SupplierAging-{asOfDate:yyyyMMdd}", format);
    }

    private static bool ValidFormat(string format) =>
        string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase);

    private string BaseCurrency() =>
        configuration["Accounting:BaseCurrencyCode"] ?? "العملة الأساسية";

    private FileContentResult Export(SupplierReportFileFormatter.Table table, string filename, string format)
    {
        Response.Headers.CacheControl = "no-store, private";
        var csv = format.Equals("csv", StringComparison.OrdinalIgnoreCase);
        return File(csv ? SupplierReportFileFormatter.Csv(table) : SupplierReportFileFormatter.Excel(table),
            csv ? "text/csv; charset=utf-8" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            filename + (csv ? ".csv" : ".xlsx"));
    }
}
