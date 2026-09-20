using MediatR;
using OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceById;
using OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceByWarehouseAndVariant;
using OAS.Application.Inventory.Balances.Queries.GetInventoryBalances;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.Balances.Services;

public sealed class InventoryBalanceService(ISender sender) : IInventoryBalanceService
{
    public Task<InventoryBalanceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryBalanceByIdQuery(id), cancellationToken);

    public Task<PagedResult<InventoryBalanceDto>> GetPageAsync(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryBalancesQuery(request, warehouseId, productVariantId), cancellationToken);

    public Task<InventoryBalanceDto?> GetByWarehouseAndVariantAsync(
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryBalanceByWarehouseAndVariantQuery(warehouseId, productVariantId), cancellationToken);
}
