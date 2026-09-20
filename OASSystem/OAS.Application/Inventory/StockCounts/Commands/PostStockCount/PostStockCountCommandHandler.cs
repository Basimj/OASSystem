using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.PostStockCount;

public sealed class PostStockCountCommandHandler(
    IStockCountRepository stockCountRepository,
    IInventoryPostingService postingService,
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
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId ?? "system";
        var movementDate = stockCount.CountDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        foreach (var line in lines)
        {
            if (line.DifferenceQuantity > 0)
            {
                await postingService.PostMovementAsync(
                    stockCount.WarehouseId,
                    line.ProductVariantId,
                    InventoryMovementType.In,
                    line.DifferenceQuantity,
                    line.AverageCostSnapshot,
                    stockCount.Id,
                    line.Id,
                    movementDate,
                    userId,
                    cancellationToken);
            }
            else if (line.DifferenceQuantity < 0)
            {
                await postingService.PostMovementAsync(
                    stockCount.WarehouseId,
                    line.ProductVariantId,
                    InventoryMovementType.Out,
                    Math.Abs(line.DifferenceQuantity),
                    line.AverageCostSnapshot,
                    stockCount.Id,
                    line.Id,
                    movementDate,
                    userId,
                    cancellationToken);
            }
        }

        stockCount.Post(nowUtc, userId);
        stockCountRepository.Update(stockCount);

        return stockCount.Id;
    }
}
