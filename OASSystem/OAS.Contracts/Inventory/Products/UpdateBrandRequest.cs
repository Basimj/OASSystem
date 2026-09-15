namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateBrandRequest(
    string Name,
    bool IsActive);