using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory;

namespace OAS.Contracts.Inventory.Products;

public sealed record CreateProductRequest(
    string ProductCode,
    string NameAr,
    string? NameEn,
    Guid CategoryId,
    Guid? BrandId,
    ProductType ProductType,
    string? Description,
    bool IsStockItem = true);