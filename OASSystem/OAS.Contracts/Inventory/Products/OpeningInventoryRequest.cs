namespace OAS.Contracts.Inventory.Products;

public sealed record OpeningInventoryRequest(
    Guid WarehouseId,
    decimal Quantity,
    decimal UnitCost);
