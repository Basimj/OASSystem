namespace OAS.Contracts.Purchasing.PurchaseReceipts;

public sealed record CancelPurchaseReceiptRequest(string? Reason, string RowVersion);
