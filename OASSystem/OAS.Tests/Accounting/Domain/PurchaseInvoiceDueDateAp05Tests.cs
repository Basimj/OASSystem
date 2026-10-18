using NUnit.Framework;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class PurchaseInvoiceDueDateAp05Tests
{
    private static readonly DateOnly InvoiceDate = new(2026, 2, 10);

    [Test]
    public void New_Invoice_Starts_With_Unverified_Due_Date()
    {
        var invoice = NewInvoice();
        Assert.That(invoice.DueDate, Is.Null);
        Assert.That(invoice.PaymentTermDaysSnapshot, Is.Null);
        Assert.That(invoice.DueDateProvenance, Is.EqualTo(PurchaseInvoiceDueDateProvenance.Unknown));
    }

    [Test]
    public void Derived_Payment_Terms_Are_Saved_As_Snapshot()
    {
        var invoice = NewInvoice();
        var due = InvoiceDate.AddDays(30);
        invoice.SetPaymentTerms(30, due, PurchaseInvoiceDueDateProvenance.DerivedAtInvoice);

        Assert.Multiple(() =>
        {
            Assert.That(invoice.DueDate, Is.EqualTo(due));
            Assert.That(invoice.PaymentTermDaysSnapshot, Is.EqualTo(30));
            Assert.That(invoice.DueDateProvenance, Is.EqualTo(PurchaseInvoiceDueDateProvenance.DerivedAtInvoice));
        });
    }

    [Test]
    public void Historical_Unverified_Invoice_Requires_Explicit_Verification()
    {
        var invoice = NewInvoice();
        invoice.VerifyHistoricalDueDate(InvoiceDate.AddDays(45), 45);
        Assert.That(invoice.DueDate, Is.EqualTo(InvoiceDate.AddDays(45)));
        Assert.That(invoice.DueDateProvenance, Is.EqualTo(PurchaseInvoiceDueDateProvenance.VerifiedHistoricalBackfill));
    }

    [Test]
    public void Unknown_Provenance_Cannot_Be_Assigned_Fabricated_Terms()
    {
        var invoice = NewInvoice();
        Assert.Throws<OAS.Domain.Exceptions.DomainException>(() =>
            invoice.SetPaymentTerms(30, InvoiceDate.AddDays(30), PurchaseInvoiceDueDateProvenance.Unknown));
    }

    private static PurchaseInvoice NewInvoice() => PurchaseInvoice.Create(
        Guid.NewGuid(), "PIN-AP05-0001", "SUP-LEGACY-1", Guid.NewGuid(),
        InvoiceDate, InvoiceDate, Guid.NewGuid(), 1m, InvoiceDate,
        TaxCalculationMode.Exclusive, null);
}
