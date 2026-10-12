namespace OAS.Contracts.Inventory.Products;
public sealed record ProductTypeDto(Guid Id, string Code, string Name, string? SystemKey, bool IsActive);
