namespace OAS.Contracts.Inventory.Products;
public sealed record UpdateProductTypeRequest(string Code, string Name, bool IsActive);
