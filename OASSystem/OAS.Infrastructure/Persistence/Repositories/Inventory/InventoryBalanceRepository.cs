using Microsoft.EntityFrameworkCore;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure.Persistence.Repositories.Inventory;

public sealed class InventoryBalanceRepository(OasDbContext dbContext)
    : EfRepository<InventoryBalance, Guid>(dbContext), IInventoryBalanceRepository
{
    public async Task<InventoryBalance?> GetByWarehouseAndVariantAsync(
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        return await Set.FirstOrDefaultAsync(
            b => b.WarehouseId == warehouseId && b.ProductVariantId == productVariantId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryBalance>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await Set.AsNoTracking()
            .Where(b => b.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }
}
