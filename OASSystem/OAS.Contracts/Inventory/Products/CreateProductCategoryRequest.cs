namespace OAS.Contracts.Inventory.Products;
public sealed record CreateProductCategoryRequest(string Code, string Name, Guid ProductTypeId);
