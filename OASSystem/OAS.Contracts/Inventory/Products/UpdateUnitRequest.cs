namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateUnitRequest(
    string Code,
    string NameAr,
    string? NameEn,
    bool IsActive);
