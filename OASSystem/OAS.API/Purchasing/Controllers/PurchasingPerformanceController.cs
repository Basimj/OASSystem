using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.Performance;
using OAS.Contracts.Purchasing.Performance;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/purchasing/performance")]
public sealed class PurchasingPerformanceController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PurchasingPerformanceReportDto>> Get(
        [FromQuery] PurchasingPerformanceFilterDto filter, CancellationToken cancellationToken)
    {
        var error = Validate(filter);
        if (error is not null) return BadRequest(error);
        return Ok(await sender.Send(new GetPurchasingPerformanceQuery(filter), cancellationToken));
    }

    /// <summary>Exports precisely the server-authorized paged DTO. Hard capped to prevent unbounded reads.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] PurchasingPerformanceFilterDto filter,
        [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var error = Validate(filter);
        if (error is not null) return BadRequest(error);
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("صيغة التصدير يجب أن تكون csv أو xlsx.");

        var first = await sender.Send(new GetPurchasingPerformanceQuery(filter with { PageNumber = 1, PageSize = 100 }), cancellationToken);
        const int maxRows = 10000;
        if (first.Page.TotalCount > maxRows)
            return BadRequest($"التصدير محدود بـ {maxRows:N0} صف. ضيّق الفترة أو اختر المورد أو الصنف.");
        var rows = new List<PurchasingPerformanceRowDto>(first.Page.Items);
        var pages = (int)Math.Ceiling(first.Page.TotalCount / 100d);
        for (var n = 2; n <= pages; n++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = await sender.Send(new GetPurchasingPerformanceQuery(filter with { PageNumber = n, PageSize = 100 }), cancellationToken);
            rows.AddRange(next.Page.Items);
        }

        var name = $"PurchasingPerformance-{filter.Kind}-{filter.FromDate:yyyyMMdd}-{filter.ToDate:yyyyMMdd}";
        Response.Headers.CacheControl = "no-store, private";
        return string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
            ? File(ToCsv(rows), "text/csv; charset=utf-8", name + ".csv")
            : File(ToExcel(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name + ".xlsx");
    }

    private static string? Validate(PurchasingPerformanceFilterDto filter)
    {
        if (filter.FromDate > filter.ToDate) return "تاريخ البداية يجب ألا يتجاوز تاريخ النهاية.";
        if (filter.PageNumber < 1 || filter.PageNumber > 1000000 || filter.PageSize is < 1 or > 100) return "الصفحة أو حجمها غير صالح.";
        if (filter.Kind is null || filter.Kind.ToLowerInvariant() is not ("orders" or "receipts" or "invoices" or "returns" or "prices"))
            return "اختر نوع تقرير معروفًا.";
        return null;
    }

    private static readonly string[] ColumnNames =
    [
        "المصدر", "رقم المستند", "التاريخ", "الحالة", "المورد", "الصنف", "الوحدة", "العملة",
        "الكمية", "المطلوب", "المقبول المرحل", "المفوتر المرحل", "المرتجع المرحل", "المتبقي للتوريد",
        "المقبول غير المفوتر (كمية)", "سعر الوحدة", "قيمة المستند (عملة المستند)",
        "قيمة بالعملة الأساسية", "فارق السعر (عملة الفاتورة)", "تاريخ الوعد", "زمن التوريد بالأيام", "متأخر"
    ];

    private static string[] Cells(PurchasingPerformanceRowDto r) =>
    [
        r.Kind, r.DocumentCode, r.DocumentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), r.Status,
        r.SupplierName, r.ProductName, r.UnitName ?? "", r.CurrencyCode, F(r.Quantity),
        F(r.OrderedQuantity), F(r.AcceptedQuantity), F(r.InvoicedQuantity), F(r.ReturnedQuantity),
        F(r.OpenQuantity), F(r.UninvoicedAcceptedQuantity), F(r.UnitPrice), F(r.DocumentAmount),
        F(r.BaseAmount), F(r.PriceVariance), r.ExpectedDeliveryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
        r.LeadTimeDays?.ToString(CultureInfo.InvariantCulture) ?? "", r.IsLate ? "نعم" : "لا"
    ];

    private static string F(decimal? x) => x?.ToString("0.####", CultureInfo.InvariantCulture) ?? "";
    private static string CsvCell(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

    private static byte[] ToCsv(IReadOnlyList<PurchasingPerformanceRowDto> rows)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Join(",", ColumnNames.Select(CsvCell)));
        foreach (var row in rows) text.AppendLine(string.Join(",", Cells(row).Select(CsvCell)));
        return new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }

    private static byte[] ToExcel(IReadOnlyList<PurchasingPerformanceRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Purchasing Performance");
        sheet.RightToLeft = true;
        for (var col = 0; col < ColumnNames.Length; col++) sheet.Cell(1, col + 1).Value = ColumnNames[col];
        for (var i = 0; i < rows.Count; i++)
        {
            var values = Cells(rows[i]);
            for (var col = 0; col < values.Length; col++) sheet.Cell(i + 2, col + 1).Value = values[col];
        }
        sheet.Range(1, 1, 1, ColumnNames.Length).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        using var buffer = new MemoryStream();
        workbook.SaveAs(buffer);
        return buffer.ToArray();
    }
}
