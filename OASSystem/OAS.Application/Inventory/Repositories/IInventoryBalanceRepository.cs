using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Repositories;

public interface IInventoryBalanceRepository : IRepository<InventoryBalance, Guid>
{
    Task<InventoryBalance?> GetByWarehouseAndVariantAsync(
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBalance>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default);
}
