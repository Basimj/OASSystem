using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Repositories;

public interface IStockCountRepository : IRepository<StockCount, Guid>
{
    Task<IReadOnlyList<StockCountLine>> GetLinesAsync(
        Guid stockCountId,
        CancellationToken cancellationToken = default);

    Task<StockCountLine?> GetLineAsync(
        Guid stockCountId,
        Guid lineId,
        CancellationToken cancellationToken = default);
}
