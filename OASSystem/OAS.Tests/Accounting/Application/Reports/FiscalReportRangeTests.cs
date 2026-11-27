using NUnit.Framework;
using OAS.Client.Accounting.Common;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.FiscalPeriods;

namespace OAS.Tests.Accounting.Application.Reports;

[TestFixture]
public sealed class FiscalReportRangeTests
{
    [Test]
    public void AllPeriodsRestoresActualNonCalendarFiscalYear()
    {
        var year = new FiscalYearDto(Guid.NewGuid(), "FY", "2026/27", new(2026,7,1), new(2027,6,30), FiscalYearStatus.Open, null, null, "");
        var period = new FiscalPeriodDto(Guid.NewGuid(), year.Id, 2, "August", new(2026,8,1), new(2026,8,31), FiscalPeriodStatus.Open, false, false, false, null, null, "");
        var specific = FiscalReportRange.Resolve([year], [period], year.Id.ToString(), period.Id.ToString());
        var all = FiscalReportRange.Resolve([year], [period], year.Id.ToString(), "");
        Assert.That(specific!.Value.From, Is.EqualTo(new DateTime(2026,8,1)));
        Assert.That(specific.Value.To, Is.EqualTo(new DateTime(2026,8,31)));
        Assert.That(all!.Value.From, Is.EqualTo(new DateTime(2026,7,1)));
        Assert.That(all.Value.To, Is.EqualTo(new DateTime(2027,6,30)));
        Assert.That(FiscalReportRange.Resolve([year], [period], "", ""), Is.Null);
    }
}
