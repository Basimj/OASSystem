namespace OAS.Contracts.Accounting.Reports;

public sealed record AccountingDashboardDto(
    long UnpostedJournalCount,
    Guid? CurrentFiscalPeriodId,
    string CurrentFiscalPeriodName,
    DateOnly? CurrentFiscalPeriodEndDate,
    long OpenCashShiftCount,
    long ActiveBankAccountCount,
    IReadOnlyList<AccountingDashboardAttentionDto> AttentionItems,
    IReadOnlyList<AccountingDashboardActivityDto> RecentActivities,
    AccountingDashboardJournalCountsDto? JournalCounts = null,
    AccountingDashboardFinancialDto? Financial = null,
    DateOnly? CurrentFiscalPeriodStartDate = null,
    string? CurrentFiscalPeriodStatus = null,
    DateTimeOffset? GeneratedAtUtc = null);

// Financial values are BASE-CURRENCY, posted-only balances as of AsOfDate.
// Expenses are posted expense vouchers with an expense date inside the period.
public sealed record AccountingDashboardFinancialDto(
    string CurrencyCode,
    byte DecimalPlaces,
    DateOnly AsOfDate,
    DateOnly? PeriodStartDate,
    decimal BankBalance,
    decimal CustomerDebitBalances,
    decimal SupplierCreditBalances,
    decimal PostedPeriodExpenses);

public sealed record AccountingDashboardJournalCountsDto(
    long Draft,
    long PendingApproval,
    long Approved);

public sealed record AccountingDashboardAttentionDto(
    string Code,
    string Title,
    string Severity,
    string? Meta = null);

public sealed record AccountingDashboardActivityDto(
    Guid EntityId,
    string ActivityType,
    string Reference,
    byte Status,
    string? Description,
    DateTimeOffset OccurredAtUtc);
