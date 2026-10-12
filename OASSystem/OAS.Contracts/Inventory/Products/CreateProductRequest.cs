namespace OAS.Contracts.Inventory.Products;
public sealed record CreateProductRequest(string ProductCode, string Name, Guid CategoryId, Guid? BrandId, string? Description, bool IsStockItem = true);
