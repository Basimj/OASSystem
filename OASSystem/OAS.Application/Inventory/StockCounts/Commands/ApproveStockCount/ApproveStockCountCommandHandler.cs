using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.ApproveStockCount;

public sealed class ApproveStockCountCommandHandler(
    IStockCountRepository stockCountRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ApproveStockCountCommand, Guid>
{
    public async Task<Guid> Handle(
        ApproveStockCountCommand command,
        CancellationToken cancellationToken)
    {
        var stockCount = await stockCountRepository.GetForUpdateAsync(
            command.StockCountId,
            cancellationToken);

        if (stockCount is null)
            throw new NotFoundException(nameof(StockCount), command.StockCountId);

        if (stockCount.Status != StockCountStatus.Review)
            throw new ConflictException("invalid_status_transition", $"Cannot approve stock count from status '{stockCount.Status}'.");

        var lines = await stockCountRepository.GetLinesAsync(command.StockCountId, cancellationToken);
        if (lines.Count == 0)
            throw new ConflictException("stock_count_has_no_lines", "Cannot approve a stock count without lines.");

        if (lines.Any(x => !x.CountedAtUtc.HasValue))
            throw new ConflictException("stock_count_incomplete_lines", "All stock count lines must be counted before approval.");

        var nowUtc = timeProvider.GetUtcNow();
        var userId = currentUser.UserId ?? "system";

        stockCount.Approve(nowUtc, userId);
        stockCountRepository.Update(stockCount);

        return stockCount.Id;
    }
}
