namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PurchaseInvoiceMatchAllocationRequest(
    Guid PurchaseInvoiceLineId,
    Guid PurchaseReceiptLineId,
    decimal MatchedQuantity);

public sealed record MatchPurchaseInvoiceRequest(
    IReadOnlyList<PurchaseInvoiceMatchAllocationRequest> Allocations,
    string RowVersion);
