using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using NUnit.Framework;
using OAS.API.Printing;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Tests.Printing;

[TestFixture]
public sealed class SupplierStatementAp08Tests
{
    private static SupplierStatementMovementDto Movement(decimal debit, decimal credit,
        decimal running, string reference = "PIN-001") => new(
            Guid.NewGuid(), "JV-001", 1, new DateOnly(2026, 11, 12),
            new DateOnly(2026, 11, 11), "فاتورة", "Purchasing", "PurchaseInvoice",
            Guid.NewGuid(), null, "AccountsPayable", debit, credit,
            credit - debit, running, "YER", debit, credit, 1m, "Invoice", reference, false);

    private static SupplierStatementDto Report(IReadOnlyList<SupplierStatementMovementDto> movements) => new(
        Guid.NewGuid(), "SUP-001", "أحمد", Guid.NewGuid(), "210100", "موردون",
        new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30),
        30_000m, 140_000m, 200_000m, 90_000m, 3,
        movements, Array.Empty<SupplierMonthlyBalanceDto>(),
        new SupplierOperationalExposureDto(0m, 0, 0m, 0, 0m, 0m),
        movements.Count, 1, movements.Count);

    [Test]
    public void Print_Payload_Preserves_Api_Opening_Closing_And_RunningBalance()
    {
        var dto = Report(new[] { Movement(0, 200_000m, 230_000.125m),
            Movement(120_000m, 0, 110_000.125m), Movement(20_000m, 0, 90_000m) });
        var print = SupplierStatementPrintPayload.Create(dto, new { Name = "OAS" },
            "YER", "tester", DateTimeOffset.Parse("2026-11-30T12:00:00Z"));
        Assert.Multiple(() =>
        {
            Assert.That(print.OpeningBalance, Is.EqualTo(30_000m));
            Assert.That(print.ClosingBalance, Is.EqualTo(90_000m));
            Assert.That(print.TotalDebit, Is.EqualTo(140_000m));
            Assert.That(print.TotalCredit, Is.EqualTo(200_000m));
            Assert.That(print.Lines[0].RunningBalance, Is.EqualTo(230_000.125m));
            Assert.That(print.Lines[2].RunningBalance, Is.EqualTo(90_000m));
            Assert.That(print.BaseCurrencyCode, Is.EqualTo("YER"));
        });
    }

    [Test]
    public void Empty_Period_Preserves_Opening_And_Closing()
    {
        var dto = Report(Array.Empty<SupplierStatementMovementDto>());
        var print = SupplierStatementPrintPayload.Create(dto, new { Name = "OAS" },
            "YER", null, DateTimeOffset.UtcNow);
        Assert.That(print.Lines, Is.Empty);
        Assert.That(print.OpeningBalance, Is.EqualTo(dto.OpeningLiabilityBaseAmount));
        Assert.That(print.ClosingBalance, Is.EqualTo(dto.ClosingLiabilityBaseAmount));
        Assert.That(SupplierReportFileFormatter.Statement(dto, "YER").Rows.Count, Is.EqualTo(2));
    }

    [Test]
    public void Csv_Is_Utf8_Preserves_Fractions_And_Escapes_Formula_Text()
    {
        var dto = Report(new[] { Movement(1.125m, 0, 89_999.875m, "=3+5") });
        var bytes = SupplierReportFileFormatter.Csv(SupplierReportFileFormatter.Statement(dto, "YER"));
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.Multiple(() =>
        {
            Assert.That(csv, Does.Contain("1.125"));
            Assert.That(csv, Does.Contain("89999.875"));
            Assert.That(csv, Does.Contain("\"'=3+5\""));
            Assert.That(csv, Does.Contain("90000"));
            Assert.That(csv, Does.Contain("30000"));
        });
    }

    [Test]
    public void Excel_Writes_Numeric_Amounts_And_Arabic_Headers()
    {
        var dto = Report(new[] { Movement(0, 200_000.125m, 230_000.125m) });
        var bytes = SupplierReportFileFormatter.Excel(SupplierReportFileFormatter.Statement(dto, "YER"));
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        Assert.Multiple(() =>
        {
            Assert.That(sheet.Cell(1, 1).GetString(), Is.EqualTo("تاريخ الترحيل"));
            Assert.That(sheet.Cell(2, 9).GetValue<decimal>(), Is.EqualTo(30_000m));
            Assert.That(sheet.Cell(3, 8).GetValue<decimal>(), Is.EqualTo(200_000.125m));
            Assert.That(sheet.Cell(4, 9).GetValue<decimal>(), Is.EqualTo(90_000m));
        });
    }

    [Test]
    public void Mixed_Transaction_Currencies_Stay_Separate_From_Base_Currency_Totals()
    {
        var eur = Movement(0m, 100m, 30_100m) with
        { TransactionCurrencyCode = "EUR", TransactionCreditAmount = 80m };
        var usd = Movement(50m, 0m, 30_050m) with
        { TransactionCurrencyCode = "USD", TransactionDebitAmount = 60m };
        var dto = Report(new[] { eur, usd });
        var payload = SupplierStatementPrintPayload.Create(dto, new { Name = "OAS" },
            "YER", null, DateTimeOffset.UtcNow);
        Assert.That(payload.Lines.Select(x => x.TransactionCurrencyCode),
            Is.EquivalentTo(new[] { "EUR", "USD" }));
        Assert.That(payload.TotalCredit, Is.EqualTo(dto.PeriodCreditBaseAmount));
        Assert.That(payload.TotalDebit, Is.EqualTo(dto.PeriodDebitBaseAmount));
    }

    [Test]
    public void Long_Report_Contains_Every_Line_Without_Truncation()
    {
        var lines = Enumerable.Range(0, 450)
            .Select(i => Movement(0m, 0m, 30_000m + i)).ToArray();
        var dto = Report(lines);
        Assert.That(SupplierStatementPrintPayload.Create(dto, new { Name = "OAS" },
            "YER", "tester", DateTimeOffset.UtcNow).Lines.Count, Is.EqualTo(450));
        Assert.That(SupplierReportFileFormatter.Statement(dto, "YER").Rows.Count, Is.EqualTo(452));
    }
}
