using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
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
        var inventoryLines = salesReturn.Lines.Where(x => x.IsActive && x.RestoresInventoryAsset).ToArray();
        var warehouseGroups = inventoryLines.GroupBy(x => x.WarehouseId!.Value).ToArray();

        var existing = await transactions.ListAsync(
            new Specification<InventoryTransaction>().Where(x =>
                x.TransactionType == InventoryTransactionType.SalesReturn &&
                x.ReferenceType == SalesSourceReferences.SalesReturn &&
                x.ReferenceId == salesReturn.Id),
            cancellationToken);
        if (existing.Count > 0)
        {
            var expectedWarehouses = warehouseGroups.Select(x => x.Key).OrderBy(x => x).ToArray();
            var postedWarehouses = existing
                .Where(x => x.Status == InventoryTransactionStatus.Posted && x.DestinationWarehouseId.HasValue)
                .Select(x => x.DestinationWarehouseId!.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            if (existing.Count == warehouseGroups.Length && expectedWarehouses.SequenceEqual(postedWarehouses))
                return new SalesReturnInventoryPostingResult(existing.Select(x => x.Id).ToArray());

            throw new ConflictException(
                "sales_return_inventory_partial_posting",
                "تم العثور على ترحيل مخزني جزئي/غير متسق لهذا المرتجع. أوقف إعادة المحاولة وراجع الحركة قبل أي ترحيل إضافي.");
        }

        foreach (var warehouseGroup in warehouseGroups)
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
