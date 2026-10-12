namespace OAS.Contracts.Inventory.Products;
public sealed record UpdateProductCategoryRequest(string Code, string Name, Guid ProductTypeId, bool IsActive);
