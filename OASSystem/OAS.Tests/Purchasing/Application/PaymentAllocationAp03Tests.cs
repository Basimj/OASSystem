using NUnit.Framework;
using OAS.Application.Purchasing.PurchaseInvoices.Services;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PaymentAllocationAp03Tests
{
    [Test]
    public void CreateLineAllocation_WithIdempotencyKey_PersistsNormalizedKey()
    {
        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            AllocationTargetDocumentType.PurchaseInvoice,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "USD",
            25m,
            1500m,
            37500m,
            DateTime.UtcNow,
            targetBaseAllocatedAmount: 37500m,
            targetDocumentAmount: 25m,
            idempotencyKey: "  AP-03-RETRY-1  ");

        Assert.That(allocation.IdempotencyKey, Is.EqualTo("AP-03-RETRY-1"));
        Assert.That(allocation.IsReversed, Is.False);
    }

    [Test]
    public void Reverse_IsAudited_AndCannotBeAppliedTwiceOrEdited()
    {
        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            AllocationTargetDocumentType.PurchaseInvoice,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "USD",
            25m,
            1500m,
            37500m,
            DateTime.UtcNow,
            targetBaseAllocatedAmount: 37500m,
            targetDocumentAmount: 25m);
        var reversedAt = new DateTime(2026, 10, 17, 15, 30, 0, DateTimeKind.Utc);

        allocation.Reverse(reversedAt, "reviewer", "incorrect target invoice");

        Assert.Multiple(() =>
        {
            Assert.That(allocation.IsReversed, Is.True);
            Assert.That(allocation.ReversedAtUtc, Is.EqualTo(reversedAt));
            Assert.That(allocation.ReversedBy, Is.EqualTo("reviewer"));
            Assert.That(allocation.ReversalReason, Is.EqualTo("incorrect target invoice"));
        });
        Assert.Throws<InvalidOperationException>(() =>
            allocation.Reverse(reversedAt, "reviewer", "again"));
        Assert.Throws<InvalidOperationException>(() =>
            allocation.UpdateAllocatedAmount(20m, 30000m, 30000m, 20m));
    }

    [Test]
    public async Task Balance_ReversedPaymentAllocation_ReopensInvoiceOutstanding()
    {
        var supplierId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(supplierId, currencyId, 100m, 1500m);
        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            AllocationTargetDocumentType.PurchaseInvoice,
            invoice.Id,
            currencyId,
            "USD",
            100m,
            1600m,
            160000m,
            DateTime.UtcNow,
            targetBaseAllocatedAmount: 150000m,
            targetDocumentAmount: 100m);
        allocation.Reverse(DateTime.UtcNow, "reviewer", "wrong invoice");

        var service = new PurchaseInvoiceBalanceService(
            new FakeRepository<PurchaseInvoice, Guid>([invoice]),
            new FakeRepository<PaymentAllocation, Guid>([allocation]),
            new FakeRepository<SupplierCreditApplication, Guid>());

        var balance = await service.GetAsync(invoice);

        Assert.Multiple(() =>
        {
            Assert.That(balance.PaidAmount, Is.Zero);
            Assert.That(balance.AppliedPaymentBaseAmount, Is.Zero);
            Assert.That(balance.OutstandingAmount, Is.EqualTo(100m));
            Assert.That(balance.OutstandingBaseAmount, Is.EqualTo(150000m));
            Assert.That(balance.RealizedExchangeDifferenceBase, Is.Zero);
        });
    }

    private static PurchaseInvoice CreatePostedInvoice(
        Guid supplierId,
        Guid currencyId,
        decimal total,
        decimal rate)
    {
        var invoice = PurchaseInvoice.Create(
            Guid.NewGuid(),
            $"PI-{Guid.NewGuid():N}"[..20],
            null,
            supplierId,
            new DateOnly(2026, 10, 17),
            new DateOnly(2026, 10, 17),
            currencyId,
            rate,
            new DateOnly(2026, 10, 17),
            TaxCalculationMode.Exclusive,
            null);
        invoice.AddLine(PurchaseInvoiceLine.Create(
            Guid.NewGuid(),
            invoice.Id,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "VAR",
            "Variant",
            1m,
            total,
            0m,
            0m,
            TaxCalculationMode.Exclusive,
            rate));
        invoice.Confirm(DateTimeOffset.UtcNow, "tester");
        invoice.MarkPosted(Guid.NewGuid(), DateTimeOffset.UtcNow, "tester");
        return invoice;
    }
}
