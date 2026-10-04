using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Returns.Services;

public sealed class SalesReturnInventoryPostingService(
    IInventoryTransactionRepository transactions,
    IRepository<InventoryTransactionLine, Guid> transactionLines,
    IInventoryPostingService inventoryPosting,
    ISequenceNumberGenerator sequences) : ISalesReturnInventoryPostingService
{
    public async Task<SalesReturnInventoryPostingResult> PostAsync(
        SalesReturn salesReturn,
        string postedBy,
        DateTimeOffset postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var transactionIds = new List<Guid>();
        var inventoryLines = salesReturn.Lines.Where(x => x.IsActive && x.RequiresInventory).ToArray();
        foreach (var warehouseGroup in inventoryLines.GroupBy(x => x.WarehouseId!.Value))
        {
            var sequence = await sequences.NextAsync("InventoryTransaction", cancellationToken);
            var transaction = new InventoryTransaction(
                $"TXN-{salesReturn.PostingDate.Year:0000}-{sequence:000000}",
                InventoryTransactionType.SalesReturn,
                postedAtUtc,
                destinationWarehouseId: warehouseGroup.Key,
                referenceType: SalesSourceReferences.SalesReturn,
                referenceId: salesReturn.Id,
                reason: $"Sales return {salesReturn.ReturnCode}");
            await transactions.AddAsync(transaction, cancellationToken);

            foreach (var line in warehouseGroup.OrderBy(x => x.LineNumber))
            {
                var unitCost = line.UnitCostSnapshot ?? 0m;
                var txLine = new InventoryTransactionLine(
                    transaction.Id,
                    line.ProductVariantId!.Value,
                    line.Quantity,
                    unitCost,
                    $"Sales return {salesReturn.ReturnCode} / line {line.LineNumber}");
                await transactionLines.AddAsync(txLine, cancellationToken);
                await inventoryPosting.PostMovementAsync(
                    warehouseGroup.Key,
                    line.ProductVariantId.Value,
                    InventoryMovementType.In,
                    line.Quantity,
                    unitCost,
                    transaction.Id,
                    txLine.Id,
                    postedAtUtc,
                    postedBy,
                    cancellationToken);
            }

            transaction.Post(postedAtUtc, postedBy);
            transactionIds.Add(transaction.Id);
        }

        return new SalesReturnInventoryPostingResult(transactionIds);
    }
}
