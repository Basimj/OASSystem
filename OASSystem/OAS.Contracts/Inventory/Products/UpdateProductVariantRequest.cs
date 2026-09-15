namespace OAS.Contracts.Inventory.Products;

public sealed record UpdateProductVariantRequest(
    string? Barcode,
    string? VariantName,
    string? Color,
    string? Size,
    Guid? UnitId,
    decimal PurchasePrice,
    decimal SellingPrice,
    bool IsActive);