using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.Reports;

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
    decimal RunningBalance);

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
    IReadOnlyList<GeneralLedgerLineDto> Lines);

public sealed record GeneralLedgerReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? AccountId,
    IReadOnlyList<GeneralLedgerAccountDto> Accounts,
    decimal TotalDebit,
    decimal TotalCredit);

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
    decimal ClosingCredit);

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
