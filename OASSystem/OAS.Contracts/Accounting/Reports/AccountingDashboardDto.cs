namespace OAS.Contracts.Accounting.Reports;

public sealed record AccountingDashboardDto(
    long UnpostedJournalCount,
    Guid? CurrentFiscalPeriodId,
    string CurrentFiscalPeriodName,
    DateOnly? CurrentFiscalPeriodEndDate,
    long OpenCashShiftCount,
    long ActiveBankAccountCount,
    IReadOnlyList<AccountingDashboardAttentionDto> AttentionItems,
    IReadOnlyList<AccountingDashboardActivityDto> RecentActivities);

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
