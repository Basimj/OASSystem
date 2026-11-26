using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Abstractions;

public interface IAccountingReportingQueryService
{
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(BalanceSheetFilterDto filter, CancellationToken cancellationToken = default);
    Task<IncomeStatementReportDto> GetIncomeStatementAsync(IncomeStatementFilterDto filter, CancellationToken cancellationToken = default);

    Task<AccountingDashboardDto> GetAccountingDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(
        GeneralLedgerFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        TrialBalanceFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<JournalReportDto> GetJournalReportAsync(
        JournalReportFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<FiscalCloseReadinessDto> GetFiscalPeriodCloseReadinessAsync(
        Guid fiscalPeriodId,
        CancellationToken cancellationToken = default);

    Task<FiscalCloseReadinessDto> GetFiscalYearCloseReadinessAsync(
        Guid fiscalYearId,
        CancellationToken cancellationToken = default);
}
