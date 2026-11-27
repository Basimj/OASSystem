using NUnit.Framework;
using OAS.Contracts.Accounting.Reports;
using OAS.Application.Accounting.Reports.Queries.GetCashBankReport;

namespace OAS.Tests.Accounting.Application.Reports;

[TestFixture]
public sealed class CashBankReportMathTests
{
    [Test]
    public void ForeignCurrencyUsesStoredAmountsInsteadOfCurrentExchangeRate()
    {
        var currency = Guid.NewGuid(); var baseId = Guid.NewGuid();
        var amounts = CashBankReportMath.Native(currency, baseId, currency, 100m, 0m, 55000m, 0m);
        Assert.That(amounts.Debit, Is.EqualTo(100m));
        Assert.That(amounts.Credit, Is.Zero);
    }
    [Test]
    public void LegacyBaseCurrencyFallbackRequiresKnownMatchingCurrency()
    {
        var currency = Guid.NewGuid();
        Assert.That(CashBankReportMath.Native(currency, currency, null, null, null, 250m, 20m), Is.EqualTo(((decimal?)250m, (decimal?)20m)));
        Assert.That(CashBankReportMath.Native(currency, null, null, null, null, 250m, 20m).Debit, Is.Null);
        Assert.That(CashBankReportMath.Native(currency, Guid.NewGuid(), null, null, null, 250m, 20m).Credit, Is.Null);
    }
    [Test]
    public void UnknownNativeAmountsDoNotBecomeZeroOrPartialTotals()
    {
        var id = Guid.NewGuid();
        Assert.That(CashBankReportMath.Native(id, id, id, 100m, null, 100m, 0m).Debit, Is.Null);
        Assert.That(CashBankReportMath.Sum([100m, null, 20m]), Is.Null);
        Assert.That(CashBankReportMath.Sum([]), Is.Zero);
    }
    [Test]
    public void OpeningPlusIncomingMinusOutgoingAndInternalTransferNet()
    {
        var first = CashBankReportMath.Sum([500m, 200m, -100m]);
        var second = CashBankReportMath.Sum([0m, 100m]);
        Assert.That(CashBankReportMath.Sum([first, second]), Is.EqualTo(700m));
        Assert.That(CashBankReportMath.Sum([-100m, 98m]), Is.EqualTo(-2m));
    }
    [Test]
    public void ValidatorRejectsInvalidDatesAndUnknownMode()
    {
        var validator = new GetCashBankReportQueryValidator();
        var start = new DateOnly(2026, 11, 1);
        Assert.That(validator.Validate(new GetCashBankReportQuery(new(start, start.AddDays(-1)))).IsValid, Is.False);
        Assert.That(validator.Validate(new GetCashBankReportQuery(new(start, start, "invalid"))).IsValid, Is.False);
        Assert.That(validator.Validate(new GetCashBankReportQuery(new(start, start, "transfers"))).IsValid, Is.True);
    }
}
