namespace OAS.Contracts.Inventory.Products;

public sealed record CreateStockProductResult(
    Guid ProductId,
    string ProductCode,
    Guid? VariantId,
    string? SKU,
    bool OpeningInventoryCreated,
    Guid? WarehouseId,
    decimal? OpeningQuantity,
    decimal? UnitCost,
    Guid? InventoryTransactionId);
