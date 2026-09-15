namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateUnitRequest(
    string NameAr,
    string? NameEn,
    bool IsActive);