using NUnit.Framework;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales;

[TestFixture]
public sealed class SalesInvoiceDomainTests
{
    [Test]
    public void Confirm_ServiceInvoice_ThenPost_WithJournal_Succeeds()
    {
        var invoice = CreateInvoice();
        invoice.AddLine(CreateServiceLine(invoice.Id, 1, 2m, 50m));

        invoice.Confirm(DateTimeOffset.UtcNow, "tester");
        invoice.SetJournalEntry(Guid.NewGuid());
        invoice.Post(DateTimeOffset.UtcNow, "tester");

        Assert.That(invoice.Status, Is.EqualTo(SalesInvoiceStatus.Posted));
        Assert.That(invoice.TotalAmount, Is.EqualTo(100m));
        Assert.That(invoice.JournalEntryId, Is.Not.Null);
    }

    [Test]
    public void Post_InventoryInvoiceWithoutCostSnapshot_IsRejected()
    {
        var invoice = CreateInvoice();
        var line = CreateInventoryLine(invoice.Id, 1, 2m, 75m);
        invoice.AddLine(line);
        invoice.Confirm(DateTimeOffset.UtcNow, "tester");
        invoice.SetJournalEntry(Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            invoice.Post(DateTimeOffset.UtcNow, "tester"));

        invoice.SetLineCostSnapshot(line.Id, 40m, 80m);
        invoice.Post(DateTimeOffset.UtcNow, "tester");

        Assert.That(invoice.Status, Is.EqualTo(SalesInvoiceStatus.Posted));
        Assert.That(line.TotalCostSnapshot, Is.EqualTo(80m));
    }

    [Test]
    public void ConfirmedInvoice_CannotBeRepriced()
    {
        var invoice = CreateInvoice();
        var line = CreateServiceLine(invoice.Id, 1, 1m, 100m);
        invoice.AddLine(line);
        invoice.Confirm(DateTimeOffset.UtcNow, "tester");

        Assert.Throws<DomainException>(() => invoice.UpdateLinePricing(
            line.Id,
            1m,
            100m,
            90m,
            SalesDiscountType.None,
            null,
            null));
    }

    private static SalesInvoice CreateInvoice()
    {
        var currencyId = Guid.NewGuid();
        return SalesInvoice.Create(
            Guid.NewGuid(),
            "SI-TEST-001",
            Guid.NewGuid(),
            null,
            null,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 1),
            currencyId,
            "YER",
            "ر.ي",
            2,
            1m,
            new DateOnly(2026, 10, 1),
            ExchangeRateType.Accounting,
            ExchangeRateSource.System,
            TaxCalculationMode.Exclusive,
            SalesPaymentTermType.Immediate,
            0,
            currencyId,
            "YER",
            2,
            "test");
    }

    private static SalesInvoiceLine CreateServiceLine(
        Guid invoiceId,
        int lineNumber,
        decimal quantity,
        decimal unitPrice) =>
        SalesInvoiceLine.Create(
            Guid.NewGuid(), invoiceId, lineNumber, null, null,
            SalesLineType.Service, null, null, null,
            "خدمة فحص", "خدمة فحص", "خدمة",
            quantity, unitPrice, unitPrice,
            SalesDiscountType.None, null, null,
            null, null, false, null,
            TaxCalculationMode.Exclusive, 2, 1m, 2);

    private static SalesInvoiceLine CreateInventoryLine(
        Guid invoiceId,
        int lineNumber,
        decimal quantity,
        decimal unitPrice) =>
        SalesInvoiceLine.Create(
            Guid.NewGuid(), invoiceId, lineNumber, null, null,
            SalesLineType.Frame, Guid.NewGuid(), Guid.NewGuid(), "FR-001",
            "إطار", "إطار", "قطعة",
            quantity, unitPrice, unitPrice,
            SalesDiscountType.None, null, null,
            null, null, false, null,
            TaxCalculationMode.Exclusive, 2, 1m, 2);
}
