using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Purchasing.Performance;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Infrastructure.Purchasing.Persistence.Queries;

/// <summary>
/// AP-12 consolidated additions. All calculations use posted documents, server filters
/// and the existing read model. GRNI below is an operational accrual estimate,
/// never claimed to be the GRNI general-ledger balance.
/// </summary>
public sealed partial class PurchasingPerformanceQueryService
{
    private async Task<(decimal? OnTimeDeliveryRatePercent, decimal UninvoicedReceiptCostBase,
        decimal PurchasePriceVarianceBase)> GetAdvancedMetricsAsync(
        PurchasingPerformanceFilterDto filter, CancellationToken ct)
    {
        var postedReceipts =
            from line in db.Set<PurchaseReceiptLine>().AsNoTracking()
            join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on line.PurchaseReceiptId equals receipt.Id
            join order in db.Set<PurchaseOrder>().AsNoTracking() on receipt.PurchaseOrderId equals order.Id
            join orderLine in db.Set<PurchaseOrderLine>().AsNoTracking() on line.PurchaseOrderLineId equals orderLine.Id
            where receipt.Status == PurchaseReceiptStatus.Posted
                && receipt.SupplierId == order.SupplierId
                && orderLine.PurchaseOrderId == order.Id
                && receipt.PostingDate <= filter.ToDate
                && (!filter.SupplierId.HasValue || receipt.SupplierId == filter.SupplierId.Value)
                && (!filter.WarehouseId.HasValue || receipt.WarehouseId == filter.WarehouseId.Value)
                && (!filter.ProductVariantId.HasValue || line.ProductVariantId == filter.ProductVariantId.Value)
                && (!filter.CurrencyId.HasValue || order.CurrencyId == filter.CurrencyId.Value)
            select new {
                ReceiptLineId = line.Id, receipt.Id, receipt.SupplierId, receipt.ReceiptDate, receipt.PostingDate,
                line.AcceptedQuantity, line.BaseAcceptedQuantity, line.ActualUnitCost, line.TotalAcceptedCost,
                DueDate = orderLine.ExpectedDeliveryDate ?? order.ExpectedDeliveryDate
            };

        // Weighted by base-unit received quantity: one box of 12 is not one loose unit.
        var eligibleDelivery = postedReceipts.Where(x =>
            x.PostingDate >= filter.FromDate && x.DueDate != null && x.BaseAcceptedQuantity > 0m);
        var eligibleWeight = await eligibleDelivery.SumAsync(x => (decimal?)x.BaseAcceptedQuantity, ct) ?? 0m;
        var onTimeWeight = await eligibleDelivery.Where(x => x.ReceiptDate <= x.DueDate)
            .SumAsync(x => (decimal?)x.BaseAcceptedQuantity, ct) ?? 0m;

        var receiptCost = await postedReceipts.SumAsync(x => (decimal?)x.TotalAcceptedCost, ct) ?? 0m;

        var matches =
            from allocation in db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
            join receipt in postedReceipts on allocation.PurchaseReceiptLineId equals receipt.ReceiptLineId
            join invoiceLine in db.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
            join invoice in db.Set<PurchaseInvoice>().AsNoTracking() on invoiceLine.PurchaseInvoiceId equals invoice.Id
            where invoice.Status == PurchaseInvoiceStatus.Posted && invoice.SupplierId == receipt.SupplierId
                && invoice.PostingDate <= filter.ToDate
            select new {
                invoice.PostingDate,
                ReceiptCost = receipt.AcceptedQuantity <= 0m ? 0m :
                    allocation.MatchedQuantity * receipt.BaseAcceptedQuantity / receipt.AcceptedQuantity * receipt.ActualUnitCost,
                NetBase = allocation.MatchedNetAmount * invoice.ExchangeRate
            };

        var matchedAsOf = await matches.SumAsync(x => (decimal?)x.ReceiptCost, ct) ?? 0m;
        var periodMatchedReceiptCost = await matches.Where(x => x.PostingDate >= filter.FromDate)
            .SumAsync(x => (decimal?)x.ReceiptCost, ct) ?? 0m;
        var periodMatchedInvoiceNet = await matches.Where(x => x.PostingDate >= filter.FromDate)
            .SumAsync(x => (decimal?)x.NetBase, ct) ?? 0m;

        var returnCost = await (
            from line in db.Set<PurchaseReturnLine>().AsNoTracking()
            join receipt in postedReceipts on line.PurchaseReceiptLineId equals receipt.ReceiptLineId
            join purchaseReturn in db.Set<PurchaseReturn>().AsNoTracking() on line.PurchaseReturnId equals purchaseReturn.Id
            where purchaseReturn.Status == PurchaseReturnStatus.Posted && line.IsActive
                && line.PurchaseInvoiceLineId == null && purchaseReturn.PurchaseReceiptId == receipt.Id
                && purchaseReturn.SupplierId == receipt.SupplierId
                && purchaseReturn.PostingDate <= filter.ToDate
            select (decimal?)line.ReceiptCostBaseAmount).SumAsync(ct) ?? 0m;

        return (
            PurchasingPerformanceMath.OnTimeDeliveryPercent(eligibleWeight, onTimeWeight),
            PurchasingPerformanceMath.UninvoicedReceiptCost(receiptCost, matchedAsOf, returnCost),
            PurchasingPerformanceMath.PurchasePriceVarianceBase(periodMatchedInvoiceNet, periodMatchedReceiptCost));
    }

    public async Task<PurchasingPerformanceOrderLineDrillDownDto> GetLineDrillDownAsync(
        Guid purchaseOrderLineId, PurchasingPerformanceFilterDto filter, CancellationToken ct = default)
    {
        if (purchaseOrderLineId == Guid.Empty) throw new ArgumentException("Purchase order line is required.");
        if (filter.FromDate > filter.ToDate) throw new ArgumentException("Invalid report dates.");

        // Apply the caller's supplier, warehouse, currency and product filters again at the
        // database boundary. A guessed line ID must not expose another supplier's documents.
        var orderLine = await (
            from line in db.Set<PurchaseOrderLine>().AsNoTracking()
            join order in db.Set<PurchaseOrder>().AsNoTracking() on line.PurchaseOrderId equals order.Id
            where line.Id == purchaseOrderLineId
                && order.OrderDate >= filter.FromDate && order.OrderDate <= filter.ToDate
                && order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.Rejected
                && order.Status != PurchaseOrderStatus.Cancelled
                && (!filter.SupplierId.HasValue || order.SupplierId == filter.SupplierId.Value)
                && (!filter.WarehouseId.HasValue || order.DestinationWarehouseId == filter.WarehouseId.Value)
                && (!filter.ProductVariantId.HasValue || line.ProductVariantId == filter.ProductVariantId.Value)
                && (!filter.CurrencyId.HasValue || order.CurrencyId == filter.CurrencyId.Value)
            select new { order.Id, order.PurchaseOrderCode, order.SupplierId })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(PurchaseOrderLine), purchaseOrderLineId);

        var settings = await db.Set<AccountingSettings>().AsNoTracking()
            .Where(x => x.Id == AccountingSettings.SingletonId)
            .Select(x => new { x.BaseCurrencyId }).FirstOrDefaultAsync(ct);
        var baseCode = settings is not null && settings.BaseCurrencyId != Guid.Empty
            ? await db.Set<Currency>().AsNoTracking().Where(x => x.Id == settings.BaseCurrencyId)
                .Select(x => x.Code).FirstOrDefaultAsync(ct) ?? "العملة الأساسية"
            : "العملة الأساسية";

        const int maxDocuments = 200;
        var receiptRows = await (
            from line in db.Set<PurchaseReceiptLine>().AsNoTracking()
            join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on line.PurchaseReceiptId equals receipt.Id
            where line.PurchaseOrderLineId == purchaseOrderLineId
                && receipt.Status == PurchaseReceiptStatus.Posted && receipt.SupplierId == orderLine.SupplierId
                && receipt.PurchaseOrderId == orderLine.Id
                && receipt.PostingDate <= filter.ToDate
            orderby receipt.PostingDate descending, receipt.Id
            select new PurchasingPerformanceLinkedDocumentDto(
                "receipts", receipt.Id, receipt.ReceiptCode, receipt.PostingDate,
                line.AcceptedQuantity, line.TotalAcceptedCost))
            .Take(maxDocuments + 1).ToListAsync(ct);

        var invoiceRows = await (
            from allocation in db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
            join receiptLine in db.Set<PurchaseReceiptLine>().AsNoTracking() on allocation.PurchaseReceiptLineId equals receiptLine.Id
            join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on receiptLine.PurchaseReceiptId equals receipt.Id
            join invoiceLine in db.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
            join invoice in db.Set<PurchaseInvoice>().AsNoTracking() on invoiceLine.PurchaseInvoiceId equals invoice.Id
            where receiptLine.PurchaseOrderLineId == purchaseOrderLineId
                && receipt.Status == PurchaseReceiptStatus.Posted && receipt.PurchaseOrderId == orderLine.Id
                && invoice.Status == PurchaseInvoiceStatus.Posted
                && invoice.SupplierId == orderLine.SupplierId && receipt.SupplierId == orderLine.SupplierId
                && receipt.PostingDate <= filter.ToDate && invoice.PostingDate <= filter.ToDate
            orderby invoice.PostingDate descending, invoice.Id
            select new PurchasingPerformanceLinkedDocumentDto(
                "invoices", invoice.Id, invoice.PurchaseInvoiceCode, invoice.PostingDate,
                allocation.MatchedQuantity, allocation.MatchedNetAmount * invoice.ExchangeRate))
            .Take(maxDocuments + 1).ToListAsync(ct);

        var returnRows = await (
            from line in db.Set<PurchaseReturnLine>().AsNoTracking()
            join receiptLine in db.Set<PurchaseReceiptLine>().AsNoTracking() on line.PurchaseReceiptLineId equals receiptLine.Id
            join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on receiptLine.PurchaseReceiptId equals receipt.Id
            join purchaseReturn in db.Set<PurchaseReturn>().AsNoTracking() on line.PurchaseReturnId equals purchaseReturn.Id
            where receiptLine.PurchaseOrderLineId == purchaseOrderLineId
                && receipt.Status == PurchaseReceiptStatus.Posted && receipt.PurchaseOrderId == orderLine.Id
                && purchaseReturn.Status == PurchaseReturnStatus.Posted && line.IsActive
                && purchaseReturn.SupplierId == orderLine.SupplierId && receipt.SupplierId == orderLine.SupplierId
                && purchaseReturn.PurchaseReceiptId == receipt.Id
                && receipt.PostingDate <= filter.ToDate && purchaseReturn.PostingDate <= filter.ToDate
            orderby purchaseReturn.PostingDate descending, purchaseReturn.Id
            select new PurchasingPerformanceLinkedDocumentDto(
                "returns", purchaseReturn.Id, purchaseReturn.ReturnCode, purchaseReturn.PostingDate,
                line.Quantity, line.SupplierGrossBaseAmount))
            .Take(maxDocuments + 1).ToListAsync(ct);

        return new PurchasingPerformanceOrderLineDrillDownDto(
            orderLine.Id, purchaseOrderLineId, orderLine.PurchaseOrderCode, baseCode,
            receiptRows.Take(maxDocuments).ToArray(),
            invoiceRows.Take(maxDocuments).ToArray(),
            returnRows.Take(maxDocuments).ToArray(),
            receiptRows.Count > maxDocuments || invoiceRows.Count > maxDocuments || returnRows.Count > maxDocuments);
    }
}
