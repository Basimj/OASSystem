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

/// <summary>
/// Lightweight product-variant lookup item used by purchasing/inventory selectors.
/// ProductName/ProductCode belong to the parent product, while SKU/VariantName
/// identify the concrete variant selected by the user.
/// </summary>
public sealed record ProductVariantLookupDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string SKU,
    string? Barcode,
    string? VariantName,
    string? Color,
    string? Size,
    Guid? UnitId,
    bool IsActive);
