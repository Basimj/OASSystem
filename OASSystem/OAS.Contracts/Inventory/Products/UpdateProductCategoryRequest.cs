namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateProductCategoryRequest(
    string NameAr,
    string? NameEn,
    Guid? ParentCategoryId,
    bool IsActive);