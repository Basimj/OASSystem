namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record CancelPurchaseInvoiceRequest(string? Reason, string RowVersion);
