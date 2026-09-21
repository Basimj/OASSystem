using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.StartStockCount;

public sealed class StartStockCountCommandHandler(
    IStockCountRepository stockCountRepository,
    TimeProvider timeProvider)
    : IRequestHandler<StartStockCountCommand, Guid>
{
    public async Task<Guid> Handle(
        StartStockCountCommand command,
        CancellationToken cancellationToken)
    {
        var stockCount = await stockCountRepository.GetForUpdateAsync(
            command.StockCountId,
            cancellationToken);

        if (stockCount is null)
            throw new NotFoundException(nameof(StockCount), command.StockCountId);

        if (stockCount.Status != StockCountStatus.Draft)
            throw new ConflictException("invalid_status_transition", $"Cannot start stock count from status '{stockCount.Status}'.");

        var lines = await stockCountRepository.GetLinesAsync(command.StockCountId, cancellationToken);
        if (lines.Count == 0)
            throw new ConflictException("stock_count_has_no_lines", "Cannot start a stock count without lines.");

        var nowUtc = timeProvider.GetUtcNow();
        stockCount.Start(nowUtc);
        stockCountRepository.Update(stockCount);

        return stockCount.Id;
    }
}
