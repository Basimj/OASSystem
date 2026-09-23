namespace OAS.Contracts.Inventory.Products;

public sealed record InitialProductVariantRequest(
    string SKU,
    string? Barcode,
    string? VariantName,
    string? Color,
    string? Size,
    Guid? UnitId,
    decimal PurchasePrice,
    decimal SellingPrice);
