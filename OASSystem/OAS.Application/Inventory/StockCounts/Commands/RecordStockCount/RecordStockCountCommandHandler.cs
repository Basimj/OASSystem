using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.RecordStockCount;

public sealed class RecordStockCountCommandHandler(
    IStockCountRepository stockCountRepository,
    IRepository<StockCountLine, Guid> lineRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<RecordStockCountCommand, Guid>
{
    public async Task<Guid> Handle(
        RecordStockCountCommand command,
        CancellationToken cancellationToken)
    {
        var stockCount = await stockCountRepository.GetByIdAsync(
            command.StockCountId,
            cancellationToken);

        if (stockCount is null)
            throw new NotFoundException(nameof(StockCount), command.StockCountId);

        if (stockCount.Status is not (StockCountStatus.Counting or StockCountStatus.Review))
            throw new ConflictException("invalid_status_for_counting", $"Cannot record counts when stock count is in status '{stockCount.Status}'.");

        var line = await stockCountRepository.GetLineAsync(
            command.StockCountId,
            command.LineId,
            cancellationToken);

        if (line is null)
            throw new NotFoundException(nameof(StockCountLine), command.LineId);

        var countedAtUtc = timeProvider.GetUtcNow();
        var countedBy = currentUser.UserId ?? "system";

        line.RecordCount(
            command.Request.CountedQuantity,
            countedAtUtc,
            countedBy);

        lineRepository.Update(line);

        return line.Id;
    }
}
