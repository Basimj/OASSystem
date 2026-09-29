namespace OAS.Contracts.Purchasing.PurchaseReceipts;

public sealed record UpdatePurchaseReceiptRequest(
    DateOnly ReceiptDate,
    DateOnly PostingDate,
    string? SupplierDeliveryCode,
    string? Notes,
    IReadOnlyList<UpdatePurchaseReceiptLineRequest> Lines,
    string RowVersion);
