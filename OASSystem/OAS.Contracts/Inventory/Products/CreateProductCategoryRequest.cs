namespace OAS.Contracts.Inventory.Products;

public sealed record CreateProductCategoryRequest(
    string Code,
    string NameAr,
    string? NameEn,
    Guid? ParentCategoryId);