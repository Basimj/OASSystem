using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Posting;

public sealed class SalesInventoryPostingService(
    IInventoryTransactionRepository transactions,
    IRepository<InventoryTransactionLine, Guid> transactionLines,
    IRepository<StockReservation, Guid> reservations,
    IInventoryPostingService inventoryPosting,
    ISequenceNumberGenerator sequences) : ISalesInventoryPostingService
{
    public async Task<SalesInventoryPostingResult> PostAsync(
        SalesInvoice invoice,
        string postedBy,
        DateTimeOffset postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var ids = new List<Guid>();
        var costs = new List<SalesInventoryLineCostResult>();
        var inventoryLines = invoice.Lines.Where(x => x.IsActive && x.RequiresInventory).ToList();

        foreach (var group in inventoryLines.GroupBy(x => x.WarehouseId!.Value))
        {
            var sequence = await sequences.NextAsync("InventoryTransaction", cancellationToken);
            var transaction = new InventoryTransaction(
                $"TXN-{invoice.PostingDate.Year:0000}-{sequence:000000}",
                InventoryTransactionType.Sale,
                postedAtUtc,
                sourceWarehouseId: group.Key,
                referenceType: SalesSourceReferences.SalesInvoice,
                referenceId: invoice.Id,
                reason: $"Sales invoice {invoice.InvoiceCode}");
            await transactions.AddAsync(transaction, cancellationToken);

            foreach (var invoiceLine in group.OrderBy(x => x.LineNumber))
            {
                var activeReservations = await GetReservationsAsync(invoice, invoiceLine, cancellationToken);
                if (activeReservations.Count != 1 || activeReservations[0].Quantity != invoiceLine.Quantity)
                    throw new ConflictException(SalesErrorCodes.ReservationConflict, "حجز المخزون الخاص بسطر الفاتورة غير مطابق للكمية المطلوبة.");

                var unitCost = await inventoryPosting.GetOutboundUnitCostAsync(
                    group.Key, invoiceLine.ProductVariantId!.Value, invoiceLine.Quantity, cancellationToken);
                var inventoryLine = new InventoryTransactionLine(
                    transaction.Id, invoiceLine.ProductVariantId.Value, invoiceLine.Quantity, unitCost,
                    $"Sales invoice {invoice.InvoiceCode} / line {invoiceLine.LineNumber}");
                await transactionLines.AddAsync(inventoryLine, cancellationToken);

                var (_, ledger) = await inventoryPosting.PostReservedOutboundAsync(
                    group.Key, invoiceLine.ProductVariantId.Value, invoiceLine.Quantity,
                    transaction.Id, inventoryLine.Id, postedAtUtc, postedBy, cancellationToken);

                var reservation = activeReservations[0];
                reservation.Consume(postedAtUtc);

                costs.Add(new SalesInventoryLineCostResult(
                    invoiceLine.Id,
                    ledger.UnitCost,
                    invoiceLine.Quantity * ledger.UnitCost,
                    transaction.Id));
            }

            transaction.Post(postedAtUtc, postedBy);
            ids.Add(transaction.Id);
        }

        return new SalesInventoryPostingResult(ids, costs);
    }

    private async Task<IReadOnlyList<StockReservation>> GetReservationsAsync(
        SalesInvoice invoice,
        SalesInvoiceLine line,
        CancellationToken cancellationToken)
    {
        string documentType;
        Guid documentId;
        Guid sourceLineId;

        if (invoice.CustomerOrderId.HasValue && line.CustomerOrderLineId.HasValue)
        {
            documentType = SalesSourceReferences.CustomerOrder;
            documentId = invoice.CustomerOrderId.Value;
            sourceLineId = line.CustomerOrderLineId.Value;
        }
        else
        {
            documentType = SalesSourceReferences.SalesInvoice;
            documentId = invoice.Id;
            sourceLineId = line.Id;
        }

        var spec = new Specification<StockReservation>()
            .Where(x => x.SourceModule == SalesSourceReferences.Module && x.SourceDocumentType == documentType &&
                        x.SourceDocumentId == documentId && x.SourceLineId == sourceLineId &&
                        x.Status == StockReservationStatus.Active && x.IsActive)
            .Tracking();
        return await reservations.ListAsync(spec, cancellationToken);
    }
}
