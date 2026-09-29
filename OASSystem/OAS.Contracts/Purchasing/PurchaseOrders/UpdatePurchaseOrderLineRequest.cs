namespace OAS.Contracts.Purchasing.PurchaseOrders;

public sealed record UpdatePurchaseOrderLineRequest(
    Guid? Id,
    int LineSequence,
    Guid ProductVariantId,
    Guid? SupplierCatalogItemId,
    Guid PurchaseUnitId,
    decimal UnitConversionFactor,
    decimal OrderedQuantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxRate,
    DateOnly? ExpectedDeliveryDate,
    string? Notes,
    IReadOnlyList<UpdatePurchaseOrderLineSourceRequest> Sources,
    string? RowVersion);
