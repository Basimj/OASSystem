namespace OAS.Contracts.Inventory.Warehouses;

public sealed record UpdateWarehouseRequest(
    string NameAr,
    string? NameEn,
    string? Description,
    bool IsDefault,
    bool IsActive);