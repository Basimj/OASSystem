using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.CoreHardening.Services;

public sealed class FiscalPeriodIntegrityService(
    IReadRepository<FiscalYear, Guid> fiscalYears,
    IReadRepository<FiscalPeriod, Guid> fiscalPeriods,
    IReadRepository<JournalEntry, Guid> journals) : IFiscalPeriodIntegrityService
{
    public async Task ValidateAsync(
        Guid fiscalYearId,
        byte periodNumber,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludingPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        var fiscalYear = await fiscalYears.GetByIdAsync(fiscalYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(FiscalYear), fiscalYearId);

        if (fiscalYear.Status == FiscalYearStatus.Closed)
        {
            throw new ConflictException(
                "fiscal_year_closed",
                "Fiscal periods cannot be created or structurally changed inside a closed fiscal year.");
        }

        if (startDate < fiscalYear.StartDate || endDate > fiscalYear.EndDate)
        {
            throw new ConflictException(
                "fiscal_period_outside_year",
                "Fiscal period dates must be fully contained within the fiscal year.");
        }

        var duplicateNumber = await fiscalPeriods.CountAsync(
            new Specification<FiscalPeriod>().Where(x =>
                x.FiscalYearId == fiscalYearId &&
                x.PeriodNumber == periodNumber &&
                (!excludingPeriodId.HasValue || x.Id != excludingPeriodId.Value)),
            cancellationToken);
        if (duplicateNumber > 0)
        {
            throw new ConflictException(
                "fiscal_period_number_exists",
                "This period number already exists in the selected fiscal year.");
        }

        var overlaps = await fiscalPeriods.CountAsync(
            new Specification<FiscalPeriod>().Where(x =>
                x.FiscalYearId == fiscalYearId &&
                (!excludingPeriodId.HasValue || x.Id != excludingPeriodId.Value) &&
                x.StartDate <= endDate &&
                x.EndDate >= startDate),
            cancellationToken);
        if (overlaps > 0)
        {
            throw new ConflictException(
                "fiscal_period_overlap",
                "Fiscal periods within the same fiscal year cannot overlap.");
        }
    }

    public async Task ValidateDateChangeAgainstJournalHistoryAsync(
        Guid fiscalPeriodId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var outside = await journals.CountAsync(
            new Specification<JournalEntry>().Where(x =>
                x.FiscalPeriodId == fiscalPeriodId &&
                (x.PostingDate < startDate || x.PostingDate > endDate)),
            cancellationToken);

        if (outside > 0)
        {
            throw new ConflictException(
                "fiscal_period_dates_conflict_with_journals",
                "The new fiscal period dates would exclude existing journal entries that belong to this period.");
        }
    }
}
