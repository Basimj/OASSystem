using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.PostStockCount;

public sealed class PostStockCountCommandHandler(
    IStockCountRepository stockCountRepository,
    IInventoryTransactionRepository transactionRepository,
    IRepository<InventoryTransactionLine, Guid> transactionLineRepository,
    IInventoryPostingService postingService,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<PostStockCountCommand, Guid>
{
    public async Task<Guid> Handle(
        PostStockCountCommand command,
        CancellationToken cancellationToken)
    {
        var stockCount = await stockCountRepository.GetForUpdateAsync(
            command.StockCountId,
            cancellationToken);

        if (stockCount is null)
            throw new NotFoundException(nameof(StockCount), command.StockCountId);

        if (stockCount.Status != StockCountStatus.Approved)
            throw new ConflictException("invalid_status_transition", $"Cannot post stock count from status '{stockCount.Status}'. Must be Approved.");

        var lines = await stockCountRepository.GetLinesAsync(command.StockCountId, cancellationToken);
        if (lines.Count == 0)
            throw new ConflictException("stock_count_has_no_lines", "Cannot post a stock count without lines.");

        if (lines.Any(x => !x.CountedAtUtc.HasValue))
            throw new ConflictException("stock_count_incomplete_lines", "All stock count lines must be counted before posting.");

        var nowUtc = timeProvider.GetUtcNow();
        var userId = currentUser.UserId ?? "system";
        var movementDate = new DateTimeOffset(
            stockCount.CountDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var increases = lines.Where(x => x.DifferenceQuantity > 0).ToArray();
        var decreases = lines.Where(x => x.DifferenceQuantity < 0).ToArray();

        if (increases.Length > 0)
        {
            await PostAdjustmentAsync(
                stockCount,
                increases,
                InventoryTransactionType.AdjustmentIncrease,
                movementDate,
                nowUtc,
                userId,
                cancellationToken);
        }

        if (decreases.Length > 0)
        {
            await PostAdjustmentAsync(
                stockCount,
                decreases,
                InventoryTransactionType.AdjustmentDecrease,
                movementDate,
                nowUtc,
                userId,
                cancellationToken);
        }

        stockCount.Post(nowUtc, userId);
        stockCountRepository.Update(stockCount);

        return stockCount.Id;
    }

    private async Task PostAdjustmentAsync(
        StockCount stockCount,
        IReadOnlyList<StockCountLine> lines,
        InventoryTransactionType transactionType,
        DateTimeOffset movementDate,
        DateTimeOffset postedAtUtc,
        string userId,
        CancellationToken cancellationToken)
    {
        var seq = await sequenceNumberGenerator.NextAsync("InventoryTransaction", cancellationToken);
        var transactionNumber = $"TXN-{stockCount.CountDate.Year:0000}-{seq:000000}";
        var isIncrease = transactionType == InventoryTransactionType.AdjustmentIncrease;

        var transaction = new InventoryTransaction(
            transactionNumber,
            transactionType,
            movementDate,
            sourceWarehouseId: isIncrease ? null : stockCount.WarehouseId,
            destinationWarehouseId: isIncrease ? stockCount.WarehouseId : null,
            referenceType: "StockCount",
            referenceId: stockCount.Id,
            reason: "Stock count variance",
            notes: $"Generated from stock count {stockCount.CountNumber}");

        await transactionRepository.AddAsync(transaction, cancellationToken);

        foreach (var stockLine in lines)
        {
            var quantity = Math.Abs(stockLine.DifferenceQuantity);
            var transactionLine = new InventoryTransactionLine(
                transaction.Id,
                stockLine.ProductVariantId,
                quantity,
                stockLine.AverageCostSnapshot,
                $"Stock count line {stockLine.Id:D}");

            await transactionLineRepository.AddAsync(transactionLine, cancellationToken);

            await postingService.PostMovementAsync(
                stockCount.WarehouseId,
                stockLine.ProductVariantId,
                isIncrease ? InventoryMovementType.In : InventoryMovementType.Out,
                quantity,
                stockLine.AverageCostSnapshot,
                transaction.Id,
                transactionLine.Id,
                movementDate,
                userId,
                cancellationToken);
        }

        // The transaction is still tracked as Added. Mutating it is enough; calling Update here
        // would risk changing a new entity to Modified before its first INSERT.
        transaction.Post(postedAtUtc, userId);
    }
}
