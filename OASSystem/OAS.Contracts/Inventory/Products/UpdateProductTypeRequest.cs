namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateProductTypeRequest(
    string Code,
    string NameAr,
    string? NameEn,
    bool IsActive);
