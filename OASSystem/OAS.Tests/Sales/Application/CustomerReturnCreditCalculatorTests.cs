using NUnit.Framework;
using OAS.Application.Sales.Returns.Services;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class CustomerReturnCreditCalculatorTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid InvoiceId = Guid.NewGuid();
    private static readonly Guid CurrencyId = Guid.NewGuid();
    private static readonly Guid BaseCurrencyId = Guid.NewGuid();

    [Test]
    public void PaidInvoice_Return30_ProducesCredit30_WithoutChangingOriginalPayment()
    {
        var items = Calc(100000m, 100000m, Row(1, 30000m));
        Assert.Multiple(() =>
        {
            Assert.That(items.Single().AppliedAgainstOriginalInvoice, Is.Zero);
            Assert.That(items.Single().EstimatedUnappliedCredit, Is.EqualTo(30000m));
            Assert.That(items.Single().EstimatedUnappliedBaseCredit, Is.EqualTo(30000m));
        });
    }

    [Test]
    public void PartPaidInvoice_Return30_FirstReducesRemaining40_NoCredit()
    {
        var items = Calc(100000m, 60000m, Row(1, 30000m));
        Assert.That(items.Single().EstimatedUnappliedCredit, Is.Zero);
        Assert.That(items.Single().AppliedAgainstOriginalInvoice, Is.EqualTo(30000m));
    }

    [Test]
    public void PartialPayment_MultipleReturns_UsesOutstandingBeforeCredit()
    {
        var items = Calc(100000m, 60000m, Row(1, 20000m), Row(2, 30000m));
        Assert.Multiple(() =>
        {
            Assert.That(items[0].EstimatedUnappliedCredit, Is.Zero);
            Assert.That(items[1].AppliedAgainstOriginalInvoice, Is.EqualTo(20000m));
            Assert.That(items[1].EstimatedUnappliedCredit, Is.EqualTo(10000m));
        });
    }

    [Test]
    public void ForeignCurrency_ReturnCredit_UsesReturnHistoricalBaseNotCurrentFx()
    {
        var items = Calc(100m, 100m, Row(1, 30m, 45000m));
        Assert.That(items.Single().EstimatedUnappliedBaseCredit, Is.EqualTo(45000m));
    }

    [Test]
    public void InvalidOverpayment_ThrowsInsteadOfProducingRefundableCredit()
        => Assert.Throws<InvalidOperationException>(() => Calc(100m, 120m, Row(1, 10m)));

    [Test]
    public void ReturnsAboveOriginalInvoice_ThrowsInsteadOfManufacturingCredit()
        => Assert.Throws<InvalidOperationException>(() => Calc(100m, 100m, Row(1, 110m)));

    private static OAS.Application.Sales.Returns.Services.CustomerReturnCreditCalculator.PostedReturn Row(
        int day, decimal amount, decimal? baseAmount = null) => new(
        Guid.NewGuid(), $"SR-{day}", new DateOnly(2026, 10, day),
        new DateTimeOffset(2026, 10, day, 12, 0, 0, TimeSpan.Zero),
        amount, baseAmount ?? amount);

    private static IReadOnlyList<OAS.Contracts.Sales.Returns.CustomerReturnCreditItemDto> Calc(
        decimal total, decimal allocated,
        params OAS.Application.Sales.Returns.Services.CustomerReturnCreditCalculator.PostedReturn[] rows) =>
        OAS.Application.Sales.Returns.Services.CustomerReturnCreditCalculator.Calculate(
            CustomerId, InvoiceId, "SI-001", CurrencyId, "USD",
            BaseCurrencyId, "YER", total, allocated, 2, rows);
}
