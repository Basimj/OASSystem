using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.PurchaseReturns.Services;

public sealed class PurchaseReturnInventoryPostingService(
    IInventoryTransactionRepository transactions,
    IRepository<InventoryTransactionLine, Guid> transactionLines,
    IInventoryPostingService inventoryPosting,
    ISequenceNumberGenerator sequences) : IPurchaseReturnInventoryPostingService
{
    public async Task<PurchaseReturnInventoryPostingResult> PostAsync(
        PurchaseReturn purchaseReturn,
        string postedBy,
        DateTimeOffset postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var sequence = await sequences.NextAsync("InventoryTransaction", cancellationToken);
        var transaction = new InventoryTransaction(
            $"TXN-{purchaseReturn.PostingDate.Year:0000}-{sequence:000000}",
            InventoryTransactionType.PurchaseReturn,
            postedAtUtc,
            sourceWarehouseId: purchaseReturn.WarehouseId,
            referenceType: "PurchaseReturn",
            referenceId: purchaseReturn.Id,
            reason: $"Purchase return {purchaseReturn.ReturnCode}");
        await transactions.AddAsync(transaction, cancellationToken);

        foreach (var line in purchaseReturn.Lines.Where(x => x.IsActive).OrderBy(x => x.LineNumber))
        {
            var unitCost = await inventoryPosting.GetAvailableOutboundUnitCostAsync(
                purchaseReturn.WarehouseId, line.ProductVariantId, line.BaseQuantity, cancellationToken);
            line.SetInventoryCostSnapshot(unitCost);
            var txLine = new InventoryTransactionLine(transaction.Id, line.ProductVariantId, line.BaseQuantity, unitCost,
                $"Purchase return {purchaseReturn.ReturnCode} / line {line.LineNumber}");
            await transactionLines.AddAsync(txLine, cancellationToken);
            await inventoryPosting.PostMovementAsync(
                purchaseReturn.WarehouseId, line.ProductVariantId, InventoryMovementType.Out, line.BaseQuantity, unitCost,
                transaction.Id, txLine.Id, postedAtUtc, postedBy, cancellationToken);
        }

        purchaseReturn.RefreshInventoryTotals();
        transaction.Post(postedAtUtc, postedBy);
        transactions.Update(transaction);
        return new PurchaseReturnInventoryPostingResult([transaction.Id]);
    }
}
