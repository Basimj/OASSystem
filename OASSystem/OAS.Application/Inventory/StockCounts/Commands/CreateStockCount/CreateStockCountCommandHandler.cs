using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

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

        if (!warehouse.IsActive)
            throw new ConflictException("stock_count_warehouse_inactive", "Cannot create a stock count for an inactive warehouse.");

        var openCountSpec = new Specification<StockCount>();
        openCountSpec.Where(x =>
            x.WarehouseId == request.WarehouseId &&
            x.Status != StockCountStatus.Posted &&
            x.Status != StockCountStatus.Cancelled);

        if (await stockCountRepository.CountAsync(openCountSpec, cancellationToken) > 0)
            throw new ConflictException("stock_count_open_exists", "An open stock count already exists for this warehouse.");

        var countNumber = request.CountNumber;
        if (string.IsNullOrWhiteSpace(countNumber))
        {
            var seq = await sequenceNumberGenerator.NextAsync("StockCount", cancellationToken);
            countNumber = $"SC-{request.CountDate.Year:0000}-{seq:000000}";
        }
        else
        {
            countNumber = countNumber.Trim();
        }

        var numberSpec = new Specification<StockCount>();
        numberSpec.Where(x => x.CountNumber == countNumber);
        if (await stockCountRepository.CountAsync(numberSpec, cancellationToken) > 0)
            throw new ConflictException("stock_count_number_exists", "A stock count with the same number already exists.");

        var balances = await balanceRepository.GetByWarehouseAsync(request.WarehouseId, cancellationToken);
        if (balances.Count == 0)
            throw new ConflictException("stock_count_warehouse_has_no_balance", "Cannot create a stock count for a warehouse without inventory balance rows.");

        var stockCount = new StockCount(
            countNumber,
            request.WarehouseId,
            request.CountDate,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim());

        await stockCountRepository.AddAsync(stockCount, cancellationToken);

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
