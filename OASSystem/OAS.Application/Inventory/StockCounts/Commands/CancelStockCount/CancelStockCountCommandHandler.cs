using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.CancelStockCount;

public sealed class CancelStockCountCommandHandler(
    IStockCountRepository stockCountRepository)
    : IRequestHandler<CancelStockCountCommand, Guid>
{
    public async Task<Guid> Handle(
        CancelStockCountCommand command,
        CancellationToken cancellationToken)
    {
        var stockCount = await stockCountRepository.GetForUpdateAsync(
            command.StockCountId,
            cancellationToken);

        if (stockCount is null)
            throw new NotFoundException(nameof(StockCount), command.StockCountId);

        if (stockCount.Status == StockCountStatus.Posted)
            throw new ConflictException("cannot_cancel_posted_count", "Cannot cancel a stock count that has already been posted.");

        stockCount.Cancel();
        stockCountRepository.Update(stockCount);

        return stockCount.Id;
    }
}
