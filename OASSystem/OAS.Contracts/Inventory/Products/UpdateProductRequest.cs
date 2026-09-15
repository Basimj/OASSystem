namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateProductRequest(
    string NameAr,
    string? NameEn,
    Guid CategoryId,
    Guid? BrandId,
    string ProductType,
    string? Description,
    bool IsStockItem,
    bool IsActive);