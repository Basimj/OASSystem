using NUnit.Framework;
using OAS.Application.Sales.Reports;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesReportArithmeticTests
{
    [Test]
    public void NetSalesAndProfit_SubtractPostedReturnsAndReversedCogs()
    {
        // Sale: net100000 cost70000. Return: net20000 and stock restored with cost14000.
        var totals = SalesReportArithmetic.Compose(100000m, 20000m, 5000m, 12000m, 56000m, 1, 1);
        Assert.Multiple(() =>
        {
            Assert.That(totals.NetRevenueBase, Is.EqualTo(80000m));
            Assert.That(totals.NetCogsBase, Is.EqualTo(56000m));
            Assert.That(totals.GrossProfitBase, Is.EqualTo(24000m));
            Assert.That(totals.GrossMarginPercent, Is.EqualTo(30m));
        });
    }

    [Test]
    public void EmptyOrFullyReturnedSales_HasNoDivideByZero()
    {
        var totals = SalesReportArithmetic.Compose(100m, 100m, 0m, 0m, 0m, 1, 1);
        Assert.That(totals.GrossMarginPercent, Is.Zero);
        Assert.That(totals.IsGrossMarginAvailable, Is.False);
    }

    [Test]
    public void ReconcileHeader_AllocatesPositiveAndNegativeResidualBeforeFilters()
    {
        var a = SalesReportArithmetic.ReconcileHeader(new decimal[] { 10.01m, 20.01m, 30.01m }, 60.02m);
        var b = SalesReportArithmetic.ReconcileHeader(new decimal[] { 10m, 20m, 30m }, 60.01m);
        Assert.Multiple(() => {
            Assert.That(a, Is.EqualTo(new decimal[] { 10.01m, 20.01m, 30m }));
            Assert.That(b.Sum(), Is.EqualTo(60.01m));
            Assert.That(b[2], Is.EqualTo(30.01m));
        });
    }

    [Test]
    public void NegativeRevenueOrUnknownCost_DoesNotDisplayMargin()
    {
        var negative = SalesReportArithmetic.Compose(0m, 100m, 0m, 0m, -50m, 0, 1);
        var missing = SalesReportArithmetic.Compose(100m, 0m, 0m, 0m, 0m, 1, 0) with { MissingCostCount = 1 };
        Assert.Multiple(() => {
            Assert.That(negative.IsGrossMarginAvailable, Is.False);
            Assert.That(missing.IsGrossMarginAvailable, Is.False);
            Assert.That(missing.IsProfitAvailable, Is.False);
        });
    }

    [Test]
    public void HistoricalBaseCurrencyScope_UsesActualCodesWithoutMixing()
    {
        var available = SalesReportCurrencyScope.Available(new string?[]{ " usd ", "YER", "USD", null, "" });
        Assert.Multiple(() => {
            Assert.That(available, Is.EqualTo(new[]{"USD", "YER"}));
            Assert.That(SalesReportCurrencyScope.Select("*", "YER", available), Is.EqualTo("YER"));
            Assert.That(SalesReportCurrencyScope.Select("*", "SAR", available), Is.EqualTo("USD"));
            Assert.That(SalesReportCurrencyScope.Select(" usd ", "YER", available), Is.EqualTo("USD"));
            Assert.That(SalesReportCurrencyScope.Select("SAR", "YER", available), Is.EqualTo("SAR"));
            Assert.That(SalesReportCurrencyScope.Select("*", "YER", Array.Empty<string>()), Is.EqualTo("YER"));
        });
    }

    [Test]
    public void BaseCurrencyRounding_IsDeterministic()
    {
        var totals = SalesReportArithmetic.Compose(100.123456m, 10.111111m, 0m, 0m, 33.123456m, 1, 1);
        Assert.That(totals.NetRevenueBase, Is.EqualTo(90.0123m));
    }
}
