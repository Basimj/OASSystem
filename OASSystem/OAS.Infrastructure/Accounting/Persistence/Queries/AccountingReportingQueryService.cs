using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class AccountingReportingQueryService(OasDbContext db) : IAccountingReportingQueryService
{
    public async Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureDateRange(fromDate, toDate);

        var accountQuery = db.Set<Account>().AsNoTracking().AsQueryable();
        if (accountId.HasValue)
            accountQuery = accountQuery.Where(x => x.Id == accountId.Value);

        var accounts = await accountQuery
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.AccountClass,
                x.NormalBalance
            })
            .ToListAsync(cancellationToken);

        if (accountId.HasValue && accounts.Count == 0)
            throw new NotFoundException(nameof(Account), accountId.Value);

        var selectedAccountIds = accounts.Select(x => x.Id).ToArray();
        if (selectedAccountIds.Length == 0)
        {
            return new GeneralLedgerReportDto(fromDate, toDate, accountId, [], 0m, 0m);
        }

        var movements = await (
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where selectedAccountIds.Contains(line.AccountId)
                  && journal.PostingDate <= toDate
                  && (journal.Status == JournalEntryStatus.Posted || journal.Status == JournalEntryStatus.Reversed)
            select new
            {
                line.AccountId,
                line.LineNumber,
                line.DebitAmount,
                line.CreditAmount,
                LineDescription = line.Description,
                JournalId = journal.Id,
                journal.JournalNumber,
                journal.PostingDate,
                journal.DocumentDate,
                JournalDescription = journal.Description,
                journal.SourceModule,
                journal.SourceDocumentType,
                journal.SourceDocumentId
            })
            .ToListAsync(cancellationToken);

        var accountReports = new List<GeneralLedgerAccountDto>(accounts.Count);
        decimal totalDebit = 0m;
        decimal totalCredit = 0m;

        foreach (var account in accounts)
        {
            var accountMovements = movements
                .Where(x => x.AccountId == account.Id)
                .OrderBy(x => x.PostingDate)
                .ThenBy(x => x.JournalNumber)
                .ThenBy(x => x.LineNumber)
                .ToList();

            var opening = accountMovements
                .Where(x => x.PostingDate < fromDate)
                .Sum(x => x.DebitAmount - x.CreditAmount);

            var period = accountMovements
                .Where(x => x.PostingDate >= fromDate && x.PostingDate <= toDate)
                .ToList();

            decimal running = opening;
            var lines = new List<GeneralLedgerLineDto>(period.Count);
            foreach (var movement in period)
            {
                running += movement.DebitAmount - movement.CreditAmount;
                lines.Add(new GeneralLedgerLineDto(
                    movement.JournalId,
                    movement.JournalNumber,
                    movement.PostingDate,
                    movement.DocumentDate,
                    string.IsNullOrWhiteSpace(movement.LineDescription)
                        ? movement.JournalDescription
                        : movement.LineDescription!,
                    movement.SourceModule,
                    movement.SourceDocumentType,
                    movement.SourceDocumentId,
                    movement.LineNumber,
                    movement.DebitAmount,
                    movement.CreditAmount,
                    running));
            }

            var debit = period.Sum(x => x.DebitAmount);
            var credit = period.Sum(x => x.CreditAmount);
            totalDebit += debit;
            totalCredit += credit;

            if (!accountId.HasValue && opening == 0m && debit == 0m && credit == 0m)
                continue;

            accountReports.Add(new GeneralLedgerAccountDto(
                account.Id,
                account.Code,
                account.NameAr,
                account.NameEn,
                (OAS.Contracts.Accounting.Enums.AccountClass)(byte)account.AccountClass,
                (OAS.Contracts.Accounting.Enums.NormalBalance)(byte)account.NormalBalance,
                opening,
                debit,
                credit,
                opening + debit - credit,
                lines));
        }

        return new GeneralLedgerReportDto(
            fromDate,
            toDate,
            accountId,
            accountReports,
            totalDebit,
            totalCredit);
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        EnsureDateRange(fromDate, toDate);

        var accounts = await db.Set<Account>()
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.AccountClass,
                x.NormalBalance
            })
            .ToListAsync(cancellationToken);

        var movements = await (
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where journal.PostingDate <= toDate
                  && (journal.Status == JournalEntryStatus.Posted || journal.Status == JournalEntryStatus.Reversed)
            group new { line, journal } by line.AccountId
            into g
            select new
            {
                AccountId = g.Key,
                OpeningDebit = g.Where(x => x.journal.PostingDate < fromDate).Sum(x => x.line.DebitAmount),
                OpeningCredit = g.Where(x => x.journal.PostingDate < fromDate).Sum(x => x.line.CreditAmount),
                PeriodDebit = g.Where(x => x.journal.PostingDate >= fromDate && x.journal.PostingDate <= toDate)
                    .Sum(x => x.line.DebitAmount),
                PeriodCredit = g.Where(x => x.journal.PostingDate >= fromDate && x.journal.PostingDate <= toDate)
                    .Sum(x => x.line.CreditAmount)
            })
            .ToListAsync(cancellationToken);

        var movementMap = movements.ToDictionary(x => x.AccountId);
        var rows = new List<TrialBalanceRowDto>();

        foreach (var account in accounts)
        {
            movementMap.TryGetValue(account.Id, out var movement);
            var openingSigned = (movement?.OpeningDebit ?? 0m) - (movement?.OpeningCredit ?? 0m);
            var periodDebit = movement?.PeriodDebit ?? 0m;
            var periodCredit = movement?.PeriodCredit ?? 0m;
            var closingSigned = openingSigned + periodDebit - periodCredit;

            if (openingSigned == 0m && periodDebit == 0m && periodCredit == 0m)
                continue;

            rows.Add(new TrialBalanceRowDto(
                account.Id,
                account.Code,
                account.NameAr,
                account.NameEn,
                (OAS.Contracts.Accounting.Enums.AccountClass)(byte)account.AccountClass,
                (OAS.Contracts.Accounting.Enums.NormalBalance)(byte)account.NormalBalance,
                openingSigned >= 0m ? openingSigned : 0m,
                openingSigned < 0m ? -openingSigned : 0m,
                periodDebit,
                periodCredit,
                closingSigned >= 0m ? closingSigned : 0m,
                closingSigned < 0m ? -closingSigned : 0m));
        }

        var openingDebitTotal = rows.Sum(x => x.OpeningDebit);
        var openingCreditTotal = rows.Sum(x => x.OpeningCredit);
        var periodDebitTotal = rows.Sum(x => x.PeriodDebit);
        var periodCreditTotal = rows.Sum(x => x.PeriodCredit);
        var closingDebitTotal = rows.Sum(x => x.ClosingDebit);
        var closingCreditTotal = rows.Sum(x => x.ClosingCredit);

        return new TrialBalanceReportDto(
            fromDate,
            toDate,
            rows,
            openingDebitTotal,
            openingCreditTotal,
            periodDebitTotal,
            periodCreditTotal,
            closingDebitTotal,
            closingCreditTotal,
            openingDebitTotal == openingCreditTotal &&
            periodDebitTotal == periodCreditTotal &&
            closingDebitTotal == closingCreditTotal);
    }

    public async Task<FiscalCloseReadinessDto> GetFiscalPeriodCloseReadinessAsync(
        Guid fiscalPeriodId,
        CancellationToken cancellationToken = default)
    {
        var period = await db.Set<FiscalPeriod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fiscalPeriodId, cancellationToken)
            ?? throw new NotFoundException(nameof(FiscalPeriod), fiscalPeriodId);

        var checks = await BuildCloseChecksAsync(
            period.StartDate,
            period.EndDate,
            fiscalPeriodId,
            requireAllPeriodsClosed: false,
            fiscalYearId: period.FiscalYearId,
            cancellationToken);

        return new FiscalCloseReadinessDto(
            period.Id,
            $"{period.PeriodNumber:00} - {period.Name}",
            period.StartDate,
            period.EndDate,
            checks.All(x => x.Passed),
            checks);
    }

    public async Task<FiscalCloseReadinessDto> GetFiscalYearCloseReadinessAsync(
        Guid fiscalYearId,
        CancellationToken cancellationToken = default)
    {
        var year = await db.Set<FiscalYear>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fiscalYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(FiscalYear), fiscalYearId);

        var checks = await BuildCloseChecksAsync(
            year.StartDate,
            year.EndDate,
            fiscalPeriodId: null,
            requireAllPeriodsClosed: true,
            fiscalYearId: year.Id,
            cancellationToken);

        return new FiscalCloseReadinessDto(
            year.Id,
            year.Code,
            year.StartDate,
            year.EndDate,
            checks.All(x => x.Passed),
            checks);
    }

    private async Task<IReadOnlyList<FiscalCloseCheckDto>> BuildCloseChecksAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? fiscalPeriodId,
        bool requireAllPeriodsClosed,
        Guid fiscalYearId,
        CancellationToken cancellationToken)
    {
        var checks = new List<FiscalCloseCheckDto>();

        var unpostedJournals = await db.Set<JournalEntry>()
            .AsNoTracking()
            .LongCountAsync(x =>
                (fiscalPeriodId.HasValue ? x.FiscalPeriodId == fiscalPeriodId.Value : x.PostingDate >= startDate && x.PostingDate <= endDate) &&
                x.Status != JournalEntryStatus.Posted &&
                x.Status != JournalEntryStatus.Reversed,
                cancellationToken);
        checks.Add(Check(
            "unposted_journals",
            "لا توجد قيود يومية غير مرحلة ضمن النطاق المالي.",
            unpostedJournals));

        var openReceiptVouchers = await db.Set<ReceiptVoucher>()
            .AsNoTracking()
            .LongCountAsync(x => x.VoucherDate >= startDate && x.VoucherDate <= endDate &&
                                 x.Status != ReceiptVoucherStatus.Posted &&
                                 x.Status != ReceiptVoucherStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_receipt_vouchers",
            "لا توجد سندات قبض غير مرحلة ضمن النطاق المالي.",
            openReceiptVouchers));

        var openPaymentVouchers = await db.Set<PaymentVoucher>()
            .AsNoTracking()
            .LongCountAsync(x => x.VoucherDate >= startDate && x.VoucherDate <= endDate &&
                                 x.Status != PaymentVoucherStatus.Posted &&
                                 x.Status != PaymentVoucherStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_payment_vouchers",
            "لا توجد سندات صرف غير مرحلة ضمن النطاق المالي.",
            openPaymentVouchers));

        var openExpenses = await db.Set<Expense>()
            .AsNoTracking()
            .LongCountAsync(x => x.ExpenseDate >= startDate && x.ExpenseDate <= endDate &&
                                 x.Status != ExpenseStatus.Posted &&
                                 x.Status != ExpenseStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_expenses",
            "لا توجد مصروفات غير مرحلة ضمن النطاق المالي.",
            openExpenses));

        var openSalesInvoices = await db.Set<SalesInvoice>()
            .AsNoTracking()
            .LongCountAsync(x => x.PostingDate >= startDate && x.PostingDate <= endDate &&
                                 x.Status != SalesInvoiceStatus.Posted &&
                                 x.Status != SalesInvoiceStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_sales_invoices",
            "لا توجد فواتير مبيعات غير مرحلة ضمن النطاق المالي.",
            openSalesInvoices));

        var openSalesReturns = await db.Set<SalesReturn>()
            .AsNoTracking()
            .LongCountAsync(x => x.PostingDate >= startDate && x.PostingDate <= endDate &&
                                 x.Status != SalesReturnStatus.Posted &&
                                 x.Status != SalesReturnStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_sales_returns",
            "لا توجد مرتجعات مبيعات غير مرحلة ضمن النطاق المالي.",
            openSalesReturns));

        var openPurchaseReceipts = await db.Set<PurchaseReceipt>()
            .AsNoTracking()
            .LongCountAsync(x => x.PostingDate >= startDate && x.PostingDate <= endDate &&
                                 x.Status != PurchaseReceiptStatus.Posted &&
                                 x.Status != PurchaseReceiptStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_purchase_receipts",
            "لا توجد استلامات مشتريات غير مرحلة ضمن النطاق المالي.",
            openPurchaseReceipts));

        var openPurchaseInvoices = await db.Set<PurchaseInvoice>()
            .AsNoTracking()
            .LongCountAsync(x => x.PostingDate >= startDate && x.PostingDate <= endDate &&
                                 x.Status != PurchaseInvoiceStatus.Posted &&
                                 x.Status != PurchaseInvoiceStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_purchase_invoices",
            "لا توجد فواتير مشتريات غير مرحلة ضمن النطاق المالي.",
            openPurchaseInvoices));

        var openPurchaseReturns = await db.Set<PurchaseReturn>()
            .AsNoTracking()
            .LongCountAsync(x => x.PostingDate >= startDate && x.PostingDate <= endDate &&
                                 x.Status != PurchaseReturnStatus.Posted &&
                                 x.Status != PurchaseReturnStatus.Cancelled,
                cancellationToken);
        checks.Add(Check(
            "open_purchase_returns",
            "لا توجد مرتجعات مشتريات غير مرحلة ضمن النطاق المالي.",
            openPurchaseReturns));

        if (requireAllPeriodsClosed)
        {
            var yearPeriods = await db.Set<FiscalPeriod>()
                .AsNoTracking()
                .Where(x => x.FiscalYearId == fiscalYearId)
                .OrderBy(x => x.EndDate)
                .ThenBy(x => x.PeriodNumber)
                .Select(x => new
                {
                    x.Id,
                    x.PeriodNumber,
                    x.Status,
                    x.AccountingLocked
                })
                .ToListAsync(cancellationToken);

            var hasPeriods = yearPeriods.Count > 0;
            checks.Add(new FiscalCloseCheckDto(
                "fiscal_year_has_periods",
                "تحتوي السنة المالية على فترة مالية واحدة على الأقل.",
                hasPeriods,
                hasPeriods ? 0 : 1));

            if (hasPeriods)
            {
                var closingPeriod = yearPeriods[^1];
                var earlierNotClosed = yearPeriods
                    .Take(yearPeriods.Count - 1)
                    .LongCount(x => x.Status != FiscalPeriodStatus.Closed);

                checks.Add(Check(
                    "prior_periods_closed",
                    "جميع الفترات السابقة لفترة الإقفال السنوي مغلقة.",
                    earlierNotClosed));

                var closingPeriodReady =
                    closingPeriod.Status == FiscalPeriodStatus.SoftClosed &&
                    !closingPeriod.AccountingLocked;

                checks.Add(new FiscalCloseCheckDto(
                    "closing_period_soft_closed",
                    "آخر فترة مالية يجب أن تكون Soft Closed وغير مقفلة محاسبيًا لاستقبال قيد الإقفال السنوي ثم إغلاقها مع السنة.",
                    closingPeriodReady,
                    closingPeriodReady ? 0 : 1));
            }

            var settings = await db.Set<AccountingSettings>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == AccountingSettings.SingletonId, cancellationToken);

            var retainedConfigured = settings?.RetainedEarningsAccountId is Guid retainedId &&
                await db.Set<Account>().AsNoTracking().AnyAsync(x =>
                    x.Id == retainedId &&
                    x.IsActive &&
                    x.IsPostingAccount &&
                    x.AccountType != AccountType.Header &&
                    x.AccountClass == AccountClass.Equity &&
                    x.NormalBalance == NormalBalance.Credit,
                    cancellationToken);

            checks.Add(new FiscalCloseCheckDto(
                "retained_earnings_configured",
                "تم إعداد حساب أرباح محتجزة صالح للإقفال السنوي.",
                retainedConfigured,
                retainedConfigured ? 0 : 1));
        }

        var trialBalance = await GetTrialBalanceAsync(startDate, endDate, cancellationToken);
        checks.Add(new FiscalCloseCheckDto(
            "trial_balance_balanced",
            "ميزان المراجعة متوازن ضمن النطاق المالي.",
            trialBalance.IsBalanced,
            trialBalance.IsBalanced ? 0 : 1));

        return checks;
    }

    private static FiscalCloseCheckDto Check(string code, string description, long blockingCount) =>
        new(code, description, blockingCount == 0, blockingCount);

    private static void EnsureDateRange(DateOnly fromDate, DateOnly toDate)
    {
        if (toDate < fromDate)
            throw new ArgumentException("To date cannot be before from date.");
    }
}
