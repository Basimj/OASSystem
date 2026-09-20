using Microsoft.EntityFrameworkCore;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure.Persistence.Repositories.Inventory;

public sealed class StockCountRepository(OasDbContext dbContext)
    : EfRepository<StockCount, Guid>(dbContext), IStockCountRepository
{
    public async Task<IReadOnlyList<StockCountLine>> GetLinesAsync(
        Guid stockCountId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.StockCountLines
            .AsNoTracking()
            .Where(l => l.StockCountId == stockCountId)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockCountLine?> GetLineAsync(
        Guid stockCountId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.StockCountLines
            .FirstOrDefaultAsync(
                l => l.StockCountId == stockCountId && l.Id == lineId,
                cancellationToken);
    }
}
