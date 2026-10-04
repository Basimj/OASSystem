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
            45m));

        salesReturn.Confirm(DateTimeOffset.UtcNow, "tester");
        var journalId = Guid.NewGuid();
        salesReturn.MarkPosted(journalId, DateTimeOffset.UtcNow, "poster");

        Assert.That(salesReturn.Status, Is.EqualTo(SalesReturnStatus.Posted));
        Assert.That(salesReturn.TotalAmount, Is.EqualTo(99m));
        Assert.That(salesReturn.BaseTotalAmount, Is.EqualTo(99m));
        Assert.That(salesReturn.JournalEntryId, Is.EqualTo(journalId));
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
