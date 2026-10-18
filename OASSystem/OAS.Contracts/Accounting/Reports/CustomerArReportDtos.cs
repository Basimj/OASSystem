namespace OAS.Contracts.Accounting.Reports;

// SA-04: All aggregate amounts are expressed in the system BASE currency.
// Positive balance = debit/receivable; negative balance = customer credit.
public sealed record CustomerStatementMovementDto(
    Guid JournalId, string JournalNumber, int LineNumber,
    DateOnly PostingDate, DateOnly DocumentDate, string Description,
    string? SourceDocumentType, Guid? SourceDocumentId,
    decimal DebitBase, decimal CreditBase, decimal BalanceBase,
    string? TransactionCurrency, decimal? TransactionDebit,
    decimal? TransactionCredit, bool IsReversal);

public sealed record CustomerStatementReportDto(
    Guid CustomerId, string CustomerCode, string CustomerName,
    Guid AccountId, string AccountCode, DateOnly From, DateOnly To,
    string BaseCurrencyCode, decimal OpeningBase, decimal PeriodDebitBase,
    decimal PeriodCreditBase, decimal ClosingBase,
    IReadOnlyList<CustomerStatementMovementDto> Movements,
    long TotalCount, int PageNumber, int PageSize);

public sealed record CustomerBalanceRowDto(
    Guid CustomerId, string CustomerCode, string CustomerName,
    Guid AccountId, string AccountCode, decimal DebitBase,
    decimal CreditBase, decimal BalanceBase);

public sealed record CustomerBalancesReportDto(
    DateOnly AsOf, string BaseCurrencyCode,
    IReadOnlyList<CustomerBalanceRowDto> Customers,
    decimal DebitBalancesBase, decimal CreditBalancesBase,
    decimal NetReceivableBase, long TotalCount, int PageNumber, int PageSize);

public sealed record CustomerAgingLineDto(
    Guid InvoiceId, Guid CustomerId, string CustomerCode,
    string CustomerName, string InvoiceNumber, DateOnly InvoiceDate,
    DateOnly PostingDate, DateOnly? DueDate,
    string Bucket, int? DaysPastDue, decimal OriginalBase,
    decimal PaymentBase, decimal ReturnBase, decimal CreditAppliedBase,
    decimal OutstandingBase, string DocumentCurrency);

public sealed record CustomerAgingReportDto(
    DateOnly AsOf, string BaseCurrencyCode,
    IReadOnlyList<CustomerAgingLineDto> Invoices,
    decimal NotDueBase, decimal Days0To30Base,
    decimal Days31To60Base, decimal Days61To90Base,
    decimal Over90Base, decimal UnknownBase, decimal TotalOutstandingBase,
    decimal UnappliedReceiptsBase, decimal UnappliedReturnCreditsBase,
    decimal CustomerAdvancesLiabilityBase,
    long TotalCount, int PageNumber, int PageSize);

public sealed record CustomerArReconciliationDto(
    DateOnly AsOf, string BaseCurrencyCode,
    Guid? ControlAccountId, string? ControlAccountCode,
    decimal ControlAccountBalanceBase, decimal CustomerSubledgerBase,
    decimal DifferenceBase, bool IsReconciled,
    bool RequiresControlAccountSelection, string? Message);

public sealed record CustomerArWorkspaceDto(
    Guid CustomerId, string CustomerCode, string CustomerName,
    CustomerStatementReportDto Statement, CustomerAgingReportDto Aging,
    decimal UnappliedReceiptsBase, decimal UnappliedReturnCreditsBase,
    decimal CustomerAdvancesLiabilityBase);

public sealed record CustomerArDocumentRowDto(
    Guid Id, string Kind, string Number, DateOnly PostingDate,
    decimal BaseAmount, string Currency, string? Status, Guid? JournalEntryId);

public sealed record CustomerArDocumentPageDto(
    string Kind, IReadOnlyList<CustomerArDocumentRowDto> Rows,
    long TotalCount, int PageNumber, int PageSize);
