using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.FiscalYears.Services;

public sealed class FiscalYearClosingService(
    IRepository<FiscalPeriod, Guid> fiscalPeriods,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencies,
    IReadRepository<Account, Guid> accounts,
    IRepository<JournalEntry, Guid> journals,
    IAccountingReportingQueryService reports,
    ISequenceNumberGenerator sequences) : IFiscalYearClosingService
{
    private const string SourceModule = "Accounting";
    private const string SourceDocumentType = "FiscalYearClosing";

    public async Task<Guid?> CloseAsync(
        FiscalYear fiscalYear,
        Guid closedBy,
        DateTime closedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fiscalYear);

        if (fiscalYear.Status != FiscalYearStatus.Closing)
        {
            throw new ConflictException(
                "fiscal_year_not_closing",
                "يجب أن تكون السنة المالية في حالة الإقفال قبل تنفيذ قيد الإقفال السنوي.");
        }

        var periods = await fiscalPeriods.ListAsync(
            new Specification<FiscalPeriod>()
                .Where(x => x.FiscalYearId == fiscalYear.Id),
            cancellationToken);

        var lastPeriod = periods
            .OrderByDescending(x => x.EndDate)
            .ThenByDescending(x => x.PeriodNumber)
            .FirstOrDefault()
            ?? throw new ConflictException(
                "fiscal_year_has_no_periods",
                "لا يمكن إقفال سنة مالية لا تحتوي على فترات مالية.");

        if (periods.Any(x => x.Id != lastPeriod.Id && x.Status != FiscalPeriodStatus.Closed))
        {
            throw new ConflictException(
                "fiscal_year_prior_periods_open",
                "يجب إغلاق جميع الفترات السابقة قبل تنفيذ الإقفال السنوي.");
        }

        var existing = await journals.ListAsync(
            new Specification<JournalEntry>()
                .Where(x =>
                    x.SourceModule == SourceModule &&
                    x.SourceDocumentType == SourceDocumentType &&
                    x.SourceDocumentId == fiscalYear.Id &&
                    (x.Status == JournalEntryStatus.Posted || x.Status == JournalEntryStatus.Reversed)),
            cancellationToken);

        if (existing.Count > 1)
        {
            throw new ConflictException(
                "fiscal_year_closing_journal_duplicate",
                "يوجد أكثر من قيد إقفال سنوي لهذه السنة المالية.");
        }

        if (existing.Count == 1)
        {
            if (lastPeriod.Status == FiscalPeriodStatus.SoftClosed && !lastPeriod.AccountingLocked)
            {
                lastPeriod.Close(closedBy, closedAtUtc);
                fiscalPeriods.Update(lastPeriod);
            }
            else if (lastPeriod.Status != FiscalPeriodStatus.Closed)
            {
                throw new ConflictException(
                    "fiscal_year_closing_period_not_ready",
                    "فترة الإقفال السنوي ليست في حالة تسمح بإكمال الإقفال.");
            }

            fiscalYear.Close(closedBy, closedAtUtc);
            return existing[0].Id;
        }

        if (lastPeriod.Status != FiscalPeriodStatus.SoftClosed || lastPeriod.AccountingLocked)
        {
            throw new ConflictException(
                "fiscal_year_closing_period_not_ready",
                "آخر فترة مالية يجب أن تكون Soft Closed وغير مقفلة محاسبيًا قبل تنفيذ قيد الإقفال السنوي.");
        }

        var trialBalance = await reports.GetTrialBalanceAsync(
            fiscalYear.StartDate,
            fiscalYear.EndDate,
            cancellationToken);

        if (!trialBalance.IsBalanced)
        {
            throw new ConflictException(
                "trial_balance_not_balanced",
                "لا يمكن تنفيذ الإقفال السنوي لأن ميزان المراجعة غير متوازن.");
        }

        var nominalRows = trialBalance.Rows
            .Where(x =>
                x.AccountClass == OAS.Contracts.Accounting.Enums.AccountClass.Revenue ||
                x.AccountClass == OAS.Contracts.Accounting.Enums.AccountClass.Expense)
            .Select(x => new
            {
                Row = x,
                SignedPeriodBalance = x.PeriodDebit - x.PeriodCredit
            })
            .Where(x => x.SignedPeriodBalance != 0m)
            .ToList();

        if (nominalRows.Count == 0)
        {
            lastPeriod.Close(closedBy, closedAtUtc);
            fiscalPeriods.Update(lastPeriod);
            fiscalYear.Close(closedBy, closedAtUtc);
            return null;
        }

        var settings = await settingsRepository.GetByIdAsync(
            AccountingSettings.SingletonId,
            cancellationToken)
            ?? throw new ConflictException(
                "accounting_settings_required",
                "يجب حفظ إعدادات المحاسبة قبل إقفال السنة المالية.");

        if (!settings.RetainedEarningsAccountId.HasValue)
        {
            throw new ConflictException(
                "retained_earnings_account_required",
                "حدد حساب الأرباح المحتجزة في إعدادات المحاسبة قبل إقفال السنة المالية.");
        }

        var retained = await accounts.GetByIdAsync(
            settings.RetainedEarningsAccountId.Value,
            cancellationToken)
            ?? throw new ConflictException(
                "retained_earnings_account_missing",
                "حساب الأرباح المحتجزة المحدد في الإعدادات غير موجود.");

        if (!retained.CanReceivePosting() ||
            retained.AccountClass != AccountClass.Equity ||
            retained.NormalBalance != NormalBalance.Credit)
        {
            throw new ConflictException(
                "retained_earnings_account_invalid",
                "حساب الأرباح المحتجزة يجب أن يكون حساب حقوق ملكية نشطًا، دائنًا، وقابلًا للترحيل.");
        }

        var baseCurrency = await currencies.GetByIdAsync(settings.BaseCurrencyId, cancellationToken)
            ?? throw new ConflictException(
                "base_currency_missing",
                "العملة الأساسية المحددة في إعدادات المحاسبة غير موجودة.");

        var sequence = await sequences.NextAsync(
            $"JournalEntry-{fiscalYear.EndDate.Year}",
            cancellationToken);

        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-{fiscalYear.EndDate.Year:0000}-{sequence:000000}",
            JournalType.Closing,
            fiscalYear.EndDate,
            fiscalYear.EndDate,
            lastPeriod.Id,
            $"Fiscal year closing / {fiscalYear.Code}",
            SourceModule,
            SourceDocumentType,
            fiscalYear.Id,
            JournalEntryStatus.Draft);

        journal.SetBaseCurrencySnapshot(
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces);

        var lineNumber = 1;
        foreach (var item in nominalRows.OrderBy(x => x.Row.AccountCode))
        {
            var debit = item.SignedPeriodBalance < 0m ? -item.SignedPeriodBalance : 0m;
            var credit = item.SignedPeriodBalance > 0m ? item.SignedPeriodBalance : 0m;

            journal.AddLine(JournalEntryLine.Create(
                Guid.NewGuid(),
                journal.Id,
                lineNumber++,
                item.Row.AccountId,
                debit,
                credit,
                $"إقفال {item.Row.AccountCode} - {item.Row.AccountNameAr}",
                null,
                null,
                null,
                null,
                null));
        }

        var currentDebit = journal.TotalDebit();
        var currentCredit = journal.TotalCredit();
        var difference = currentDebit - currentCredit;

        if (difference != 0m)
        {
            journal.AddLine(JournalEntryLine.Create(
                Guid.NewGuid(),
                journal.Id,
                lineNumber,
                retained.Id,
                difference < 0m ? -difference : 0m,
                difference > 0m ? difference : 0m,
                difference > 0m
                    ? $"صافي ربح السنة {fiscalYear.Code}"
                    : $"صافي خسارة السنة {fiscalYear.Code}",
                null,
                null,
                null,
                null,
                null));
        }

        journal.SetPendingApproval();
        journal.Approve(closedBy, closedAtUtc);
        journal.Post(closedBy, closedAtUtc);
        await journals.AddAsync(journal, cancellationToken);

        lastPeriod.Close(closedBy, closedAtUtc);
        fiscalPeriods.Update(lastPeriod);
        fiscalYear.Close(closedBy, closedAtUtc);
        return journal.Id;
    }
}
