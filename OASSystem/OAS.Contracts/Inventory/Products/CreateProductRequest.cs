namespace OAS.Contracts.Inventory.Products;

public sealed record CreateProductRequest(
    string ProductCode,
    string NameAr,
    string? NameEn,
    Guid CategoryId,
    Guid? BrandId,
    Guid ProductTypeId,
    string? Description,
    bool IsStockItem = true);
