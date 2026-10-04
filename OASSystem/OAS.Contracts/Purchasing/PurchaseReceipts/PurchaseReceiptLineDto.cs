namespace OAS.Contracts.Purchasing.PurchaseReceipts;

public sealed record PurchaseReceiptLineDto(
    Guid Id,
    Guid PurchaseReceiptId,
    Guid PurchaseOrderLineId,
    int LineSequence,
    Guid ProductVariantId,
    string? ProductCode,
    string? ProductName,
    decimal OrderedQuantitySnapshot,
    decimal PreviouslyReceivedQty,
    decimal RemainingReceivableQuantity,
    decimal ReceivedQuantity,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    decimal BaseAcceptedQuantity,
    decimal ActualUnitCost,
    decimal TotalAcceptedCost,
    DateOnly? ExpiryDate,
    string? BatchCode,
    string? Notes,
    string RowVersion,
    decimal ReturnedQuantity = 0m);
