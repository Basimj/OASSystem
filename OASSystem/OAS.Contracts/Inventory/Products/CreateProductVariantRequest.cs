namespace OAS.Contracts.Inventory.Products;

public sealed record CreateProductVariantRequest(
    Guid ProductId,
    string SKU,
    string? Barcode,
    string? VariantName,
    string? Color,
    string? Size,
    Guid? UnitId,
    decimal PurchasePrice,
    decimal SellingPrice);