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
        // A previous line in the same unit of work may have created this balance.
        // Database queries do not return Added entities before SaveChanges.
        var tracked = Set.Local.FirstOrDefault(
            b => b.WarehouseId == warehouseId && b.ProductVariantId == productVariantId);
        if (tracked is not null)
            return tracked;

        return await Set.FirstOrDefaultAsync(
            b => b.WarehouseId == warehouseId && b.ProductVariantId == productVariantId,
            cancellationToken);
    }

    // Reimplement the repository interface here without changing shared CRUD behavior.
    public new void Update(InventoryBalance balance)
    {
        if (DbContext.Entry(balance).State != EntityState.Added)
            base.Update(balance);
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
