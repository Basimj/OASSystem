using OAS.Contracts.Common.Pagination;

namespace OAS.Contracts.Accounting.Reports;

public sealed record SupplierStatementFilterDto(
    Guid SupplierId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reference = null,
    string? SourceDocumentType = null,
    string? CurrencyCode = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    SortDirection SortDirection = SortDirection.Ascending);

public sealed record SupplierStatementMovementDto(
    Guid JournalEntryId,
    string JournalNumber,
    int LineNumber,
    DateOnly PostingDate,
    DateOnly DocumentDate,
    string Description,
    string? SourceModule,
    string? SourceDocumentType,
    Guid? SourceDocumentId,
    Guid? SourceDocumentLineId,
    string? PostingRole,
    decimal DebitBaseAmount,
    decimal CreditBaseAmount,
    decimal LiabilityDeltaBaseAmount,
    decimal RunningLiabilityBaseAmount,
    string? TransactionCurrencyCode,
    decimal? TransactionDebitAmount,
    decimal? TransactionCreditAmount,
    decimal? ExchangeRate,
    string MovementType,
    string Reference,
    bool IsReversedOriginal);

public sealed record SupplierMonthlyBalanceDto(
    int Year,
    int Month,
    decimal OpeningLiabilityBaseAmount,
    decimal DebitBaseAmount,
    decimal CreditBaseAmount,
    decimal ClosingLiabilityBaseAmount);

public sealed record SupplierOperationalExposureDto(
    decimal PurchaseOrdersBaseAmount,
    long PurchaseOrderCount,
    decimal ReceiptGrniCreatedBaseAmount,
    long PurchaseReceiptCount,
    decimal GrniClearedBaseAmount,
    decimal OpenGrniBaseAmount);

public sealed record SupplierStatementDto(
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningLiabilityBaseAmount,
    decimal PeriodDebitBaseAmount,
    decimal PeriodCreditBaseAmount,
    decimal ClosingLiabilityBaseAmount,
    long DistinctDocumentCount,
    IReadOnlyList<SupplierStatementMovementDto> Movements,
    IReadOnlyList<SupplierMonthlyBalanceDto> MonthlyBalances,
    SupplierOperationalExposureDto OperationalExposure,
    long TotalCount,
    int PageNumber,
    int PageSize);

public sealed record SupplierBalancesFilterDto(
    DateOnly AsOfDate,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    SortDirection SortDirection = SortDirection.Ascending,
    bool IncludeZeroBalance = false);

public sealed record SupplierBalanceRowDto(
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal DebitToDateBaseAmount,
    decimal CreditToDateBaseAmount,
    decimal LiabilityBalanceBaseAmount,
    decimal DebitBalanceBaseAmount,
    decimal CreditBalanceBaseAmount);

public sealed record SupplierBalancesDto(
    DateOnly AsOfDate,
    IReadOnlyList<SupplierBalanceRowDto> Rows,
    decimal TotalLiabilityBaseAmount,
    decimal TotalDebitBalanceBaseAmount,
    decimal TotalCreditBalanceBaseAmount,
    long TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ApReconciliationDto(
    DateOnly AsOfDate,
    Guid? ControlAccountId,
    string? ControlAccountCode,
    string? ControlAccountName,
    bool ControlAccountDerived,
    decimal SupplierSubledgerLiabilityBaseAmount,
    decimal ControlAccountTreeLiabilityBaseAmount,
    decimal DifferenceBaseAmount,
    bool IsReconciled,
    long SupplierCount,
    decimal GrniBalanceBaseAmount);
