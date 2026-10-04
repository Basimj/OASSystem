using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseReturnsDomainTests
{
    [Test]
    public void ReceiptLine_ReturnReservation_CannotExceedAcceptedQuantity()
    {
        var line = PurchaseReceiptLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
            10m, 0m, 10m, 8m, 2m, 1m, 25m, null, null, null);

        line.ReserveReturnQuantity(5m);
        Assert.That(line.ReturnedQuantity, Is.EqualTo(5m));
        Assert.That(line.RemainingReturnableQuantity, Is.EqualTo(3m));
        Assert.Throws<DomainException>(() => line.ReserveReturnQuantity(3.001m));

        line.ReleaseReturnQuantity(2m);
        Assert.That(line.ReturnedQuantity, Is.EqualTo(3m));
    }

    [Test]
    public void PurchaseReturn_Post_UsesInventorySnapshotAndComputesPriceVariance()
    {
        var entity = PurchaseReturn.Create(
            Guid.NewGuid(),
            "PRT-2026-000001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 3),
            new DateOnly(2026, 10, 3),
            "supplier return");

        var line = PurchaseReturnLine.Create(
            Guid.NewGuid(), entity.Id, 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            2m, 2m, 40m, 100m, 10m);
        entity.AddLine(line);
        entity.Confirm(DateTimeOffset.UtcNow, "tester");

        line.SetInventoryCostSnapshot(45m);
        entity.RefreshInventoryTotals();
        var journalId = Guid.NewGuid();
        entity.MarkPosted(journalId, DateTimeOffset.UtcNow, "poster");

        Assert.That(entity.Status, Is.EqualTo(PurchaseReturnStatus.Posted));
        Assert.That(entity.InventoryCostBaseAmount, Is.EqualTo(90m));
        Assert.That(entity.SupplierNetBaseAmount, Is.EqualTo(100m));
        Assert.That(entity.PurchasePriceVarianceBaseAmount, Is.EqualTo(10m));
        Assert.That(entity.JournalEntryId, Is.EqualTo(journalId));
    }
}
