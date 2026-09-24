namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateBrandRequest(
    string Code,
    string Name,
    bool IsActive);
