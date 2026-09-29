namespace OAS.Contracts.Purchasing.PurchaseReceipts;

public sealed record CreatePurchaseReceiptRequest(
    Guid PurchaseOrderId,
    DateOnly ReceiptDate,
    DateOnly PostingDate,
    string? SupplierDeliveryCode,
    string? Notes,
    IReadOnlyList<CreatePurchaseReceiptLineRequest> Lines);
