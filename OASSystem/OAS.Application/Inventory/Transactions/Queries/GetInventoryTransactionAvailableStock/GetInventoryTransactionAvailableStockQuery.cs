using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Repositories;
using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionAvailableStock;

// Creation permission exposes only the positive available quantities needed for selection,
// not inventory valuation, reservations, or the full stock reporting endpoint.
public sealed record GetInventoryTransactionAvailableStockQuery(Guid WarehouseId)
    : IQuery<IReadOnlyList<InventoryTransactionAvailableStockDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [InventoryPermissions.Transactions.Create];
}

public sealed class GetInventoryTransactionAvailableStockQueryHandler(
    IInventoryBalanceRepository balances,
    IReadRepository<Warehouse, Guid> warehouses)
    : IRequestHandler<GetInventoryTransactionAvailableStockQuery, IReadOnlyList<InventoryTransactionAvailableStockDto>>
{
    public async Task<IReadOnlyList<InventoryTransactionAvailableStockDto>> Handle(
        GetInventoryTransactionAvailableStockQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);
        if (!warehouse.IsActive)
            throw new ConflictException("warehouse_inactive", "مخزن المصدر غير نشط.");

        var items = await balances.GetByWarehouseAsync(request.WarehouseId, cancellationToken);
        return items.Where(x => x.AvailableQuantity > 0m)
            .Select(x => new InventoryTransactionAvailableStockDto(x.ProductVariantId, x.AvailableQuantity))
            .ToArray();
    }
}
