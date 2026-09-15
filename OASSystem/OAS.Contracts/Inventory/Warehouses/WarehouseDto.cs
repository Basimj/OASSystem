namespace OAS.Contracts.Inventory.Warehouses;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    string? Description,
    bool IsDefault,
    bool IsActive);