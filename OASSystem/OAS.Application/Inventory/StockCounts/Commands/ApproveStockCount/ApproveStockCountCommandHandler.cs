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

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId ?? "system";

        stockCount.Approve(nowUtc, userId);
        stockCountRepository.Update(stockCount);

        return stockCount.Id;
    }
}
