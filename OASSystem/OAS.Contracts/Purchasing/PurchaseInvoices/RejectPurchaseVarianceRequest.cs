namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record RejectPurchaseVarianceRequest(
    Guid AllocationId,
    string Reason,
    string AllocationRowVersion,
    string InvoiceRowVersion);
