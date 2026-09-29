namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PurchaseInvoicePostResultDto(
    Guid PurchaseInvoiceId,
    Guid JournalEntryId,
    string PurchaseInvoiceCode);
