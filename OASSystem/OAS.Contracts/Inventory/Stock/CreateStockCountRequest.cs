namespace OAS.Contracts.Inventory.Stock;

public sealed record CreateStockCountRequest(
    string CountNumber,
    Guid WarehouseId,
    DateOnly CountDate,
    string? Notes);