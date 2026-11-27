using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.FiscalPeriods;

namespace OAS.Client.Accounting.Common;

public static class FiscalReportRange
{
    public static (DateTime From, DateTime To)? Resolve(IEnumerable<FiscalYearDto> years, IEnumerable<FiscalPeriodDto> periods, string yearId, string periodId)
    {
        var period = periods.FirstOrDefault(p => p.Id.ToString() == periodId && p.FiscalYearId.ToString() == yearId);
        if (period is not null) return (period.StartDate.ToDateTime(TimeOnly.MinValue), period.EndDate.ToDateTime(TimeOnly.MinValue));
        var year = years.FirstOrDefault(y => y.Id.ToString() == yearId);
        return year is null ? null : (year.StartDate.ToDateTime(TimeOnly.MinValue), year.EndDate.ToDateTime(TimeOnly.MinValue));
    }
}
