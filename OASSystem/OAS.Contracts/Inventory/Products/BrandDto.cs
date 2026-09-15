namespace OAS.Contracts.Inventory.Products;

public sealed record BrandDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);