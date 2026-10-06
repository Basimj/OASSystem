namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record SupplierPurchaseHistoryItemDto(
    Guid SupplierId,
    string SupplierName,
    Guid ProductVariantId,
    string? ProductCode,
    string ProductName,
    Guid PurchaseOrderId,
    string PurchaseOrderCode,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    Guid? PurchaseReceiptId,
    string? ReceiptCode,
    DateOnly? ReceiptDate,
    decimal OrderedQuantity,
    decimal AcceptedQuantity,
    decimal? UnitPrice,
    decimal? ActualUnitCost);
