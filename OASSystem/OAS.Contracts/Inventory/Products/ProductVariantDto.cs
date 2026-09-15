namespace OAS.Contracts.Inventory.Products;

public sealed record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string SKU,
    string? Barcode,
    string? VariantName,
    string? Color,
    string? Size,
    Guid? UnitId,
    decimal PurchasePrice,
    decimal SellingPrice,
    bool IsActive);