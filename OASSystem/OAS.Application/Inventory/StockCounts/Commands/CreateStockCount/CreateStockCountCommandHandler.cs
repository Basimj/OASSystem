using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.StockCounts.Commands.CreateStockCount;

public sealed class CreateStockCountCommandHandler(
    IStockCountRepository stockCountRepository,
    IRepository<StockCountLine, Guid> lineRepository,
    IInventoryBalanceRepository balanceRepository,
    IReadRepository<Warehouse, Guid> warehouseRepository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateStockCountCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateStockCountCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        var warehouse = await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        var countNumber = request.CountNumber;
        if (string.IsNullOrWhiteSpace(countNumber))
        {
            var seq = await sequenceNumberGenerator.NextAsync("StockCount", cancellationToken);
            countNumber = $"SC-{request.CountDate.Year:0000}-{seq:000000}";
        }

        var stockCount = new StockCount(
            countNumber,
            request.WarehouseId,
            request.CountDate,
            request.Notes);

        await stockCountRepository.AddAsync(stockCount, cancellationToken);

        var balances = await balanceRepository.GetByWarehouseAsync(request.WarehouseId, cancellationToken);
        foreach (var balance in balances)
        {
            var line = new StockCountLine(
                stockCount.Id,
                balance.ProductVariantId,
                balance.OnHandQuantity,
                balance.AverageUnitCost);

            await lineRepository.AddAsync(line, cancellationToken);
        }

        return stockCount.Id;
    }
}
