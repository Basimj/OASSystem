namespace OAS.Contracts.Inventory.Stock;

public sealed record InventoryBalanceDto(
    Guid Id,
    Guid WarehouseId,
    Guid ProductVariantId,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal OnOrderQuantity,
    decimal AvailableQuantity,
    decimal AverageUnitCost,
    decimal InventoryValue,
    DateTimeOffset? LastMovementAtUtc);