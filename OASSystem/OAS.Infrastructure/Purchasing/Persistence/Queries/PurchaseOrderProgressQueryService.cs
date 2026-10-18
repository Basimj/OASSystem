using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.PurchaseOrders.Queries;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;
using InvoiceStatus = OAS.Domain.Purchasing.Enums.PurchaseInvoiceStatus;
using ReceiptStatus = OAS.Domain.Purchasing.Enums.PurchaseReceiptStatus;
using ReturnStatus = OAS.Domain.Purchasing.Enums.PurchaseReturnStatus;

namespace OAS.Infrastructure.Purchasing.Persistence.Queries;

/// <summary>
/// Aggregates already-posted document quantities without creating shadow columns or
/// duplicating accounting postings. Used only for the AP-07 progress indicator.
/// </summary>
public sealed class PurchaseOrderProgressQueryService(OasDbContext db) : IPurchaseOrderProgressQueryService
{
    public async Task<PurchaseOrderProgressDto> GetAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await db.Set<PurchaseOrder>().AsNoTracking()
            .Where(x => x.Id == orderId)
            .Select(x => new { x.Id, x.PurchaseOrderCode })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), orderId);
        var lines = await db.Set<PurchaseOrderLine>().AsNoTracking()
            .Where(x => x.PurchaseOrderId == orderId)
            .OrderBy(x => x.LineSequence)
            .Select(x => new { x.Id, x.LineSequence, x.ProductNameSnapshot, x.OrderedQuantity })
            .ToListAsync(ct);

        var received = await (
                from line in db.Set<PurchaseReceiptLine>().AsNoTracking()
                join receipt in db.Set<PurchaseReceipt>().AsNoTracking()
                    on line.PurchaseReceiptId equals receipt.Id
                where receipt.PurchaseOrderId == orderId && receipt.Status == ReceiptStatus.Posted
                group line by line.PurchaseOrderLineId into g
                select new
                {
                    OrderLineId = g.Key,
                    Received = g.Sum(x => x.AcceptedQuantity)
                })
            .ToDictionaryAsync(x => x.OrderLineId, x => x.Received, ct);

        // Reserved/draft return quantities do not count as posted return movements.
        var returned = await (
                from returnLine in db.Set<PurchaseReturnLine>().AsNoTracking()
                join parent in db.Set<PurchaseReturn>().AsNoTracking()
                    on returnLine.PurchaseReturnId equals parent.Id
                join receiptLine in db.Set<PurchaseReceiptLine>().AsNoTracking()
                    on returnLine.PurchaseReceiptLineId equals receiptLine.Id
                join receipt in db.Set<PurchaseReceipt>().AsNoTracking()
                    on receiptLine.PurchaseReceiptId equals receipt.Id
                where receipt.PurchaseOrderId == orderId && parent.Status == ReturnStatus.Posted
                    && parent.SupplierId == receipt.SupplierId && parent.PurchaseReceiptId == receipt.Id
                group returnLine by receiptLine.PurchaseOrderLineId into g
                select new { OrderLineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.OrderLineId, x => x.Quantity, ct);

        var invoiced = await (
                from allocation in db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
                join receiptLine in db.Set<PurchaseReceiptLine>().AsNoTracking()
                    on allocation.PurchaseReceiptLineId equals receiptLine.Id
                join receipt in db.Set<PurchaseReceipt>().AsNoTracking()
                    on receiptLine.PurchaseReceiptId equals receipt.Id
                join invoiceLine in db.Set<PurchaseInvoiceLine>().AsNoTracking()
                    on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
                join invoice in db.Set<PurchaseInvoice>().AsNoTracking()
                    on invoiceLine.PurchaseInvoiceId equals invoice.Id
                where receipt.PurchaseOrderId == orderId && receipt.Status == ReceiptStatus.Posted
                    && invoice.Status == InvoiceStatus.Posted && invoice.SupplierId == receipt.SupplierId
                group allocation by receiptLine.PurchaseOrderLineId into g
                select new { OrderLineId = g.Key, Quantity = g.Sum(x => x.MatchedQuantity) })
            .ToDictionaryAsync(x => x.OrderLineId, x => x.Quantity, ct);

        var result = lines.Select(line =>
        {
            received.TryGetValue(line.Id, out var receiptTotals);
            invoiced.TryGetValue(line.Id, out var invoiceQuantity);
            var accepted = receiptTotals;
            returned.TryGetValue(line.Id, out var returnedQuantity);
            return PurchaseOrderProgressCalculator.Calculate(line.Id, line.LineSequence,
                line.ProductNameSnapshot, line.OrderedQuantity, accepted,
                invoiceQuantity, returnedQuantity);
        }).ToArray();

        return new PurchaseOrderProgressDto(order.Id, order.PurchaseOrderCode, result);
    }
}
