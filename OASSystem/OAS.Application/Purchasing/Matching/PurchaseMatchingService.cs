using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Purchasing.Entities;
using DomainInvoiceStatus = OAS.Domain.Purchasing.Enums.PurchaseInvoiceStatus;

namespace OAS.Application.Purchasing.Matching;

public sealed class PurchaseMatchingService(
    IPurchaseMatchingDataPort dataPort,
    IPurchasingSettingsPort settingsPort) : IPurchaseMatchingService
{
    public async Task<PurchaseMatchEvaluation> EvaluateAsync(
        PurchaseInvoice invoice,
        IReadOnlyList<PurchaseInvoiceMatchAllocationRequest> allocations,
        CancellationToken cancellationToken = default)
    {
        if (invoice.Status is DomainInvoiceStatus.Posted or DomainInvoiceStatus.Cancelled)
            throw new ConflictException("purchasing_invoice_match_locked", "لا يمكن تشغيل المطابقة على فاتورة مرحلة أو ملغاة.");
        if (allocations.Count == 0)
            throw Validation("Allocations", "purchase_match_allocations_required");

        var invoiceLines = invoice.Lines.ToDictionary(x => x.Id);

        foreach (var item in allocations)
        {
            if (item.MatchedQuantity <= 0)
                throw Validation("MatchedQuantity", "purchase_match_quantity_must_be_positive");
            if (!invoiceLines.ContainsKey(item.PurchaseInvoiceLineId))
                throw Validation("PurchaseInvoiceLineId", "purchase_match_invoice_line_not_found");
        }

        var requestedByInvoiceLine = allocations
            .GroupBy(x => x.PurchaseInvoiceLineId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.MatchedQuantity));

        foreach (var line in invoice.Lines)
        {
            requestedByInvoiceLine.TryGetValue(line.Id, out var matched);
            if (matched <= 0m)
                throw new ConflictException("purchasing_invoice_line_match_required", "يجب تخصيص كمية مستلمة واحدة على الأقل لكل سطر فاتورة قبل اعتماد المطابقة.");
            if (matched > line.Quantity)
                throw new ConflictException("purchasing_match_quantity_exceeds_invoice", "لا يمكن تخصيص كمية مستلمة أكبر من كمية سطر فاتورة المورد.");
        }

        var tolerances = await settingsPort.GetMatchingTolerancesAsync(cancellationToken);
        var candidates = new Dictionary<Guid, PurchaseReceiptMatchCandidate>();
        foreach (var receiptLineId in allocations.Select(x => x.PurchaseReceiptLineId).Distinct())
        {
            candidates[receiptLineId] = await dataPort.GetReceiptCandidateAsync(receiptLineId, invoice.Id, cancellationToken)
                ?? throw new NotFoundException("PurchaseReceiptLine", receiptLineId);
        }

        foreach (var group in allocations.GroupBy(x => x.PurchaseReceiptLineId))
        {
            var candidate = candidates[group.Key];
            var requested = group.Sum(x => x.MatchedQuantity);
            if (requested > candidate.AvailableQuantity)
                throw new ConflictException("purchasing_match_quantity_exceeds_available_receipt", "لا يمكن مطابقة كمية أكبر من الكمية المقبولة غير المطابقة والمتاحة بعد المرتجعات.");
        }

        foreach (var group in allocations.GroupBy(x => x.PurchaseInvoiceLineId))
        {
            var line = invoiceLines[group.Key];
            if (!line.PurchaseOrderLineId.HasValue)
                throw new ConflictException("purchasing_match_po_line_required", "يجب ربط سطر فاتورة المورد بسطر أمر شراء قبل تشغيل المطابقة الثلاثية.");

            var lineCandidates = group.Select(x => candidates[x.PurchaseReceiptLineId]).ToArray();
            var first = lineCandidates[0];
            if (lineCandidates.Any(x => x.PurchaseUnitId != first.PurchaseUnitId || x.UnitConversionFactor != first.UnitConversionFactor))
                throw new ConflictException("purchasing_match_unit_mismatch", "لا يمكن مطابقة سطر فاتورة واحد مع استلامات تستخدم وحدات شراء أو معاملات تحويل مختلفة.");
            if (line.PurchaseOrderLineId.HasValue && lineCandidates.Any(x => x.PurchaseOrderLineId != line.PurchaseOrderLineId.Value))
                throw new ConflictException("purchasing_match_po_line_mismatch", "سطر أمر الشراء لا يطابق الاستلام المحدد.");
        }

        var results = new List<PurchaseMatchAllocationEvaluation>(allocations.Count);

        foreach (var lineGroup in allocations.GroupBy(x => x.PurchaseInvoiceLineId))
        {
            var line = invoiceLines[lineGroup.Key];
            var matchedLineQuantity = lineGroup.Sum(x => x.MatchedQuantity);
            var lineQuantityVariance = Math.Round(line.Quantity - matchedLineQuantity, 3);
            var quantityStatus = lineQuantityVariance == 0m
                ? PurchaseMatchStatus.Matched
                : Math.Abs(lineQuantityVariance) <= tolerances.QuantityTolerance
                    ? PurchaseMatchStatus.WithinTolerance
                    : PurchaseMatchStatus.RequiresApproval;

            var firstAllocation = true;
            foreach (var item in lineGroup)
            {
                var candidate = candidates[item.PurchaseReceiptLineId];

                if (candidate.ReceiptStatus != PurchaseReceiptStatus.Posted)
                    throw new ConflictException("purchasing_match_receipt_not_posted", "لا يمكن المطابقة مع استلام غير مرحل.");
                if (candidate.SupplierId != invoice.SupplierId)
                    throw new ConflictException("purchasing_match_supplier_mismatch", "المورد في الاستلام لا يطابق مورد الفاتورة.");
                if (candidate.CurrencyId != invoice.CurrencyId)
                    throw new ConflictException("purchasing_match_currency_mismatch", "عملة أمر الشراء/الاستلام لا تطابق عملة فاتورة المورد.");
                if (candidate.ProductVariantId != line.ProductVariantId)
                    throw new ConflictException("purchasing_match_product_mismatch", "المنتج في الاستلام لا يطابق سطر الفاتورة.");
                if (line.PurchaseOrderLineId.HasValue && line.PurchaseOrderLineId.Value != candidate.PurchaseOrderLineId)
                    throw new ConflictException("purchasing_match_po_line_mismatch", "سطر أمر الشراء لا يطابق الاستلام المحدد.");

                var ratio = item.MatchedQuantity / line.Quantity;
                var invoiceNet = Math.Round(line.NetAmount * ratio, 4);
                var invoiceNetBase = Math.Round(line.BaseNetAmount * ratio, 4);
                var invoiceTaxBase = Math.Round(line.BaseTaxAmount * ratio, 4);
                var matchedBaseQuantity = Math.Round(item.MatchedQuantity * candidate.UnitConversionFactor, 3);
                var receiptCostBase = Math.Round(candidate.ReceiptUnitCostBase * matchedBaseQuantity, 4);

                // Three-way price matching compares the supplier invoice price with the PO net price.
                // Accounting PPV remains invoice base subtotal versus GRNI/receipt cost at posting time.
                var purchaseOrderNetBase = Math.Round(candidate.PurchaseOrderNetUnitPriceBase * item.MatchedQuantity, 4);
                var priceVarianceBase = Math.Round(invoiceNetBase - purchaseOrderNetBase, 4);
                // Use the PO tax amount that was already calculated by the central tax convention.
                // This keeps matching correct for both tax-exclusive and tax-inclusive modes.
                var expectedTaxBase = Math.Round(candidate.PurchaseOrderTaxUnitAmountBase * item.MatchedQuantity, 4);
                var taxVarianceBase = Math.Round(invoiceTaxBase - expectedTaxBase, 4);

                var priceStatus = priceVarianceBase == 0m
                    ? PurchaseMatchStatus.Matched
                    : Math.Abs(priceVarianceBase) <= tolerances.PriceTolerance
                        ? PurchaseMatchStatus.WithinTolerance
                        : PurchaseMatchStatus.RequiresApproval;
                var taxStatus = taxVarianceBase == 0m
                    ? PurchaseMatchStatus.Matched
                    : Math.Abs(taxVarianceBase) <= tolerances.TaxTolerance
                        ? PurchaseMatchStatus.WithinTolerance
                        : PurchaseMatchStatus.RequiresApproval;

                // Quantity variance is an invoice-line variance. Persist it once on the first
                // allocation so it is not duplicated when one invoice line is split across receipts.
                var persistedQuantityVariance = firstAllocation ? lineQuantityVariance : 0m;
                var allocationStatus = MaxStatus(
                    firstAllocation ? quantityStatus : PurchaseMatchStatus.Matched,
                    priceStatus,
                    taxStatus);

                results.Add(new PurchaseMatchAllocationEvaluation(
                    line.Id,
                    candidate.PurchaseReceiptLineId,
                    candidate.ReceiptCode,
                    item.MatchedQuantity,
                    invoiceNet,
                    persistedQuantityVariance,
                    priceVarianceBase,
                    taxVarianceBase,
                    allocationStatus,
                    tolerances.QuantityTolerance,
                    tolerances.PriceTolerance,
                    tolerances.TaxTolerance,
                    receiptCostBase,
                    purchaseOrderNetBase,
                    expectedTaxBase,
                    invoiceNetBase,
                    invoiceTaxBase));

                firstAllocation = false;
            }
        }

        var requiresApproval = results.Any(x => x.Status == PurchaseMatchStatus.RequiresApproval);
        var overallStatus = requiresApproval
            ? PurchaseMatchStatus.RequiresApproval
            : results.Any(x => x.Status == PurchaseMatchStatus.WithinTolerance)
                ? PurchaseMatchStatus.WithinTolerance
                : PurchaseMatchStatus.Matched;

        var totalReceiptCostBase = Math.Round(results.Sum(x => x.ReceiptCostBaseAmount), 4);
        return new PurchaseMatchEvaluation(
            overallStatus,
            requiresApproval,
            results,
            totalReceiptCostBase,
            invoice.BaseSubtotal,
            invoice.BaseTaxAmount,
            Math.Round(invoice.BaseSubtotal - totalReceiptCostBase, 4));
    }

    private static PurchaseMatchStatus MaxStatus(params PurchaseMatchStatus[] statuses)
    {
        if (statuses.Any(x => x == PurchaseMatchStatus.RequiresApproval)) return PurchaseMatchStatus.RequiresApproval;
        if (statuses.Any(x => x == PurchaseMatchStatus.WithinTolerance)) return PurchaseMatchStatus.WithinTolerance;
        return PurchaseMatchStatus.Matched;
    }

    private static RequestValidationException Validation(string field, string code) =>
        new(new Dictionary<string, string[]> { [field] = [code] });
}
