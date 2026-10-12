namespace OAS.Contracts.Inventory.Products;
public sealed record UpdateUnitRequest(string Code, string Name, bool IsActive);
