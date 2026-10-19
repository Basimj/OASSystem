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
    }

    [Test]
    public void BaseCurrencyRounding_IsDeterministic()
    {
        var totals = SalesReportArithmetic.Compose(100.123456m, 10.111111m, 0m, 0m, 33.123456m, 1, 1);
        Assert.That(totals.NetRevenueBase, Is.EqualTo(90.0123m));
    }
}
