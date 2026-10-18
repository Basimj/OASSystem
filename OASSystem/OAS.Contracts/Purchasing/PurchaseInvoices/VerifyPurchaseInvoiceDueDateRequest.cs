namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record VerifyPurchaseInvoiceDueDateRequest(
    DateOnly DueDate,
    int? PaymentTermDaysSnapshot,
    string Reason,
    string RowVersion);
