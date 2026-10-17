using NUnit.Framework;
using OAS.Application.Purchasing.PurchaseInvoices.Services;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseInvoiceBalanceServiceTests
{
    [Test]
    public async Task GetAsync_Payment60AndAppliedReturn23_OnInvoice115_ReturnsOutstanding32()
    {
        var supplierId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(supplierId, currencyId, 115m, 1m);
        var invoices = new FakeRepository<PurchaseInvoice, Guid>([invoice]);
        var allocations = new FakeRepository<PaymentAllocation, Guid>();
        var credits = new FakeRepository<SupplierCreditApplication, Guid>();

        await allocations.AddAsync(PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), null, Guid.NewGuid(), AllocationTargetDocumentType.PurchaseInvoice,
            invoice.Id, currencyId, "YER", 60m, 1m, 60m, DateTime.UtcNow, 60m,
            targetDocumentAmount: 60m));
        await credits.AddAsync(SupplierCreditApplication.Create(
            Guid.NewGuid(), Guid.NewGuid(), invoice.Id, supplierId,
            23m, 23m, 23m, DateTime.UtcNow, "tester", "balance-test"));

        var service = new PurchaseInvoiceBalanceService(invoices, allocations, credits);
        var result = await service.GetAsync(invoice);

        Assert.Multiple(() =>
        {
            Assert.That(result.PaidAmount, Is.EqualTo(60m));
            Assert.That(result.AppliedCreditInvoiceCurrencyAmount, Is.EqualTo(23m));
            Assert.That(result.OutstandingAmount, Is.EqualTo(32m));
            Assert.That(result.OutstandingBaseAmount, Is.EqualTo(32m));
            Assert.That(result.SettledAmount, Is.EqualTo(83m));
        });
    }

    [Test]
    public async Task GetAsync_Invoice100UsdAt1500_Payment100UsdAt1600_HasZeroDocumentAndBaseOutstanding()
    {
        var supplierId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(supplierId, currencyId, 100m, 1500m);
        var allocations = new FakeRepository<PaymentAllocation, Guid>();
        await allocations.AddAsync(PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), null, Guid.NewGuid(), AllocationTargetDocumentType.PurchaseInvoice,
            invoice.Id, currencyId, "USD", 100m, 1600m, 160000m, DateTime.UtcNow,
            targetBaseAllocatedAmount: 150000m,
            targetDocumentAmount: 100m));

        var service = new PurchaseInvoiceBalanceService(
            new FakeRepository<PurchaseInvoice, Guid>([invoice]),
            allocations,
            new FakeRepository<SupplierCreditApplication, Guid>());

        var result = await service.GetAsync(invoice);

        Assert.Multiple(() =>
        {
            Assert.That(result.PaidAmount, Is.EqualTo(100m));
            Assert.That(result.OutstandingAmount, Is.Zero);
            Assert.That(result.OutstandingBaseAmount, Is.Zero);
            Assert.That(result.SettledAmount, Is.EqualTo(100m));
        });
    }

    [Test]
    public async Task GetAsync_MultiplePaymentsAtDifferentRates_PreservesExplainableDocumentAndHistoricalBaseRemaining()
    {
        var supplierId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(supplierId, currencyId, 100m, 1500m);
        var allocations = new FakeRepository<PaymentAllocation, Guid>();

        await allocations.AddAsync(PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), null, Guid.NewGuid(), AllocationTargetDocumentType.PurchaseInvoice,
            invoice.Id, currencyId, "USD", 25m, 1600m, 40000m, DateTime.UtcNow,
            targetBaseAllocatedAmount: 37500m,
            targetDocumentAmount: 25m));
        await allocations.AddAsync(PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), null, Guid.NewGuid(), AllocationTargetDocumentType.PurchaseInvoice,
            invoice.Id, currencyId, "USD", 35m, 1400m, 49000m, DateTime.UtcNow,
            targetBaseAllocatedAmount: 52500m,
            targetDocumentAmount: 35m));

        var service = new PurchaseInvoiceBalanceService(
            new FakeRepository<PurchaseInvoice, Guid>([invoice]),
            allocations,
            new FakeRepository<SupplierCreditApplication, Guid>());

        var result = await service.GetAsync(invoice);

        Assert.Multiple(() =>
        {
            Assert.That(result.PaidAmount, Is.EqualTo(60m));
            Assert.That(result.OutstandingAmount, Is.EqualTo(40m));
            Assert.That(result.OutstandingBaseAmount, Is.EqualTo(60000m));
        });
    }

    [Test]
    public async Task GetAsync_ReversedCredit_DoesNotReduceOutstanding()
    {
        var supplierId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(supplierId, currencyId, 100m, 1m);
        var credit = SupplierCreditApplication.Create(
            Guid.NewGuid(), Guid.NewGuid(), invoice.Id, supplierId,
            30m, 30m, 30m, DateTime.UtcNow, "tester", "reverse-test");
        credit.Reverse(DateTime.UtcNow, "reviewer", "correction");

        var service = new PurchaseInvoiceBalanceService(
            new FakeRepository<PurchaseInvoice, Guid>([invoice]),
            new FakeRepository<PaymentAllocation, Guid>(),
            new FakeRepository<SupplierCreditApplication, Guid>([credit]));

        var result = await service.GetAsync(invoice);

        Assert.That(result.AppliedCreditBaseAmount, Is.Zero);
        Assert.That(result.OutstandingAmount, Is.EqualTo(100m));
    }

    private static PurchaseInvoice CreatePostedInvoice(Guid supplierId, Guid currencyId, decimal total, decimal rate)
    {
        var invoice = PurchaseInvoice.Create(
            Guid.NewGuid(), $"PI-{Guid.NewGuid():N}"[..20], null, supplierId,
            new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 17), currencyId, rate,
            new DateOnly(2026, 10, 17), TaxCalculationMode.Exclusive, null);
        invoice.AddLine(PurchaseInvoiceLine.Create(
            Guid.NewGuid(), invoice.Id, 1, Guid.NewGuid(), Guid.NewGuid(),
            "VAR", "Variant", 1m, total, 0m, 0m, TaxCalculationMode.Exclusive, rate));
        invoice.Confirm(DateTimeOffset.UtcNow, "tester");
        invoice.MarkPosted(Guid.NewGuid(), DateTimeOffset.UtcNow, "tester");
        return invoice;
    }
}
