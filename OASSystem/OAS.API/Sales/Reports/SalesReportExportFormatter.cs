using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using OAS.Contracts.Sales.Reports;

namespace OAS.API.Sales.Reports;

/// <summary>Presentation only. Never performs an independent financial calculation.</summary>
public static class SalesReportExportFormatter
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private static readonly string[] Headers =
    [
        "التاريخ", "نوع المستند", "رقم المستند", "العميل", "العملة الأصلية",
        "إيراد الفاتورة - أساس", "المرتجع - أساس", "صافي المبيعات - أساس",
        "خصومات الفواتير - أساس", "الضريبة الصافية - أساس", "التكلفة الصافية - أساس",
        "الربح الإجمالي - أساس", "هامش الربح %"
    ];

    private static IEnumerable<object?[]> Rows(IEnumerable<SalesReportDocumentDto> documents)
        => documents.Select(x => new object?[]
        {
            x.PostingDate.ToString("yyyy-MM-dd"), x.DocumentKind, x.DocumentCode,
            x.CustomerName ?? x.CustomerId.ToString("D"), x.CurrencyCode,
            x.Totals.InvoiceRevenueBase, x.Totals.ReturnedRevenueBase,
            x.Totals.NetRevenueBase, x.Totals.InvoiceDiscountsBase,
            x.Totals.NetSalesTaxBase, x.Totals.IsProfitAvailable ? x.Totals.NetCogsBase : null,
            x.Totals.IsProfitAvailable ? x.Totals.GrossProfitBase : null, MarginValue(x.Totals)
        });

    public static byte[] Csv(IEnumerable<SalesReportDocumentDto> documents)
    {
        var sb = new StringBuilder();
        AddRow(sb, Headers.Cast<object?>());
        foreach (var row in Rows(documents)) AddRow(sb, row);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static void AddRow(StringBuilder sb, IEnumerable<object?> cells)
    {
        sb.AppendLine(string.Join(",", cells.Select(value =>
        {
            var str = value is IFormattable f ? f.ToString(null, Culture) : value?.ToString() ?? "";
            // Prevent CSV formula execution in Excel when importing untrusted document text.
            if (value is string && str.TrimStart() is { Length: > 0 } trimmed &&
                "=+-@\t\r".Contains(trimmed[0])) str = "'" + str;
            return "\"" + str.Replace("\"", "\"\"") + "\"";
        })));
    }

    /// <summary>Legacy documents-only overload retained for existing callers/tests.</summary>
    public static byte[] Excel(IEnumerable<SalesReportDocumentDto> documents) => Excel(null, documents);

    /// <summary>Professional workbook: financial summary, analysis, then all exported document rows.</summary>
    public static byte[] Excel(SalesReportDto? report, IEnumerable<SalesReportDocumentDto> documents)
    {
        using var book = new XLWorkbook();
        if (report is not null)
        {
            BuildSummary(book, report);
            BuildAnalysis(book, report);
        }

        var sheet = book.Worksheets.Add("المستندات المرحلة");
        sheet.RightToLeft = true;
        for (var col = 0; col < Headers.Length; col++) sheet.Cell(1, col + 1).Value = Headers[col];
        var rowNumber = 2;
        foreach (var values in Rows(documents))
        {
            for (var col = 0; col < values.Length; col++)
                SetValue(sheet.Cell(rowNumber, col + 1), values[col]);
            rowNumber++;
        }
        TableFormatting(sheet, Headers.Length, rowNumber - 1, 1);
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(12, 45);
        if (rowNumber > 2)
            sheet.Range(2, 6, rowNumber - 1, Headers.Length).Style.NumberFormat.Format = "#,##0.00;[Red](#,##0.00)";

        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildSummary(XLWorkbook book, SalesReportDto report)
    {
        var sheet = book.Worksheets.Add("الملخص المالي");
        sheet.RightToLeft = true;
        Title(sheet, "تقرير المبيعات والربحية — الملخص المالي", 2);
        sheet.Cell(3, 1).Value = "من تاريخ"; sheet.Cell(3, 2).Value = report.Filter.FromDate.ToString("yyyy-MM-dd");
        sheet.Cell(4, 1).Value = "إلى تاريخ"; sheet.Cell(4, 2).Value = report.Filter.ToDate.ToString("yyyy-MM-dd");
        sheet.Cell(5, 1).Value = "التجميع"; sheet.Cell(5, 2).Value = GroupName(report.Filter.GroupBy);
        sheet.Cell(6, 1).Value = "العملة الأساسية"; sheet.Cell(6, 2).Value = report.BaseCurrencyCode;
        sheet.Cell(8, 1).Value = "المؤشر"; sheet.Cell(8, 2).Value = "القيمة";
        var t = report.Totals;
        var metrics = new (string Title, decimal Value)[]
        {
            ("إيرادات الفواتير", t.InvoiceRevenueBase),
            ("المرتجعات", t.ReturnedRevenueBase),
            ("صافي الإيرادات", t.NetRevenueBase),
            ("خصومات الفواتير", t.InvoiceDiscountsBase),
            ("ضريبة المبيعات الصافية", t.NetSalesTaxBase),
            ("تكلفة المبيعات الصافية", t.NetCogsBase),
            ("الربح الإجمالي", t.GrossProfitBase),
            ("هامش الربح %", t.GrossMarginPercent),
            ("عدد الفواتير", t.InvoiceCount),
            ("عدد المرتجعات", t.ReturnCount)
        };
        for (var i = 0; i < metrics.Length; i++)
        {
            sheet.Cell(i + 9, 1).Value = metrics[i].Title;
            sheet.Cell(i + 9, 2).Value = metrics[i].Value;
        }
        if (!t.IsProfitAvailable)
        {
            sheet.Cell(14, 2).Clear(XLClearOptions.Contents);
            sheet.Cell(15, 2).Clear(XLClearOptions.Contents);
        }
        SetValue(sheet.Cell(16, 2), MarginValue(t));
        sheet.Cell(20, 1).Value = "الربح الإجمالي قبل المصروفات الإدارية والتشغيلية؛ إيراد الفواتير بعد الخصومات.";
        if (!t.IsGrossMarginAvailable)
            sheet.Cell(21, 1).Value = !t.IsProfitAvailable
                ? "هامش الربح غير معبّر لعدم اكتمال بيانات التكلفة."
                : "هامش الربح غير معبّر لأن صافي الإيرادات صفر أو سالب؛ قد تشمل الفترة مرتجعات عن فواتير سابقة.";
        TableFormatting(sheet, 2, 18, 8);
        sheet.Range(9, 2, 18, 2).Style.NumberFormat.Format = "#,##0.00;[Red](#,##0.00)";
        sheet.Column(1).Width = 48;
        sheet.Column(2).Width = 26;
    }

    private static void BuildAnalysis(XLWorkbook book, SalesReportDto report)
    {
        var sheet = book.Worksheets.Add("تحليل الأداء");
        sheet.RightToLeft = true;
        Title(sheet, $"تحليل الأداء حسب {GroupName(report.Filter.GroupBy)}", 7);
        sheet.Cell(3, 1).Value = $"الفترة: {report.Filter.FromDate:yyyy-MM-dd} إلى {report.Filter.ToDate:yyyy-MM-dd} — العملة: {report.BaseCurrencyCode}";
        if (report.BreakdownTruncated)
            sheet.Cell(4, 1).Value = "تنبيه: تحليل الأداء يعرض المجموعات المتاحة في نتيجة التقرير؛ الملخص المالي يشمل جميع العمليات ضمن نطاق التقرير.";
        var names = new[] { "المجموعة", "إيرادات الفواتير", "المرتجعات", "صافي الإيراد", "التكلفة", "الربح الإجمالي", "الهامش %" };
        for (var col = 0; col < names.Length; col++) sheet.Cell(6, col + 1).Value = names[col];
        var row = 7;
        foreach (var item in report.Breakdown)
        {
            sheet.Cell(row, 1).Value = item.Label;
            sheet.Cell(row, 2).Value = item.Totals.InvoiceRevenueBase;
            sheet.Cell(row, 3).Value = item.Totals.ReturnedRevenueBase;
            sheet.Cell(row, 4).Value = item.Totals.NetRevenueBase;
            SetValue(sheet.Cell(row, 5), item.Totals.IsProfitAvailable ? item.Totals.NetCogsBase : null);
            SetValue(sheet.Cell(row, 6), item.Totals.IsProfitAvailable ? item.Totals.GrossProfitBase : null);
            SetValue(sheet.Cell(row, 7), MarginValue(item.Totals));
            row++;
        }
        TableFormatting(sheet, 7, row - 1, 6);
        if (row > 7) sheet.Range(7, 2, row - 1, 7).Style.NumberFormat.Format = "#,##0.00;[Red](#,##0.00)";
        sheet.Column(1).Width = 36;
        for (var i = 2; i <= 7; i++) sheet.Column(i).Width = 20;
        sheet.SheetView.FreezeRows(6);
    }

    private static void Title(IXLWorksheet sheet, string value, int columns)
    {
        sheet.Cell(1, 1).Value = value;
        var header = sheet.Range(1, 1, 1, columns);
        header.Merge();
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#20395B");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Font.Bold = true;
        header.Style.Font.FontSize = 16;
        sheet.Row(1).Height = 29;
    }

    private static void TableFormatting(IXLWorksheet sheet, int columns, int lastRow, int headerRow)
    {
        var head = sheet.Range(headerRow, 1, headerRow, columns);
        head.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE9F7");
        head.Style.Font.Bold = true;
        head.Style.Font.FontColor = XLColor.FromHtml("#172D49");
        if (lastRow > headerRow)
            sheet.Range(headerRow + 1, 1, lastRow, columns).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
    }

    private static object MarginValue(SalesReportTotalsDto totals) =>
        totals.IsGrossMarginAvailable ? totals.GrossMarginPercent : "غير معبّر";

    private static void SetValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case decimal amount: cell.Value = amount; break;
            case int number: cell.Value = number; break;
            case null: cell.Clear(XLClearOptions.Contents); break;
            default: cell.Value = value?.ToString() ?? ""; break;
        }
    }

    private static string GroupName(SalesReportGroupBy group) => group switch
    {
        SalesReportGroupBy.Month => "الشهر", SalesReportGroupBy.Customer => "العميل",
        SalesReportGroupBy.Employee => "الموظف", SalesReportGroupBy.Product => "الصنف",
        SalesReportGroupBy.Category => "التصنيف", SalesReportGroupBy.Warehouse => "المخزن",
        SalesReportGroupBy.Year => "السنة", SalesReportGroupBy.Summary => "الملخص",
        SalesReportGroupBy.Invoice => "الفاتورة", SalesReportGroupBy.Line => "البند",
        SalesReportGroupBy.ProductType => "نوع المنتج", SalesReportGroupBy.Brand => "العلامة التجارية",
        SalesReportGroupBy.Operator => "المستخدم", SalesReportGroupBy.Currency => "العملة",
        SalesReportGroupBy.PaymentPlan => "طريقة الدفع", SalesReportGroupBy.Collections => "التحصيلات",
        SalesReportGroupBy.Return => "المرتجعات", SalesReportGroupBy.Discounts => "الخصومات",
        SalesReportGroupBy.Taxes => "الضرائب", SalesReportGroupBy.Cost => "التكلفة",
        SalesReportGroupBy.Profit => "الربح", _ => "اليوم"
    };
}
