using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Abstractions;

public interface IAccountingReportingQueryService
{
    Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<FiscalCloseReadinessDto> GetFiscalPeriodCloseReadinessAsync(
        Guid fiscalPeriodId,
        CancellationToken cancellationToken = default);

    Task<FiscalCloseReadinessDto> GetFiscalYearCloseReadinessAsync(
        Guid fiscalYearId,
        CancellationToken cancellationToken = default);
}
