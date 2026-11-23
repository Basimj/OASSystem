using System.Text;
using ClosedXML.Excel;
using NUnit.Framework;
using OAS.API.Sales.Reports;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Reports;
using OAS.Contracts.Sales.Reports;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesReportExportTests
{
    [Test]
    public void ReportQuery_RequiresSalesViewPermission()
    {
        var filter = new SalesReportFilterDto(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        var query = new GetSalesReportQuery(filter);
        Assert.That(query.RequiredPermissions, Does.Contain(SalesPermissions.View));
    }

    [Test]
    public void Csv_UsesExactlyReportDtoValues_AndProtectsSpreadsheetFromFormulaInjection()
    {
        var totals = SalesReportArithmetic.Compose(100m, 0m, 15m, 5m, 40m, 1, 0);
        var row = new SalesReportDocumentDto("SalesInvoice", Guid.NewGuid(), "=SUM(1+1)",
            new DateOnly(2026, 10, 18), Guid.NewGuid(), null, "YER", totals);
        var bytes = SalesReportExportFormatter.Csv([row]);
        Assert.That(bytes.Take(3), Is.EqualTo(Encoding.UTF8.GetPreamble()));
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.Multiple(() =>
        {
            Assert.That(csv, Does.Contain("'=SUM(1+1)"));
            Assert.That(csv, Does.Contain("60"));
            Assert.That(csv, Does.Contain("2026-10-18"));
        });
    }

    [Test]
    public void Excel_ProducesSpreadsheetBytes()
    {
        var totals = SalesReportArithmetic.Compose(10m, 0m, 0m, 0m, 5m, 1, 0);
        var row = new SalesReportDocumentDto("SalesInvoice", Guid.NewGuid(), "SI-1",
            new DateOnly(2026, 10, 18), Guid.NewGuid(), null, "YER", totals);
        var bytes = SalesReportExportFormatter.Excel([row]);
        // Excel .xlsx is an Office Open XML ZIP package.
        Assert.That(bytes.Length, Is.GreaterThan(100));
        Assert.That(bytes.Take(2), Is.EqualTo(new byte[] { (byte)'P', (byte)'K' }));
    }
    [Test]
    public void Excel_WithReport_IncludesFinancialSummary_Breakdown_AndPostedDocuments()
    {
        var totals = SalesReportArithmetic.Compose(120m, 20m, 10m, 5m, 65m, 2, 1);
        var filter = new SalesReportFilterDto(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30),
            SalesReportGroupBy.Customer);
        var document = new SalesReportDocumentDto("SalesInvoice", Guid.NewGuid(), "SI-2026-12",
            new DateOnly(2026, 11, 18), Guid.NewGuid(), null, "YER", totals);
        var report = new SalesReportDto(filter, "YER", totals,
            [new SalesReportBreakdownDto(document.CustomerId.ToString("D"), "عميل تجريبي", totals)],
            [document], 1, 1, 25, false);

        var bytes = SalesReportExportFormatter.Excel(report, [document]);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Multiple(() =>
        {
            Assert.That(workbook.Worksheets.Select(x => x.Name), Does.Contain("الملخص المالي"));
            Assert.That(workbook.Worksheets.Select(x => x.Name), Does.Contain("تحليل الأداء"));
            Assert.That(workbook.Worksheets.Select(x => x.Name), Does.Contain("المستندات المرحلة"));
            Assert.That(workbook.Worksheet("الملخص المالي").Cell(11, 2).GetValue<decimal>(), Is.EqualTo(100m));
            Assert.That(workbook.Worksheet("تحليل الأداء").Cell(7, 1).GetString(), Is.EqualTo("عميل تجريبي"));
            Assert.That(workbook.Worksheet("المستندات المرحلة").Cell(2, 3).GetString(), Is.EqualTo("SI-2026-12"));
        });
    }

    [Test]
    public void Exports_DisplayNonMeaningfulMargin_WhenNetRevenueNonPositive()
    {
        var totals = new SalesReportTotalsDto(
            100m, 250m, -150m, 0m, 0m, -10m, -140m, 93.33m, 1, 1);
        var document = new SalesReportDocumentDto("SalesReturn", Guid.NewGuid(), "SR-TEST",
            new DateOnly(2026, 11, 23), Guid.NewGuid(), null, "YER", totals);
        var report = new SalesReportDto(
            new SalesReportFilterDto(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 23), SalesReportGroupBy.Customer),
            "YER", totals,
            [new SalesReportBreakdownDto(document.CustomerId.ToString("D"), "عميل تجريبي", totals)],
            [document], 1, 1, 25, false);

        var csv = Encoding.UTF8.GetString(SalesReportExportFormatter.Csv([document]));
        Assert.That(csv, Does.Contain("غير معبّر"));

        using var book = new XLWorkbook(new MemoryStream(SalesReportExportFormatter.Excel(report, [document])));
        Assert.Multiple(() =>
        {
            Assert.That(book.Worksheet("الملخص المالي").Cell(16, 2).GetString(), Is.EqualTo("غير معبّر"));
            Assert.That(book.Worksheet("تحليل الأداء").Cell(7, 7).GetString(), Is.EqualTo("غير معبّر"));
            Assert.That(book.Worksheet("المستندات المرحلة").Cell(2, 13).GetString(), Is.EqualTo("غير معبّر"));
        });
    }

}
