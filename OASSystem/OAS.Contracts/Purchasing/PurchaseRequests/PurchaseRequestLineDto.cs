namespace OAS.Contracts.Purchasing.PurchaseRequests;

public sealed record PurchaseRequestLineDto(
    Guid Id,
    Guid PurchaseRequestId,
    int LineSequence,
    Guid ProductVariantId,
    string? ProductCode,
    string? ProductName,
    decimal RequestedQuantity,
    decimal AllocatedToPurchaseOrderQuantity,
    decimal RemainingQuantity,
    DateOnly? RequiredDate,
    Guid? CustomerOrderLineId,
    Guid? PreferredSupplierId,
    string? PreferredSupplierName,
    string? Notes,
    string RowVersion);
