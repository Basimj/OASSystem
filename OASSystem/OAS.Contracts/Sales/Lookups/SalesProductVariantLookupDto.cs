using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesProductVariantLookupDto(
    Guid Id,
    Guid ProductId,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    string? ProductTypeSystemKey,
    SalesLineType SalesLineType,
    Guid CategoryId,
    string ProductCode,
    string ProductName,
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
    decimal? SPH,
    decimal? CYL,
    decimal? ADD,
    bool IsActive,
    string? LensType = null,
    string? LensMaterial = null,
    string? LensCoating = null,
    decimal? LensRefractiveIndex = null);
