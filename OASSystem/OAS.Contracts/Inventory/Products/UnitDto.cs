namespace OAS.Contracts.Inventory.Products;

public sealed record UnitDto(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    bool IsActive);