namespace OAS.Contracts.Accounting.Reports;

// Document amounts are not automatically AP balances. The journal statement and aging
// endpoints remain the authoritative ledgers; these rows provide drill-down only.
public sealed record SupplierWorkspaceDocumentsFilter(
    Guid SupplierId,
    string DocumentType,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Search = null,
    string? CurrencyCode = null,
    int PageNumber = 1,
    int PageSize = 50);

public sealed record SupplierWorkspaceDocumentRowDto(
    Guid Id,
    string DocumentType,
    string Reference,
    DateOnly DocumentDate,
    byte Status,
    decimal? DocumentAmount,
    decimal? BaseAmount,
    string? CurrencyCode,
    Guid? JournalEntryId,
    Guid? PaymentVoucherLineId = null,
    decimal? UnappliedPaymentBaseAmount = null);

public sealed record SupplierWorkspaceDocumentsDto(
    Guid SupplierId,
    string DocumentType,
    IReadOnlyList<SupplierWorkspaceDocumentRowDto> Rows,
    long TotalCount,
    int PageNumber,
    int PageSize);
