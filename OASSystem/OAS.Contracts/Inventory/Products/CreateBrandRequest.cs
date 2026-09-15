namespace OAS.Contracts.Inventory.Products;

public sealed record CreateBrandRequest(
    string Code,
    string Name);