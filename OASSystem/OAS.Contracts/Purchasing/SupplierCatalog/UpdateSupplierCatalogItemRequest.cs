namespace OAS.Contracts.Purchasing.SupplierCatalog;

public sealed record UpdateSupplierCatalogItemRequest(
    string? SupplierProductCode,
    string? SupplierProductName,
    Guid PurchaseUnitId,
    decimal UnitConversionFactor,
    int? LeadTimeDays,
    decimal MinimumOrderQuantity,
    bool IsPreferred,
    bool IsActive,
    string RowVersion);
