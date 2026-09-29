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
        var duplicateReceiptAllocations = allocations.GroupBy(x => x.PurchaseReceiptLineId).Any(x => x.Count() > 1);
        if (duplicateReceiptAllocations)
            throw Validation("Allocations", "purchase_match_duplicate_receipt_line");

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
            if (matched != line.Quantity)
                throw new ConflictException("purchasing_invoice_line_not_fully_matched", "يجب تخصيص كامل كمية سطر الفاتورة على استلامات مرحلة قبل اعتماد المطابقة.");
        }

        var tolerances = await settingsPort.GetMatchingTolerancesAsync(cancellationToken);
        var results = new List<PurchaseMatchAllocationEvaluation>(allocations.Count);

        foreach (var item in allocations)
        {
            var line = invoiceLines[item.PurchaseInvoiceLineId];
            var candidate = await dataPort.GetReceiptCandidateAsync(item.PurchaseReceiptLineId, invoice.Id, cancellationToken)
                ?? throw new NotFoundException("PurchaseReceiptLine", item.PurchaseReceiptLineId);

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
            if (item.MatchedQuantity > candidate.AvailableQuantity)
                throw new ConflictException("purchasing_match_quantity_exceeds_available_receipt", "لا يمكن مطابقة كمية أكبر من الكمية المقبولة غير المطابقة في الاستلام.");

            var ratio = item.MatchedQuantity / line.Quantity;
            var invoiceNet = Math.Round(line.NetAmount * ratio, 4);
            var invoiceNetBase = Math.Round(line.BaseNetAmount * ratio, 4);
            var invoiceTaxBase = Math.Round(line.BaseTaxAmount * ratio, 4);
            var receiptCostBase = Math.Round(candidate.ReceiptUnitCostBase * item.MatchedQuantity, 4);
            var priceVarianceBase = Math.Round(invoiceNetBase - receiptCostBase, 4);
            var expectedTaxBase = Math.Round(invoiceNetBase * candidate.PurchaseOrderTaxRate / 100m, 4);
            var taxVarianceBase = Math.Round(invoiceTaxBase - expectedTaxBase, 4);

            var priceAbs = Math.Abs(priceVarianceBase);
            var taxAbs = Math.Abs(taxVarianceBase);
            var status = priceAbs == 0m && taxAbs == 0m
                ? PurchaseMatchStatus.Matched
                : priceAbs <= tolerances.PriceTolerance && taxAbs <= tolerances.TaxTolerance
                    ? PurchaseMatchStatus.WithinTolerance
                    : PurchaseMatchStatus.RequiresApproval;

            results.Add(new PurchaseMatchAllocationEvaluation(
                line.Id,
                candidate.PurchaseReceiptLineId,
                candidate.ReceiptCode,
                item.MatchedQuantity,
                invoiceNet,
                0m,
                priceVarianceBase,
                taxVarianceBase,
                status,
                tolerances.PriceTolerance,
                tolerances.TaxTolerance,
                receiptCostBase,
                invoiceNetBase,
                invoiceTaxBase));
        }

        var requiresApproval = results.Any(x => x.Status == PurchaseMatchStatus.RequiresApproval);
        var overallStatus = requiresApproval
            ? PurchaseMatchStatus.RequiresApproval
            : results.Any(x => x.Status == PurchaseMatchStatus.WithinTolerance)
                ? PurchaseMatchStatus.WithinTolerance
                : PurchaseMatchStatus.Matched;

        return new PurchaseMatchEvaluation(
            overallStatus,
            requiresApproval,
            results,
            Math.Round(results.Sum(x => x.ReceiptCostBaseAmount), 4),
            Math.Round(results.Sum(x => x.InvoiceNetBaseAmount), 4),
            Math.Round(results.Sum(x => x.InvoiceTaxBaseAmount), 4),
            Math.Round(results.Sum(x => x.InvoiceNetBaseAmount - x.ReceiptCostBaseAmount), 4));
    }

    private static RequestValidationException Validation(string field, string code) =>
        new(new Dictionary<string, string[]> { [field] = [code] });
}
