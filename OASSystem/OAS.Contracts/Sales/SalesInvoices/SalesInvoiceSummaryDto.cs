using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record SalesInvoiceSummaryDto(
    Guid Id,
    string InvoiceCode,
    Guid CustomerId,
    string? CustomerCode,
    string? CustomerName,
    DateOnly InvoiceDate,
    DateOnly PostingDate,
    SalesInvoiceStatus Status,
    string CurrencyCodeSnapshot,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    Guid? JournalEntryId,
    string RowVersion);
