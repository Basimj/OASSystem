namespace OAS.Contracts.Inventory.Products;

public sealed record ProductCategoryDto(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    Guid? ParentCategoryId,
    bool IsActive);