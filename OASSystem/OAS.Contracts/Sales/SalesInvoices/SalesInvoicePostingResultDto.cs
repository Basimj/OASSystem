namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record SalesInvoicePostingResultDto(
    Guid InvoiceId,
    string InvoiceCode,
    Guid JournalEntryId,
    IReadOnlyList<Guid> InventoryTransactionIds,
    string RowVersion,
    Guid? ReceiptVoucherId = null,
    string? ReceiptVoucherNumber = null);
