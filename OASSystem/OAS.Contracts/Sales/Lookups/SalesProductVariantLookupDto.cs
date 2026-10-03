using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesProductVariantLookupDto(
    Guid Id,
    Guid ProductId,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeNameAr,
    string? ProductTypeSystemKey,
    SalesLineType SalesLineType,
    Guid CategoryId,
    string ProductCode,
    string ProductNameAr,
    string SKU,
    string? Barcode,
    string? VariantName,
    string? Color,
    string? Size,
    Guid? UnitId,
    string? UnitName,
    decimal SellingPrice,
    bool IsStockItem,
    bool IsPrescriptionLens,
    decimal? SphereMin,
    decimal? SphereMax,
    decimal? CylinderMin,
    decimal? CylinderMax,
    decimal? AddMin,
    decimal? AddMax,
    bool IsActive);
