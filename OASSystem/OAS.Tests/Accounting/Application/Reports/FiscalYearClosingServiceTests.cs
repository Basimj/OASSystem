using NUnit.Framework;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.FiscalYears.Services;
using OAS.Contracts.Accounting.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.Reports;

[TestFixture]
public sealed class FiscalYearClosingServiceTests
{
    [Test]
    public async Task CloseAsync_CreatesBalancedClosingJournal_AndMovesNetProfitToRetainedEarnings()
    {
        var year = FiscalYear.Create(
            Guid.NewGuid(), "FY26", "2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), FiscalYearStatus.Closing);
        var period = FiscalPeriod.Create(
            Guid.NewGuid(), year.Id, 12, "ديسمبر", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31),
            FiscalPeriodStatus.SoftClosed, true, true, false);

        var currency = Currency.Create(Guid.NewGuid(), "YER", "ريال يمني", "Yemeni Rial", "ر.ي", 2);
        var retained = Account.Create(
            Guid.NewGuid(), "3100", "الأرباح المحتجزة", "Retained Earnings", null, 1,
            AccountClass.Equity, AccountType.Detail, NormalBalance.Credit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: false,
            isSystemAccount: true, isActive: true, effectiveDate: new DateOnly(2026, 1, 1));
        var asset = Account.Create(
            Guid.NewGuid(), "1100", "النقدية", "Cash", null, 1,
            AccountClass.Asset, AccountType.Detail, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: false,
            isSystemAccount: true, isActive: true, effectiveDate: new DateOnly(2026, 1, 1));
        var revenue = Account.Create(
            Guid.NewGuid(), "4100", "إيرادات المبيعات", "Sales Revenue", null, 1,
            AccountClass.Revenue, AccountType.Detail, NormalBalance.Credit,
            true, false, false, true, true, new DateOnly(2026, 1, 1));
        var expense = Account.Create(
            Guid.NewGuid(), "5100", "تكلفة ومصروف", "Expense", null, 1,
            AccountClass.Expense, AccountType.Detail, NormalBalance.Debit,
            true, false, false, true, true, new DateOnly(2026, 1, 1));

        var settings = AccountingSettings.Create(currency.Id, retainedEarningsAccountId: retained.Id);
        var journals = new FakeRepository<JournalEntry, Guid>();
        var reports = new StubAccountingReports(new TrialBalanceReportDto(
            year.StartDate,
            year.EndDate,
            [
                Row(asset, periodDebit: 400m, periodCredit: 0m),
                Row(revenue, periodDebit: 0m, periodCredit: 1_000m),
                Row(expense, periodDebit: 600m, periodCredit: 0m)
            ],
            0m, 0m, 1_000m, 1_000m, 1_000m, 1_000m, true));

        var service = new FiscalYearClosingService(
            new FakeRepository<FiscalPeriod, Guid>([period]),
            new FakeRepository<AccountingSettings, Guid>([settings]),
            new FakeRepository<Currency, Guid>([currency]),
            new FakeRepository<Account, Guid>([retained, asset, revenue, expense]),
            journals,
            reports,
            new FixedSequence(7));

        var actor = Guid.NewGuid();
        var journalId = await service.CloseAsync(year, actor, new DateTime(2026, 12, 31, 20, 0, 0, DateTimeKind.Utc));

        Assert.That(journalId, Is.Not.Null);
        Assert.That(year.Status, Is.EqualTo(FiscalYearStatus.Closed));
        Assert.That(period.Status, Is.EqualTo(FiscalPeriodStatus.Closed));
        Assert.That(period.AccountingLocked, Is.True);
        Assert.That(journals.LastAdded, Is.Not.Null);
        Assert.That(journals.LastAdded!.JournalType, Is.EqualTo(JournalType.Closing));
        Assert.That(journals.LastAdded.Status, Is.EqualTo(JournalEntryStatus.Posted));
        Assert.That(journals.LastAdded.IsBalanced(), Is.True);
        Assert.That(journals.LastAdded.TotalDebit(), Is.EqualTo(1_000m));
        Assert.That(journals.LastAdded.TotalCredit(), Is.EqualTo(1_000m));

        var retainedLine = journals.LastAdded.Lines.Single(x => x.AccountId == retained.Id);
        Assert.That(retainedLine.DebitAmount, Is.EqualTo(0m));
        Assert.That(retainedLine.CreditAmount, Is.EqualTo(400m));
    }

    private static TrialBalanceRowDto Row(Account account, decimal periodDebit, decimal periodCredit) => new(
        account.Id,
        account.Code,
        account.NameAr,
        account.NameEn,
        (OAS.Contracts.Accounting.Enums.AccountClass)(byte)account.AccountClass,
        (OAS.Contracts.Accounting.Enums.NormalBalance)(byte)account.NormalBalance,
        0m,
        0m,
        periodDebit,
        periodCredit,
        Math.Max(periodDebit - periodCredit, 0m),
        Math.Max(periodCredit - periodDebit, 0m));

    private sealed class FixedSequence(long value) : ISequenceNumberGenerator
    {
        public Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default) => Task.FromResult(value);
    }

    private sealed class StubAccountingReports(TrialBalanceReportDto trialBalance) : IAccountingReportingQueryService
    {
        public Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
            => Task.FromResult(trialBalance);

        public Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(DateOnly fromDate, DateOnly toDate, Guid? accountId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<FiscalCloseReadinessDto> GetFiscalPeriodCloseReadinessAsync(Guid fiscalPeriodId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<FiscalCloseReadinessDto> GetFiscalYearCloseReadinessAsync(Guid fiscalYearId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
