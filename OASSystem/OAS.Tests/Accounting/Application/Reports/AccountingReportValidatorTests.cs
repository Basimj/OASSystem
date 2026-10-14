using NUnit.Framework;
using OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;
using OAS.Application.Accounting.Reports.Queries.GetJournalReport;
using OAS.Application.Accounting.Reports.Queries.GetTrialBalance;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Tests.Accounting.Application.Reports;

[TestFixture]
public sealed class AccountingReportValidatorTests
{
    [Test]
    public void GeneralLedger_InvalidDateRange_IsRejected()
    {
        var result = new GetGeneralLedgerQueryValidator().Validate(
            new GetGeneralLedgerQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "report_date_range_invalid"), Is.True);
    }

    [Test]
    public void TrialBalance_InvalidAsOfDate_IsRejected()
    {
        var result = new GetTrialBalanceQueryValidator().Validate(
            new GetTrialBalanceQuery(
                new DateOnly(2026, 2, 1),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 1, 31)));

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void JournalReport_PageSizeOverLimit_IsRejected()
    {
        var filter = new JournalReportFilterDto(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            PageSize: 501);
        var result = new GetJournalReportQueryValidator().Validate(new GetJournalReportQuery(filter));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "page_size_invalid"), Is.True);
    }
}
