using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Reports;

public sealed record GeneralLedgerFilterDto(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? FiscalYearId = null,
    Guid? FiscalPeriodId = null,
    Guid? AccountId = null,
    bool IncludeAccountSubtree = false,
    JournalType? JournalType = null,
    string? SourceModule = null,
    Guid? CustomerId = null,
    Guid? SupplierId = null,
    string? Reference = null,
    bool IncludeOpeningBalance = true);

public sealed record GeneralLedgerLineDto(
    Guid JournalEntryId,
    string JournalNumber,
    DateOnly PostingDate,
    DateOnly DocumentDate,
    string Description,
    string? SourceModule,
    string? SourceDocumentType,
    Guid? SourceDocumentId,
    int LineNumber,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    JournalType? JournalType = null,
    Guid? CustomerId = null,
    Guid? SupplierId = null,
    string? TransactionCurrencyCode = null,
    decimal? TransactionDebit = null,
    decimal? TransactionCredit = null,
    decimal? ExchangeRate = null,
    string? Reference = null);

public sealed record GeneralLedgerAccountDto(
    Guid AccountId,
    string AccountCode,
    string AccountNameAr,
    string? AccountNameEn,
    AccountClass AccountClass,
    NormalBalance NormalBalance,
    decimal OpeningBalance,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal ClosingBalance,
    IReadOnlyList<GeneralLedgerLineDto> Lines,
    byte Level = 1,
    Guid? ParentAccountId = null,
    AccountType AccountType = AccountType.Posting);

public sealed record GeneralLedgerReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? AccountId,
    IReadOnlyList<GeneralLedgerAccountDto> Accounts,
    decimal TotalDebit,
    decimal TotalCredit);

public sealed record TrialBalanceFilterDto(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly? AsOfDate = null,
    Guid? FiscalYearId = null,
    Guid? FiscalPeriodId = null,
    byte? AccountLevel = null,
    bool PostingAccountsOnly = false,
    bool IncludeZeroBalance = false,
    bool GroupedTree = false);

public sealed record TrialBalanceRowDto(
    Guid AccountId,
    string AccountCode,
    string AccountNameAr,
    string? AccountNameEn,
    AccountClass AccountClass,
    NormalBalance NormalBalance,
    decimal OpeningDebit,
    decimal OpeningCredit,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal ClosingDebit,
    decimal ClosingCredit,
    byte Level = 1,
    Guid? ParentAccountId = null,
    AccountType AccountType = AccountType.Posting,
    bool IsAggregate = false);

public sealed record TrialBalanceReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<TrialBalanceRowDto> Rows,
    decimal OpeningDebitTotal,
    decimal OpeningCreditTotal,
    decimal PeriodDebitTotal,
    decimal PeriodCreditTotal,
    decimal ClosingDebitTotal,
    decimal ClosingCreditTotal,
    bool IsBalanced);

public sealed record JournalReportFilterDto(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? FiscalYearId = null,
    Guid? FiscalPeriodId = null,
    JournalType? JournalType = null,
    string? SourceModule = null,
    JournalEntryStatus? Status = null,
    Guid? AccountId = null,
    Guid? UserId = null,
    string? JournalNumber = null,
    string? Reference = null,
    int PageNumber = 1,
    int PageSize = 100);

public sealed record JournalReportLineDto(
    Guid JournalEntryId,
    string JournalNumber,
    DateOnly PostingDate,
    DateOnly DocumentDate,
    JournalType JournalType,
    JournalEntryStatus Status,
    Guid AccountId,
    string AccountCode,
    string AccountNameAr,
    int LineNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    string? SourceModule,
    string? SourceDocumentType,
    Guid? SourceDocumentId,
    string? CreatedBy,
    Guid? PostedBy,
    Guid? CustomerId,
    Guid? SupplierId,
    string? Reference);

public sealed record JournalReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<JournalReportLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    long TotalCount,
    int PageNumber,
    int PageSize);

public sealed record FiscalCloseCheckDto(
    string Code,
    string Description,
    bool Passed,
    long BlockingCount);

public sealed record FiscalCloseReadinessDto(
    Guid ScopeId,
    string ScopeCode,
    DateOnly StartDate,
    DateOnly EndDate,
    bool CanClose,
    IReadOnlyList<FiscalCloseCheckDto> Checks);
