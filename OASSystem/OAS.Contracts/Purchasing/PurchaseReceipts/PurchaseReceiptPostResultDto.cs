namespace OAS.Contracts.Purchasing.PurchaseReceipts;

public sealed record PurchaseReceiptPostResultDto(
    Guid PurchaseReceiptId,
    Guid InventoryTransactionId,
    Guid JournalEntryId,
    Guid PurchaseOrderId,
    string ReceiptCode);
