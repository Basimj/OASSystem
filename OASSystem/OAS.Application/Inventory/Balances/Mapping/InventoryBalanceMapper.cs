using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Balances.Mapping;

public sealed class InventoryBalanceMapper
{
    public InventoryBalanceDto ToRead(InventoryBalance source)
    {
        return new InventoryBalanceDto(
            source.Id,
            source.WarehouseId,
            source.ProductVariantId,
            source.OnHandQuantity,
            source.ReservedQuantity,
            source.OnOrderQuantity,
            source.AvailableQuantity,
            source.AverageUnitCost,
            source.InventoryValue,
            source.LastMovementAtUtc);
    }
}
