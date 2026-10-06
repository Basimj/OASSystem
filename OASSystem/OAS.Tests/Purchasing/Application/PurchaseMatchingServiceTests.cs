using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Matching;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Enums;
using ContractMatchStatus = OAS.Contracts.Purchasing.Enums.PurchaseMatchStatus;
using ContractReceiptStatus = OAS.Contracts.Purchasing.Enums.PurchaseReceiptStatus;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseMatchingServiceTests
{
    [Test]
    public async Task ExactThreeWayMatch_IsMatched_WithZeroVariances()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 10m)]);

        var allocation = result.Allocations.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.OverallStatus, Is.EqualTo(ContractMatchStatus.Matched));
            Assert.That(result.RequiresApproval, Is.False);
            Assert.That(allocation.QuantityVariance, Is.EqualTo(0m));
            Assert.That(allocation.PriceVarianceAmount, Is.EqualTo(0m));
            Assert.That(allocation.TaxVarianceAmount, Is.EqualTo(0m));
            Assert.That(allocation.PurchaseOrderNetBaseAmount, Is.EqualTo(1000m));
            Assert.That(allocation.ExpectedTaxBaseAmount, Is.EqualTo(150m));
        });
    }

    [Test]
    public async Task QuantityVariance_InsideTolerance_IsWithinTolerance()
    {
        var fixture = CreateFixture(invoiceQuantity: 10m, availableReceiptQuantity: 9.5m, quantityTolerance: 0.5m);

        var result = await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 9.5m)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.OverallStatus, Is.EqualTo(ContractMatchStatus.WithinTolerance));
            Assert.That(result.RequiresApproval, Is.False);
            Assert.That(result.Allocations.Single().QuantityVariance, Is.EqualTo(0.5m));
            Assert.That(result.Allocations.Single().QuantityTolerance, Is.EqualTo(0.5m));
        });
    }

    [Test]
    public async Task QuantityVariance_AboveTolerance_RequiresApproval()
    {
        var fixture = CreateFixture(invoiceQuantity: 10m, availableReceiptQuantity: 9m, quantityTolerance: 0.5m);

        var result = await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 9m)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.OverallStatus, Is.EqualTo(ContractMatchStatus.RequiresApproval));
            Assert.That(result.RequiresApproval, Is.True);
            Assert.That(result.Allocations.Single().QuantityVariance, Is.EqualTo(1m));
        });
    }

    [Test]
    public async Task PriceVariance_InsideTolerance_IsWithinTolerance()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 101m, invoiceTaxRate: 0m, poTaxUnitAmountBase: 0m, priceTolerance: 10m);

        var result = await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 10m)]);

        var allocation = result.Allocations.Single();
        Assert.Multiple(() =>
        {
            Assert.That(allocation.PriceVarianceAmount, Is.EqualTo(10m));
            Assert.That(allocation.PriceTolerance, Is.EqualTo(10m));
            Assert.That(result.OverallStatus, Is.EqualTo(ContractMatchStatus.WithinTolerance));
            Assert.That(result.RequiresApproval, Is.False);
        });
    }

    [Test]
    public async Task PriceVariance_AboveTolerance_RequiresApproval()
    {
        var fixture = CreateFixture(invoiceUnitPrice: 101m, invoiceTaxRate: 0m, poTaxUnitAmountBase: 0m, priceTolerance: 9.99m);

        var result = await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 10m)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Allocations.Single().PriceVarianceAmount, Is.EqualTo(10m));
            Assert.That(result.OverallStatus, Is.EqualTo(ContractMatchStatus.RequiresApproval));
            Assert.That(result.RequiresApproval, Is.True);
        });
    }

    [Test]
    public async Task TaxVariance_UsesPurchaseOrderTaxAmount_AndHonorsTolerance()
    {
        var fixture = CreateFixture(invoiceTaxRate: 15m, poTaxUnitAmountBase: 14m, taxTolerance: 10m);

        var result = await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 10m)]);

        var allocation = result.Allocations.Single();
        Assert.Multiple(() =>
        {
            Assert.That(allocation.ExpectedTaxBaseAmount, Is.EqualTo(140m));
            Assert.That(allocation.InvoiceTaxBaseAmount, Is.EqualTo(150m));
            Assert.That(allocation.TaxVarianceAmount, Is.EqualTo(10m));
            Assert.That(result.OverallStatus, Is.EqualTo(ContractMatchStatus.WithinTolerance));
        });
    }

    [Test]
    public void InvoiceLineWithoutPurchaseOrderLine_CannotRunThreeWayMatching()
    {
        var fixture = CreateFixture(linkToPurchaseOrder: false);

        var ex = Assert.ThrowsAsync<ConflictException>(async () => await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 10m)]));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void DifferentUnitConversionForSameInvoiceLine_IsRejected()
    {
        var poLineId = Guid.NewGuid();
        var fixture = CreateFixture(invoiceQuantity: 10m, availableReceiptQuantity: 5m, purchaseOrderLineId: poLineId);
        var secondReceiptId = Guid.NewGuid();
        var second = fixture.Candidate with
        {
            PurchaseReceiptLineId = secondReceiptId,
            ReceiptCode = "GR-TEST-MATCH-2",
            AvailableQuantity = 5m,
            UnitConversionFactor = 2m
        };
        fixture.DataPort.Add(second);

        var ex = Assert.ThrowsAsync<ConflictException>(async () => await fixture.Service.EvaluateAsync(
            fixture.Invoice,
            [
                new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, fixture.ReceiptLineId, 5m),
                new PurchaseInvoiceMatchAllocationRequest(fixture.InvoiceLineId, secondReceiptId, 5m)
            ]));

        Assert.That(ex, Is.Not.Null);
    }

    private static Fixture CreateFixture(
        decimal invoiceQuantity = 10m,
        decimal availableReceiptQuantity = 10m,
        decimal quantityTolerance = 0m,
        decimal invoiceUnitPrice = 100m,
        decimal invoiceTaxRate = 15m,
        decimal poNetUnitPriceBase = 100m,
        decimal poTaxUnitAmountBase = 15m,
        decimal priceTolerance = 0m,
        decimal taxTolerance = 0m,
        bool linkToPurchaseOrder = true,
        Guid? purchaseOrderLineId = null)
    {
        var supplierId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var poLineId = purchaseOrderLineId ?? Guid.NewGuid();
        var productVariantId = Guid.NewGuid();
        var receiptLineId = Guid.NewGuid();
        var purchaseUnitId = Guid.NewGuid();
        var invoice = PurchaseInvoice.Create(
            Guid.NewGuid(),
            "PI-TEST-MATCH",
            "SUP-INV-MATCH",
            supplierId,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            currencyId,
            1m,
            new DateOnly(2026, 10, 5),
            TaxCalculationMode.Exclusive,
            null);

        var effectivePoLineId = linkToPurchaseOrder ? poLineId : null;
        var invoiceLine = PurchaseInvoiceLine.Create(
            Guid.NewGuid(),
            invoice.Id,
            1,
            effectivePoLineId,
            productVariantId,
            "P-001",
            "Test product",
            invoiceQuantity,
            invoiceUnitPrice,
            0m,
            invoiceTaxRate,
            TaxCalculationMode.Exclusive,
            1m);
        invoice.AddLine(invoiceLine);

        var candidate = new PurchaseReceiptMatchCandidate(
            receiptLineId,
            "GR-TEST-MATCH",
            poLineId,
            productVariantId,
            supplierId,
            currencyId,
            purchaseUnitId,
            1m,
            availableReceiptQuantity,
            0m,
            availableReceiptQuantity,
            100m,
            100m,
            poNetUnitPriceBase,
            poTaxUnitAmountBase,
            15m,
            ContractReceiptStatus.Posted);

        var dataPort = new FakeMatchingDataPort(candidate);
        var settings = new FakeSettingsPort(new PurchaseMatchingTolerances(quantityTolerance, priceTolerance, taxTolerance));
        return new Fixture(new PurchaseMatchingService(dataPort, settings), invoice, invoiceLine.Id, receiptLineId, candidate, dataPort);
    }

    private sealed record Fixture(
        PurchaseMatchingService Service,
        PurchaseInvoice Invoice,
        Guid InvoiceLineId,
        Guid ReceiptLineId,
        PurchaseReceiptMatchCandidate Candidate,
        FakeMatchingDataPort DataPort);

    private sealed class FakeMatchingDataPort(params PurchaseReceiptMatchCandidate[] candidates) : IPurchaseMatchingDataPort
    {
        private readonly Dictionary<Guid, PurchaseReceiptMatchCandidate> _candidates = candidates.ToDictionary(x => x.PurchaseReceiptLineId);

        public void Add(PurchaseReceiptMatchCandidate candidate) => _candidates[candidate.PurchaseReceiptLineId] = candidate;

        public Task<PurchaseReceiptMatchCandidate?> GetReceiptCandidateAsync(Guid purchaseReceiptLineId, Guid purchaseInvoiceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_candidates.TryGetValue(purchaseReceiptLineId, out var candidate) ? candidate : null);
    }

    private sealed class FakeSettingsPort(PurchaseMatchingTolerances tolerances) : IPurchasingSettingsPort
    {
        public Task<PurchaseMatchingTolerances> GetMatchingTolerancesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(tolerances);
    }
}
