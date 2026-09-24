namespace OAS.Contracts.Inventory.Products;

public sealed record ProductTypeDto(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    string? SystemKey,
    bool IsActive);
