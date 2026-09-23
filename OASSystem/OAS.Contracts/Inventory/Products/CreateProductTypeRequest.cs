namespace OAS.Contracts.Inventory.Products;

public sealed record CreateProductTypeRequest(
    string Code,
    string NameAr,
    string? NameEn);
