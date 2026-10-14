namespace OAS.Application.Accounting.CoreHardening.Services;

public interface IFiscalPeriodIntegrityService
{
    Task ValidateAsync(
        Guid fiscalYearId,
        byte periodNumber,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludingPeriodId = null,
        CancellationToken cancellationToken = default);

    Task ValidateDateChangeAgainstJournalHistoryAsync(
        Guid fiscalPeriodId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
