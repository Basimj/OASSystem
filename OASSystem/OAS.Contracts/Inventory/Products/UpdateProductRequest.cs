namespace OAS.Contracts.Inventory.Products;
public sealed record UpdateProductRequest(string ProductCode, string Name, Guid CategoryId, Guid? BrandId, string? Description, bool IsStockItem, bool IsActive);
