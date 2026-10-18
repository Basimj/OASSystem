using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using OAS.Contracts.Accounting.Reports;

namespace OAS.API.Printing;

/// <summary>Exports the values returned by AP report DTOs verbatim, not a second balance engine.</summary>
public static class SupplierReportFileFormatter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    public sealed record Table(string[] Headers, IReadOnlyList<object?[]> Rows);

    public static Table Statement(SupplierStatementDto report, string currency) => new(
        ["تاريخ الترحيل", "تاريخ المستند", "نوع المستند", "مرجع المستند", "رقم القيد", "البيان",
         "مدين - " + currency, "دائن - " + currency, "رصيد جاري - " + currency, "عملة العملية", "مدين العملية", "دائن العملية"],
        [
            ["رصيد افتتاحي", "", "", "", "", "الرصيد الافتتاحي", null, null, report.OpeningLiabilityBaseAmount, "", null, null],
            ..report.Movements.Select(x => new object?[] {
                x.PostingDate.ToString("yyyy-MM-dd"), x.DocumentDate.ToString("yyyy-MM-dd"),
                x.SourceDocumentType ?? "", x.Reference, x.JournalNumber, x.Description,
                x.DebitBaseAmount, x.CreditBaseAmount, x.RunningLiabilityBaseAmount,
                x.TransactionCurrencyCode ?? "", x.TransactionDebitAmount, x.TransactionCreditAmount }),
            ["إجمالي الفترة", "", "", "", "", "إجماليات الفترة (عملة الأساس)",
             report.PeriodDebitBaseAmount, report.PeriodCreditBaseAmount, report.ClosingLiabilityBaseAmount, "", null, null]
        ]);

    public static Table Balances(SupplierBalancesDto report, string currency) => new(
        ["كود المورد", "اسم المورد", "كود الحساب", "اسم الحساب", "مدين حتى التاريخ - " + currency,
         "دائن حتى التاريخ - " + currency, "الرصيد - " + currency],
        [
            ..report.Rows.Select(x => new object?[] {
                x.SupplierCode, x.SupplierName, x.AccountCode, x.AccountName,
                x.DebitToDateBaseAmount, x.CreditToDateBaseAmount, x.LiabilityBalanceBaseAmount }),
            ["إجمالي الالتزامات", "", "", "", null, null, report.TotalLiabilityBaseAmount],
            ["إجمالي الرصيد المدين", "", "", "", null, null, report.TotalDebitBalanceBaseAmount],
            ["إجمالي الرصيد الدائن", "", "", "", null, null, report.TotalCreditBalanceBaseAmount]
        ]);

    public static Table Aging(SupplierAgingReportDto report, string currency) => new(
        ["كود المورد", "اسم المورد", "نوع المستند", "رقم المستند", "تاريخ القيد", "تاريخ الاستحقاق",
         "فئة التأخر", "المبلغ الأصلي - " + currency, "المتبقي - " + currency, "عملة المستند", "المبلغ بعملة المستند"],
        [
            ..report.Lines.Select(x => new object?[] {
                x.SupplierCode, x.SupplierName, x.SourceType, x.Reference,
                x.PostingDate.ToString("yyyy-MM-dd"), x.DueDate?.ToString("yyyy-MM-dd") ?? "",
                x.Bucket, x.OriginalBaseAmount, x.OutstandingBaseAmount,
                x.CurrencyCode ?? "", x.OriginalDocumentAmount }),
            ["الإجمالي", "", "", "", "", "", "", null, report.TotalOutstanding, "", null],
            ["غير مستحق", "", "", "", "", "", "", null, report.NotDue, "", null],
            ["0 - 30 يوم", "", "", "", "", "", "", null, report.Days0To30, "", null],
            ["31 - 60 يوم", "", "", "", "", "", "", null, report.Days31To60, "", null],
            ["61 - 90 يوم", "", "", "", "", "", "", null, report.Days61To90, "", null],
            ["أكثر من 90 يوم", "", "", "", "", "", "", null, report.DaysOver90, "", null],
            ["غير محدد", "", "", "", "", "", "", null, report.Unknown, "", null],
            ["مدفوعات غير مخصصة", "", "", "", "", "", "", null, report.UnappliedPayments, "", null],
            ["ائتمانات غير مطبقة", "", "", "", "", "", "", null, report.UnappliedCredits, "", null]
        ]);

    public static byte[] Csv(Table table)
    {
        var csv = new StringBuilder();
        WriteCsvRow(csv, table.Headers.Select(x => (object?)x));
        foreach (var row in table.Rows)
            WriteCsvRow(csv, row);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    private static void WriteCsvRow(StringBuilder builder, IEnumerable<object?> values)
    {
        builder.AppendLine(string.Join(",", values.Select(value =>
        {
            var s = value switch
            {
                null => "",
                decimal d => d.ToString("0.############################", Invariant),
                IFormattable f => f.ToString(null, Invariant),
                _ => value.ToString() ?? ""
            };
            // Avoid spreadsheet formula injection when CSV is opened with Excel.
            if (s.Length > 0 && "=+-@\t\r".Contains(s.TrimStart().FirstOrDefault()) &&
                value is string) s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        })));
    }

    public static byte[] Excel(Table table, string sheetName = "تقرير الموردين")
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add(sheetName);
        sheet.RightToLeft = true;
        for (var i = 0; i < table.Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = table.Headers[i];
        for (var row = 0; row < table.Rows.Count; row++)
        {
            for (var col = 0; col < table.Rows[row].Length; col++)
            {
                var cell = sheet.Cell(row + 2, col + 1);
                var value = table.Rows[row][col];
                switch (value)
                {
                    case decimal number: cell.Value = number; break;
                    case int number32: cell.Value = number32; break;
                    case long number64: cell.Value = number64; break;
                    case null: break;
                    default: cell.Value = value.ToString() ?? ""; break;
                }
            }
        }
        sheet.Range(1, 1, 1, table.Headers.Length).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, table.Headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#342D52");
        sheet.Range(1, 1, 1, table.Headers.Length).Style.Font.FontColor = XLColor.White;
        sheet.Columns().AdjustToContents(8, 45);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }
}
