namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record ApprovePurchaseVarianceRequest(
    Guid AllocationId,
    string ApprovalReason,
    string AllocationRowVersion,
    string InvoiceRowVersion);
