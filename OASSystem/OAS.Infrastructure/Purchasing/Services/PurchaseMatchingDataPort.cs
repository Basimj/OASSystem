using Microsoft.EntityFrameworkCore;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class PurchaseMatchingDataPort(OasDbContext dbContext) : IPurchaseMatchingDataPort
{
    public async Task<PurchaseReceiptMatchCandidate?> GetReceiptCandidateAsync(Guid purchaseReceiptLineId, Guid purchaseInvoiceId, CancellationToken cancellationToken = default)
    {
        var row = await (from line in dbContext.Set<PurchaseReceiptLine>().AsNoTracking()
                         join receipt in dbContext.Set<PurchaseReceipt>().AsNoTracking() on line.PurchaseReceiptId equals receipt.Id
                         join orderLine in dbContext.Set<PurchaseOrderLine>().AsNoTracking() on line.PurchaseOrderLineId equals orderLine.Id
                         join order in dbContext.Set<PurchaseOrder>().AsNoTracking() on orderLine.PurchaseOrderId equals order.Id
                         where line.Id == purchaseReceiptLineId
                         select new
                         {
                             line.Id,
                             receipt.ReceiptCode,
                             line.PurchaseOrderLineId,
                             line.ProductVariantId,
                             receipt.SupplierId,
                             order.CurrencyId,
                             line.AcceptedQuantity,
                             line.ActualUnitCost,
                             orderLine.UnitPrice,
                             orderLine.TaxRate,
                             receipt.Status
                         }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var alreadyMatched = await (from allocation in dbContext.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
                                    join invoiceLine in dbContext.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
                                    where allocation.PurchaseReceiptLineId == purchaseReceiptLineId && invoiceLine.PurchaseInvoiceId != purchaseInvoiceId
                                    select (decimal?)allocation.MatchedQuantity).SumAsync(cancellationToken) ?? 0m;
        var available = Math.Max(0m, row.AcceptedQuantity - alreadyMatched);
        return new PurchaseReceiptMatchCandidate(row.Id, row.ReceiptCode, row.PurchaseOrderLineId, row.ProductVariantId,
            row.SupplierId, row.CurrencyId, row.AcceptedQuantity, available, row.ActualUnitCost, row.UnitPrice, row.TaxRate,
            (OAS.Contracts.Purchasing.Enums.PurchaseReceiptStatus)(byte)row.Status);
    }
}
