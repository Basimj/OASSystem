using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.Balances.Services;

public interface IInventoryBalanceService
{
    Task<InventoryBalanceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryBalanceDto>> GetPageAsync(PageRequest request, Guid? warehouseId = null, Guid? productVariantId = null, CancellationToken cancellationToken = default);
    Task<InventoryBalanceDto?> GetByWarehouseAndVariantAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default);
}
