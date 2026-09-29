namespace OAS.Contracts.Purchasing.PurchaseReceipts;

public sealed record CreatePurchaseReceiptLineRequest(
    Guid PurchaseOrderLineId,
    int LineSequence,
    decimal ReceivedQuantity,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    decimal ActualUnitCost,
    DateOnly? ExpiryDate,
    string? BatchCode,
    string? Notes);
