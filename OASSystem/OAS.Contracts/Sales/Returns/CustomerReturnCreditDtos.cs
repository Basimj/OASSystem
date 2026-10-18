namespace OAS.Contracts.Sales.Returns;

/// <summary>
/// Read-only accounting preview. These amounts must not authorize a refund or
/// credit application. A dedicated, locked ledger is required for write operations.
/// </summary>
public sealed record CustomerReturnCreditPreviewDto(
    Guid CustomerId,
    decimal TotalHistoricalBaseCredit,
    IReadOnlyList<CustomerReturnCreditItemDto> Returns);

public sealed record CustomerReturnCreditItemDto(
    Guid SalesReturnId,
    string ReturnCode,
    Guid SalesInvoiceId,
    string InvoiceCode,
    Guid CustomerId,
    DateOnly ReturnDate,
    Guid CurrencyId,
    string CurrencyCode,
    Guid BaseCurrencyId,
    string BaseCurrencyCode,
    decimal ReturnAmount,
    decimal AppliedAgainstOriginalInvoice,
    decimal EstimatedUnappliedCredit,
    decimal EstimatedUnappliedBaseCredit);
