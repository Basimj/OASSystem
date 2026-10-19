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
            x.CustomerId.ToString("D"), x.CurrencyCode,
            x.Totals.InvoiceRevenueBase, x.Totals.ReturnedRevenueBase,
            x.Totals.NetRevenueBase, x.Totals.InvoiceDiscountsBase,
            x.Totals.NetSalesTaxBase, x.Totals.NetCogsBase,
            x.Totals.GrossProfitBase, x.Totals.GrossMarginPercent
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

    public static byte[] Excel(IEnumerable<SalesReportDocumentDto> documents)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("تقرير المبيعات");
        sheet.RightToLeft = true;
        for (var col = 0; col < Headers.Length; col++) sheet.Cell(1, col + 1).Value = Headers[col];
        var rowNumber = 2;
        foreach (var values in Rows(documents))
        {
            for (var col = 0; col < values.Length; col++)
            {
                var cell = sheet.Cell(rowNumber, col + 1);
                switch (values[col])
                {
                    case decimal amount: cell.Value = amount; break;
                    case int number: cell.Value = number; break;
                    default: cell.Value = values[col]?.ToString() ?? ""; break;
                }
            }
            rowNumber++;
        }
        sheet.Range(1, 1, 1, Headers.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents(10, 45);
        sheet.SheetView.FreezeRows(1);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }
}
