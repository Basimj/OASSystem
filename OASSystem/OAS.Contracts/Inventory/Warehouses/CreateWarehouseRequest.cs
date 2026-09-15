namespace OAS.Contracts.Inventory.Warehouses;

public sealed record CreateWarehouseRequest(
    string Code,
    string NameAr,
    string? NameEn,
    string? Description,
    bool IsDefault = false);