using MediatR;
using OAS.Application.Inventory.Balances.Mapping;
using OAS.Application.Inventory.Repositories;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceByWarehouseAndVariant;

public sealed class GetInventoryBalanceByWarehouseAndVariantQueryHandler(
    IInventoryBalanceRepository repository,
    InventoryBalanceMapper mapper)
    : IRequestHandler<GetInventoryBalanceByWarehouseAndVariantQuery, InventoryBalanceDto?>
{
    public async Task<InventoryBalanceDto?> Handle(
        GetInventoryBalanceByWarehouseAndVariantQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByWarehouseAndVariantAsync(
            request.WarehouseId,
            request.ProductVariantId,
            cancellationToken);

        return entity is null ? null : mapper.ToRead(entity);
    }
}
