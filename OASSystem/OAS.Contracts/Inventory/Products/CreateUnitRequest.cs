namespace OAS.Contracts.Inventory.Products;

public sealed record CreateUnitRequest(
    string Code,
    string NameAr,
    string? NameEn);