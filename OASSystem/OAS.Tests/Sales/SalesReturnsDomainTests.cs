using NUnit.Framework;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales;

[TestFixture]
public sealed class SalesReturnsDomainTests
{
    [Test]
    public void InvoiceLine_ReturnReservation_CannotExceedSoldQuantity_AndCanBeReleased()
    {
        var line = CreateInventoryLine(Guid.NewGuid(), 2m);

        line.ReserveReturnQuantity(1.25m);
        Assert.That(line.ReturnedQuantity, Is.EqualTo(1.25m));
        Assert.That(line.RemainingReturnableQuantity, Is.EqualTo(0.75m));

        Assert.Throws<DomainException>(() => line.ReserveReturnQuantity(0.76m));

        line.ReleaseReturnQuantity(0.25m);
        Assert.That(line.ReturnedQuantity, Is.EqualTo(1m));
        Assert.That(line.RemainingReturnableQuantity, Is.EqualTo(1m));
    }

    [Test]
    public void SalesReturn_ConfirmAndPost_PreservesSourceAndAccountingTotals()
    {
        var currencyId = Guid.NewGuid();
        var salesReturn = SalesReturn.Create(
            Guid.NewGuid(),
            "SR-2026-000001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 3),
            new DateOnly(2026, 10, 3),
            currencyId,
            "YER",
            2,
            1m,
            new DateOnly(2026, 10, 3),
            ExchangeRateType.Accounting,
            ExchangeRateSource.System,
            currencyId,
            "YER",
            2,
            "customer return");

        salesReturn.AddLine(SalesReturnLine.Create(
            Guid.NewGuid(),
            salesReturn.Id,
            1,
            Guid.NewGuid(),
            SalesLineType.Frame,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "FR-001",
            "Frame",
            1m,
            90m,
            9m,
            99m,
            90m,
            9m,
            99m,
            45m,
            45m,
            SalesReturnCondition.Resellable,
            SalesReturnStockDisposition.Restock));

        salesReturn.Confirm(DateTimeOffset.UtcNow, "tester");
        var journalId = Guid.NewGuid();
        salesReturn.MarkPosted(journalId, DateTimeOffset.UtcNow, "poster");

        Assert.That(salesReturn.Status, Is.EqualTo(SalesReturnStatus.Posted));
        Assert.That(salesReturn.TotalAmount, Is.EqualTo(99m));
        Assert.That(salesReturn.BaseTotalAmount, Is.EqualTo(99m));
        Assert.That(salesReturn.JournalEntryId, Is.EqualTo(journalId));
    }

    [Test]
    public void SalesReturnLine_NeedsInspection_MustRemainInQuarantine()
    {
        Assert.Throws<DomainException>(() => SalesReturnLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), SalesLineType.Lens,
            Guid.NewGuid(), Guid.NewGuid(), "LN-001", "Custom lens",
            1m, 100m, 0m, 100m, 100m, 0m, 100m, 50m, 50m,
            SalesReturnCondition.NeedsInspection, SalesReturnStockDisposition.Restock));
    }

    [Test]
    public void SalesReturnLine_Quarantine_RestoresInventoryAsset_ButIsNotDirectRestock()
    {
        var line = SalesReturnLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), SalesLineType.Lens,
            Guid.NewGuid(), Guid.NewGuid(), "LN-001", "Custom lens",
            1m, 100m, 0m, 100m, 100m, 0m, 100m, 50m, 50m,
            SalesReturnCondition.NeedsInspection, SalesReturnStockDisposition.Quarantine);

        Assert.That(line.RestoresInventoryAsset, Is.True);
        Assert.That(line.StockDisposition, Is.EqualTo(SalesReturnStockDisposition.Quarantine));
    }

    [Test]
    public void QuarantinedLine_Inspection_CanReleaseOnlyResellableStockToDestinationWarehouse()
    {
        var quarantineWarehouseId = Guid.NewGuid();
        var sellableWarehouseId = Guid.NewGuid();
        var line = SalesReturnLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), SalesLineType.Lens,
            Guid.NewGuid(), quarantineWarehouseId, "LN-002", "Custom lens",
            1m, 100m, 0m, 100m, 100m, 0m, 100m, 50m, 50m,
            SalesReturnCondition.NeedsInspection, SalesReturnStockDisposition.Quarantine);

        var inspectedAt = DateTimeOffset.UtcNow;
        line.RecordInspection(
            SalesReturnCondition.Resellable,
            SalesReturnStockDisposition.Restock,
            inspectedAt,
            "quality-user",
            sellableWarehouseId);

        Assert.Multiple(() =>
        {
            Assert.That(line.ReturnCondition, Is.EqualTo(SalesReturnCondition.Resellable));
            Assert.That(line.StockDisposition, Is.EqualTo(SalesReturnStockDisposition.Restock));
            Assert.That(line.WarehouseId, Is.EqualTo(sellableWarehouseId), "After release the return line tracks the warehouse that physically holds the item.");
            Assert.That(line.InspectedAtUtc, Is.EqualTo(inspectedAt));
            Assert.That(line.InspectedBy, Is.EqualTo("quality-user"));
        });
    }

    [Test]
    public void QuarantinedLine_DamagedInspection_CannotBeReleasedToSellableStock()
    {
        var line = SalesReturnLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), SalesLineType.Lens,
            Guid.NewGuid(), Guid.NewGuid(), "LN-003", "Damaged lens",
            1m, 100m, 0m, 100m, 100m, 0m, 100m, 50m, 50m,
            SalesReturnCondition.NeedsInspection, SalesReturnStockDisposition.Quarantine);

        Assert.Throws<DomainException>(() => line.RecordInspection(
            SalesReturnCondition.Damaged,
            SalesReturnStockDisposition.Restock,
            DateTimeOffset.UtcNow,
            "quality-user",
            Guid.NewGuid()));
    }

    [Test]
    public void SalesReturn_Confirm_RejectsInventoryLineWithoutQualityDecision()
    {
        var currencyId = Guid.NewGuid();
        var salesReturn = SalesReturn.Create(
            Guid.NewGuid(), "SR-2026-000002", Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 3),
            currencyId, "YER", 2, 1m, new DateOnly(2026, 10, 3),
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            currencyId, "YER", 2, null);
        salesReturn.AddLine(SalesReturnLine.Create(
            Guid.NewGuid(), salesReturn.Id, 1, Guid.NewGuid(), SalesLineType.Frame,
            Guid.NewGuid(), Guid.NewGuid(), "FR-001", "Frame",
            1m, 100m, 0m, 100m, 100m, 0m, 100m, 50m, 50m));

        Assert.Throws<DomainException>(() => salesReturn.Confirm(DateTimeOffset.UtcNow, "tester"));
    }

    [Test]
    public void SalesReturn_ReturnWindowOverride_RequiresReason_AndKeepsAuditSnapshot()
    {
        var currencyId = Guid.NewGuid();
        var salesReturn = SalesReturn.Create(
            Guid.NewGuid(), "SR-2026-000003", Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 15),
            currencyId, "YER", 2, 1m, new DateOnly(2026, 11, 15),
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            currencyId, "YER", 2, null);

        Assert.Throws<DomainException>(() => salesReturn.SetReturnPolicySnapshot(
            new DateOnly(2026, 10, 1), 30, true, null, "manager"));

        salesReturn.SetReturnPolicySnapshot(
            new DateOnly(2026, 10, 1), 30, true, "استثناء معتمد", "manager");

        Assert.That(salesReturn.ReturnWindowDaysSnapshot, Is.EqualTo(30));
        Assert.That(salesReturn.ReturnWindowOverridden, Is.True);
        Assert.That(salesReturn.ReturnWindowOverrideReason, Is.EqualTo("استثناء معتمد"));
        Assert.That(salesReturn.ReturnWindowOverriddenBy, Is.EqualTo("manager"));
    }

    private static SalesInvoiceLine CreateInventoryLine(Guid invoiceId, decimal quantity) =>
        SalesInvoiceLine.Create(
            Guid.NewGuid(), invoiceId, 1, null, null,
            SalesLineType.Frame, Guid.NewGuid(), Guid.NewGuid(), "FR-001",
            "Frame", "Frame", "Piece",
            quantity, 100m, 100m,
            SalesDiscountType.None, null, null,
            null, null, false, null,
            TaxCalculationMode.Exclusive, 2, 1m, 2);
}
