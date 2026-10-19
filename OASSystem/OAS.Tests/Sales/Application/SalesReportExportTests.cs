using System.Text;
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
}
