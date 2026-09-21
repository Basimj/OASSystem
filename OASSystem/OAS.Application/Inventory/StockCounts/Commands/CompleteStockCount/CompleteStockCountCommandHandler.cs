using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.CompleteStockCount;

public sealed class CompleteStockCountCommandHandler(
    IStockCountRepository stockCountRepository,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteStockCountCommand, Guid>
{
    public async Task<Guid> Handle(
        CompleteStockCountCommand command,
        CancellationToken cancellationToken)
    {
        var stockCount = await stockCountRepository.GetForUpdateAsync(
            command.StockCountId,
            cancellationToken);

        if (stockCount is null)
            throw new NotFoundException(nameof(StockCount), command.StockCountId);

        if (stockCount.Status != StockCountStatus.Counting)
            throw new ConflictException("invalid_status_transition", $"Cannot complete stock count from status '{stockCount.Status}'.");

        var lines = await stockCountRepository.GetLinesAsync(command.StockCountId, cancellationToken);
        if (lines.Count == 0)
            throw new ConflictException("stock_count_has_no_lines", "Cannot complete a stock count without lines.");

        if (lines.Any(x => !x.CountedAtUtc.HasValue))
            throw new ConflictException("stock_count_incomplete_lines", "All stock count lines must be counted before completion.");

        var nowUtc = timeProvider.GetUtcNow();
        stockCount.Complete(nowUtc);
        stockCountRepository.Update(stockCount);

        return stockCount.Id;
    }
}
