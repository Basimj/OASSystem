namespace OAS.Contracts.Inventory.Products;
public sealed record ProductCategoryDto(Guid Id, string Code, string Name, Guid ProductTypeId, bool IsActive);
