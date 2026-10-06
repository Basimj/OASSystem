using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Payments;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Enums;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Purchasing.Application.Payments;

[TestFixture]
public sealed class PurchaseInvoicePaymentAllocationTargetValidatorTests
{
    private FakeRepository<PurchaseInvoice, Guid> _invoiceRepository = null!;
    private FakeRepository<PaymentAllocation, Guid> _allocationRepository = null!;
    private PurchaseInvoicePaymentAllocationTargetValidator _validator = null!;
    private Guid _supplierId;
    private Guid _currencyId;

    [SetUp]
    public void Setup()
    {
        _invoiceRepository = new();
        _allocationRepository = new();
        _validator = new PurchaseInvoicePaymentAllocationTargetValidator(_invoiceRepository, _allocationRepository);
        _supplierId = Guid.NewGuid();
        _currencyId = Guid.NewGuid();
    }

    [Test]
    public async Task ValidateAsync_WhenInvoiceIsPostedAndWithinOutstanding_Succeeds()
    {
        var invoice = CreatePostedInvoice(100m, 2m);
        await _invoiceRepository.AddAsync(invoice);

        var result = await _validator.ValidateAsync(
            invoice.Id,
            _currencyId,
            40m,
            80m,
            null,
            CancellationToken.None);

        Assert.That(result.TargetBaseAllocatedAmount, Is.EqualTo(80m));
    }

    [Test]
    public async Task ValidateAsync_WhenExistingAllocationsConsumeOutstanding_ThrowsConflict()
    {
        var invoice = CreatePostedInvoice(100m, 2m);
        await _invoiceRepository.AddAsync(invoice);
        await _allocationRepository.AddAsync(PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            AllocationTargetDocumentType.PurchaseInvoice,
            invoice.Id,
            _currencyId,
            "USD",
            80m,
            2m,
            160m,
            DateTime.UtcNow,
            160m));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await _validator.ValidateAsync(invoice.Id, _currencyId, 30m, 60m, null, CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("purchase_invoice_payment_allocation_exceeds_outstanding"));
    }

    [Test]
    public async Task ValidateAsync_WhenInvoiceIsNotPosted_ThrowsConflict()
    {
        var invoice = CreateConfirmedInvoice(100m, 2m);
        await _invoiceRepository.AddAsync(invoice);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await _validator.ValidateAsync(invoice.Id, _currencyId, 20m, 40m, null, CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("purchase_invoice_payment_invoice_not_posted"));
    }

    [Test]
    public async Task ValidateSourceAsync_WhenSupplierMatches_Succeeds()
    {
        var invoice = CreatePostedInvoice(100m, 2m);
        await _invoiceRepository.AddAsync(invoice);
        var source = new PaymentAllocationSourceContext(
            PaymentSourceType.PaymentVoucher,
            SettlementPartyType.Supplier,
            null,
            _supplierId,
            null);

        Assert.DoesNotThrowAsync(async () =>
            await _validator.ValidateSourceAsync(invoice.Id, source, CancellationToken.None));
    }

    [Test]
    public async Task ValidateSourceAsync_WhenSupplierDiffers_ThrowsConflict()
    {
        var invoice = CreatePostedInvoice(100m, 2m);
        await _invoiceRepository.AddAsync(invoice);
        var source = new PaymentAllocationSourceContext(
            PaymentSourceType.PaymentVoucher,
            SettlementPartyType.Supplier,
            null,
            Guid.NewGuid(),
            null);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await _validator.ValidateSourceAsync(invoice.Id, source, CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("purchase_invoice_payment_supplier_mismatch"));
    }

    [Test]
    public async Task ValidateSourceAsync_WhenSourceIsReceiptVoucher_ThrowsConflict()
    {
        var invoice = CreatePostedInvoice(100m, 2m);
        await _invoiceRepository.AddAsync(invoice);
        var source = new PaymentAllocationSourceContext(
            PaymentSourceType.ReceiptVoucher,
            SettlementPartyType.Supplier,
            null,
            _supplierId,
            null);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await _validator.ValidateSourceAsync(invoice.Id, source, CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("purchase_invoice_payment_source_invalid"));
    }

    private PurchaseInvoice CreatePostedInvoice(decimal unitPrice, decimal exchangeRate)
    {
        var invoice = CreateConfirmedInvoice(unitPrice, exchangeRate);
        invoice.MarkPosted(Guid.NewGuid(), DateTimeOffset.UtcNow, "tester");
        return invoice;
    }

    private PurchaseInvoice CreateConfirmedInvoice(decimal unitPrice, decimal exchangeRate)
    {
        var invoice = PurchaseInvoice.Create(
            Guid.NewGuid(),
            "PI-TEST-001",
            "SUP-INV-001",
            _supplierId,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            _currencyId,
            exchangeRate,
            new DateOnly(2026, 10, 5),
            TaxCalculationMode.Exclusive,
            null);

        invoice.AddLine(PurchaseInvoiceLine.Create(
            Guid.NewGuid(),
            invoice.Id,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "VAR-001",
            "Test variant",
            1m,
            unitPrice,
            0m,
            0m,
            TaxCalculationMode.Exclusive,
            exchangeRate));
        invoice.Confirm(DateTimeOffset.UtcNow, "tester");
        return invoice;
    }
}
