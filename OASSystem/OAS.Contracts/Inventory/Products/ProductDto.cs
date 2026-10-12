namespace OAS.Contracts.Inventory.Products;
public sealed record ProductDto(Guid Id, string ProductCode, string Name, Guid CategoryId, Guid? BrandId, string? Description, bool IsStockItem, bool IsActive);
