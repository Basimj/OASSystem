namespace OAS.Contracts.Purchasing.SupplierCatalog;

public sealed record CreateSupplierCatalogItemRequest(
    Guid SupplierId,
    Guid ProductVariantId,
    string? SupplierProductCode,
    string? SupplierProductName,
    Guid PurchaseUnitId,
    decimal UnitConversionFactor,
    int? LeadTimeDays,
    decimal MinimumOrderQuantity,
    bool IsPreferred,
    bool IsActive);
