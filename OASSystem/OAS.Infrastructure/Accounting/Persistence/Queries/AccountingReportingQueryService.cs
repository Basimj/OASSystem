using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Reports.Services;
using OAS.Application.Accounting.Journals.Mapping;
using OAS.Contracts.Accounting.Journals;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class AccountingReportingQueryService(OasDbContext db, TimeProvider timeProvider) : IAccountingReportingQueryService
{
    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(BalanceSheetFilterDto filter, CancellationToken cancellationToken = default)
    {
        var asOfDate = filter.AsOfDate;
        if (filter.FiscalYearId is Guid yearId)
        {
            var year = await db.Set<FiscalYear>().AsNoTracking().FirstOrDefaultAsync(y => y.Id == yearId, cancellationToken)
                ?? throw new NotFoundException(nameof(FiscalYear), yearId);
            if (asOfDate < year.StartDate || asOfDate > year.EndDate)
                throw new ConflictException("report_date_outside_year", "تاريخ الميزانية خارج السنة المالية المختارة.");
        }
        // A balance sheet is cumulative. Do not truncate to the selected year's start,
        // and do not exclude closing entries or posted reversals.
        var accounts = await db.Set<Account>().AsNoTracking().OrderBy(a => a.Code).ToListAsync(cancellationToken);
        var directory = accounts.ToDictionary(a => a.Id);
        var balances = await (from line in db.Set<JournalEntryLine>().AsNoTracking()
                              join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
                              where journal.Status == JournalEntryStatus.Posted && journal.PostingDate <= asOfDate
                              group line by line.AccountId into g
                              select new { AccountId = g.Key, Balance = g.Sum(l => l.DebitAmount - l.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Balance, cancellationToken);
        var rows = accounts.Select(a => new BalanceSheetAccountDto(a.Id, a.Code, a.NameAr, a.ParentAccountId, a.Level,
            (OAS.Contracts.Accounting.Enums.AccountClass)a.AccountClass,
            (OAS.Contracts.Accounting.Enums.FinancialStatementCategory?)FinancialStatementCategories.Resolve(a.AccountClass, a.FinancialStatementCategory, a.ParentAccountId, directory),
            balances.GetValueOrDefault(a.Id))).ToArray();
        return new BalanceSheetReportDto(asOfDate, rows, BalanceSheetTotalsDto.Calculate(rows));
    }

    public async Task<IncomeStatementReportDto> GetIncomeStatementAsync(IncomeStatementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = await ResolveRangeAsync(filter.FromDate, filter.ToDate, null, filter.FiscalYearId, filter.FiscalPeriodId, cancellationToken);
        var accounts = await db.Set<Account>().AsNoTracking().ToListAsync(cancellationToken);
        var directory = accounts.ToDictionary(a => a.Id);
        var nominal = accounts.Where(a => a.AccountClass is AccountClass.Revenue or AccountClass.Expense)
            .Select(a => new IncomeStatementAccountDto(a.Id, a.Code, a.NameAr, a.ParentAccountId, a.Level,
                (OAS.Contracts.Accounting.Enums.AccountClass)a.AccountClass,
                (OAS.Contracts.Accounting.Enums.FinancialStatementCategory?)FinancialStatementCategories.Resolve(a.AccountClass, a.FinancialStatementCategory, a.ParentAccountId, directory))).ToArray();
        // Exclude annual closing entries and every linked reversal, including legacy chains.
        var links = await db.Set<JournalEntry>().AsNoTracking()
            .Where(j => j.JournalType == JournalType.Closing || j.SourceDocumentType == "FiscalYearClosing" || j.ReversedJournalId.HasValue)
            .Select(j => new { j.Id, j.ReversedJournalId, j.JournalType, j.SourceDocumentType }).ToListAsync(cancellationToken);
        var byId = links.ToDictionary(j => j.Id);
        var excluded = new HashSet<Guid>();
        foreach (var close in links.Where(j => j.JournalType == JournalType.Closing || j.SourceDocumentType == "FiscalYearClosing"))
        {
            Guid? id = close.Id;
            while (id is Guid key && excluded.Add(key))
                id = byId.TryGetValue(key, out var link) ? link.ReversedJournalId : null;
        }
        var excludedIds = excluded.ToArray();
        var query = from line in db.Set<JournalEntryLine>().AsNoTracking()
                    join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
                    join account in db.Set<Account>().AsNoTracking() on line.AccountId equals account.Id
                    where journal.Status == JournalEntryStatus.Posted && journal.PostingDate >= fromDate && journal.PostingDate <= toDate
                        && (account.AccountClass == AccountClass.Revenue || account.AccountClass == AccountClass.Expense)
                        && journal.JournalType != JournalType.Closing && !excludedIds.Contains(journal.Id)
                        && (!filter.CostCenterId.HasValue || line.CostCenterId == filter.CostCenterId)
                    group line by new { line.AccountId, journal.PostingDate.Year, journal.PostingDate.Month } into g
                    select new IncomeStatementMovementDto(g.Key.AccountId, g.Key.Year, g.Key.Month, g.Sum(l => l.CreditAmount - l.DebitAmount));
        var movements = await query.ToListAsync(cancellationToken);
        return new IncomeStatementReportDto(fromDate, toDate, nominal, movements);
    }

    public async Task<AccountingDashboardDto> GetAccountingDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeProvider.LocalTimeZone);
        var today = DateOnly.FromDateTime(localNow.DateTime);

        var journalStatusRows = await db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.Status != JournalEntryStatus.Posted && x.Status != JournalEntryStatus.Reversed)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.LongCount() })
            .ToListAsync(cancellationToken);
        var draftCount = journalStatusRows.Where(x => x.Status == JournalEntryStatus.Draft).Sum(x => x.Count);
        var pendingCount = journalStatusRows.Where(x => x.Status == JournalEntryStatus.PendingApproval).Sum(x => x.Count);
        var approvedCount = journalStatusRows.Where(x => x.Status == JournalEntryStatus.Approved).Sum(x => x.Count);
        var unpostedJournalCount = draftCount + pendingCount + approvedCount;

        var currentPeriod = await (
            from period in db.Set<FiscalPeriod>().AsNoTracking()
            join year in db.Set<FiscalYear>().AsNoTracking()
                on period.FiscalYearId equals year.Id
            where period.StartDate <= today && period.EndDate >= today
            orderby period.Status, period.PeriodNumber
            select new
            {
                period.Id,
                period.Name,
                period.PeriodNumber,
                period.StartDate,
                period.EndDate,
                period.Status,
                FiscalYearCode = year.Code
            })
            .FirstOrDefaultAsync(cancellationToken);

        var openCashShiftCount = await db.Set<CashShift>()
            .AsNoTracking()
            .LongCountAsync(x =>
                x.Status == CashShiftStatus.Open || x.Status == CashShiftStatus.Closing,
                cancellationToken);

        var oldestOpenShift = await db.Set<CashShift>()
            .AsNoTracking()
            .Where(x => x.Status == CashShiftStatus.Open || x.Status == CashShiftStatus.Closing)
            .OrderBy(x => x.OpenedAtUtc)
            .Select(x => (DateTime?)x.OpenedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var activeBankAccountCount = await db.Set<BankAccount>()
            .AsNoTracking()
            .LongCountAsync(x => x.IsActive, cancellationToken);

        var attention = new List<AccountingDashboardAttentionDto>(5);
        if (pendingCount > 0)
            attention.Add(new AccountingDashboardAttentionDto("pending-approval-journals", "قيود تنتظر الاعتماد", "danger", $"{pendingCount:N0}"));
        if (approvedCount > 0)
            attention.Add(new AccountingDashboardAttentionDto("approved-journals", "قيود جاهزة للترحيل", "warning", $"{approvedCount:N0}"));
        if (draftCount > 0)
            attention.Add(new AccountingDashboardAttentionDto("draft-journals", "مسودات قيود غير مكتملة", "info", $"{draftCount:N0}"));

        if (currentPeriod is null)
        {
            attention.Add(new AccountingDashboardAttentionDto(
                "missing-current-period",
                "لا توجد فترة مالية تغطي تاريخ اليوم",
                "danger",
                "السنوات والفترات المالية"));
        }
        else
        {
            var daysRemaining = currentPeriod.EndDate.DayNumber - today.DayNumber;
            if (currentPeriod.Status != FiscalPeriodStatus.Closed && daysRemaining <= 7)
            {
                var periodCaption = string.IsNullOrWhiteSpace(currentPeriod.FiscalYearCode)
                    ? currentPeriod.Name
                    : $"{currentPeriod.Name} {currentPeriod.FiscalYearCode}";
                var dayText = daysRemaining <= 0 ? "اليوم" : $"خلال {daysRemaining} يوم";
                attention.Add(new AccountingDashboardAttentionDto(
                    "period-closing-soon",
                    $"الفترة المالية {periodCaption} تنتهي {dayText}",
                    "warning",
                    "مراجعة جاهزية الإقفال"));
            }
        }

        if (openCashShiftCount > 0)
        {
            var meta = "ورديات الصندوق";
            if (oldestOpenShift.HasValue)
            {
                var opened = DateTime.SpecifyKind(oldestOpenShift.Value, DateTimeKind.Utc);
                var age = nowUtc - new DateTimeOffset(opened);
                if (age.TotalHours >= 24)
                    meta = $"أقدم وردية مفتوحة منذ {Math.Max(1, (int)Math.Floor(age.TotalDays))} يوم";
            }

            attention.Add(new AccountingDashboardAttentionDto(
                "open-cash-shifts",
                $"{openCashShiftCount:N0} ورديات صندوق مفتوحة",
                openCashShiftCount > 1 ? "warning" : "info",
                meta));
        }

        // The dashboard never inserts decorative/mock money figures. All figures
        // are computed from posted accounting data in the configured base currency.
        var currency = await (from settings in db.Set<AccountingSettings>().AsNoTracking()
                              join c in db.Set<Currency>().AsNoTracking()
                                  on settings.BaseCurrencyId equals c.Id
                              where settings.Id == AccountingSettings.SingletonId
                              select new { c.Code, c.DecimalPlaces })
            .SingleOrDefaultAsync(cancellationToken);

        AccountingDashboardFinancialDto? financial = null;
        if (currency is not null)
        {
            var postedLines = from line in db.Set<JournalEntryLine>().AsNoTracking()
                              join journal in db.Set<JournalEntry>().AsNoTracking()
                                  on line.JournalEntryId equals journal.Id
                              where journal.Status == JournalEntryStatus.Posted && journal.PostingDate <= today
                              select new { line.AccountId, line.DebitAmount, line.CreditAmount };

            var bankAccounts = db.Set<BankAccount>().AsNoTracking()
                .Where(x => x.IsActive).Select(x => x.AccountId);
            var bankBalance = await postedLines
                .Where(x => bankAccounts.Contains(x.AccountId))
                .SumAsync(x => x.DebitAmount - x.CreditAmount, cancellationToken);

            // Group by GL account first: the same linked account must never be
            // counted twice when multiple parties point to it.
            var customerAccounts = db.Set<Customer>().AsNoTracking().Select(x => x.AccountId);
            var customerBalances = await postedLines
                .Where(x => customerAccounts.Contains(x.AccountId))
                .GroupBy(x => x.AccountId)
                .Select(g => g.Sum(x => x.DebitAmount - x.CreditAmount))
                .ToListAsync(cancellationToken);
            var receivables = customerBalances.Sum(x => Math.Max(0m, x));

            var supplierAccounts = db.Set<Supplier>().AsNoTracking().Select(x => x.AccountId);
            var supplierBalances = await postedLines
                .Where(x => supplierAccounts.Contains(x.AccountId))
                .GroupBy(x => x.AccountId)
                .Select(g => g.Sum(x => x.CreditAmount - x.DebitAmount))
                .ToListAsync(cancellationToken);
            var payables = supplierBalances.Sum(x => Math.Max(0m, x));

            decimal postedExpenses = 0m;
            if (currentPeriod is not null)
            {
                postedExpenses = await db.Set<Expense>().AsNoTracking()
                    .Where(x => x.Status == ExpenseStatus.Posted &&
                        x.ExpenseDate >= currentPeriod.StartDate && x.ExpenseDate <= today)
                    .SumAsync(x => x.BaseAmount ?? x.Amount, cancellationToken);
            }

            financial = new AccountingDashboardFinancialDto(
                currency.Code, currency.DecimalPlaces, today, currentPeriod?.StartDate,
                bankBalance, receivables, payables, postedExpenses);
        }

        var journalActivities = await db.Set<JournalEntry>()
            .AsNoTracking()
            .OrderByDescending(x => x.LastModifiedAtUtc ?? x.CreatedAtUtc)
            .Take(4)
            .Select(x => new ActivityCandidate(
                x.Id,
                "journal",
                x.JournalNumber,
                (byte)x.Status,
                x.Description,
                x.LastModifiedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var receiptActivities = await db.Set<ReceiptVoucher>()
            .AsNoTracking()
            .OrderByDescending(x => x.LastModifiedAtUtc ?? x.CreatedAtUtc)
            .Take(4)
            .Select(x => new ActivityCandidate(
                x.Id,
                "receipt",
                x.VoucherNumber,
                (byte)x.Status,
                x.Description,
                x.LastModifiedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var paymentActivities = await db.Set<PaymentVoucher>()
            .AsNoTracking()
            .OrderByDescending(x => x.LastModifiedAtUtc ?? x.CreatedAtUtc)
            .Take(4)
            .Select(x => new ActivityCandidate(
                x.Id,
                "payment",
                x.VoucherNumber,
                (byte)x.Status,
                x.Description,
                x.LastModifiedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var activities = journalActivities
            .Concat(receiptActivities)
            .Concat(paymentActivities)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(5)
            .Select(x => new AccountingDashboardActivityDto(
                x.EntityId,
                x.ActivityType,
                x.Reference,
                x.Status,
                x.Description,
                x.OccurredAtUtc))
            .ToArray();

        var currentPeriodName = currentPeriod is null
            ? "لا توجد فترة حالية"
            : string.IsNullOrWhiteSpace(currentPeriod.FiscalYearCode)
                ? currentPeriod.Name
                : $"{currentPeriod.Name} {currentPeriod.FiscalYearCode}";

        return new AccountingDashboardDto(
            unpostedJournalCount,
            currentPeriod?.Id,
            currentPeriodName,
            currentPeriod?.EndDate,
            openCashShiftCount,
            activeBankAccountCount,
            attention,
            activities,
            new AccountingDashboardJournalCountsDto(draftCount, pendingCount, approvedCount),
            financial,
            currentPeriod?.StartDate,
            currentPeriod?.Status switch
            {
                FiscalPeriodStatus.Open => "مفتوحة",
                FiscalPeriodStatus.SoftClosed => "إقفال مبدئي",
                FiscalPeriodStatus.Closed => "مغلقة",
                _ => currentPeriod is null ? "غير محددة" : "غير معروفة"
            },
            nowUtc);
    }

    public Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default) =>
        GetGeneralLedgerAsync(
            new GeneralLedgerFilterDto(fromDate, toDate, AccountId: accountId),
            cancellationToken);

    public async Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(
        GeneralLedgerFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = await ResolveRangeAsync(
            filter.FromDate, filter.ToDate, null, filter.FiscalYearId, filter.FiscalPeriodId, cancellationToken);

        var allAccounts = await db.Set<Account>()
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new AccountReportMeta(
                x.Id, x.Code, x.NameAr, x.NameEn, x.ParentAccountId, x.Level,
                x.AccountClass, x.AccountType, x.NormalBalance, x.IsPostingAccount))
            .ToListAsync(cancellationToken);

        IReadOnlySet<Guid>? selectedIds = null;
        if (filter.AccountId.HasValue)
        {
            if (allAccounts.All(x => x.Id != filter.AccountId.Value))
                throw new NotFoundException(nameof(Account), filter.AccountId.Value);

            selectedIds = filter.IncludeAccountSubtree
                ? ResolveAccountSubtree(filter.AccountId.Value, allAccounts)
                : new HashSet<Guid> { filter.AccountId.Value };
        }

        var movementFrom = filter.IncludeOpeningBalance ? (DateOnly?)null : fromDate;
        var query =
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where journal.Status == JournalEntryStatus.Posted
                  && journal.PostingDate <= toDate
                  && (!movementFrom.HasValue || journal.PostingDate >= movementFrom.Value)
            select new
            {
                line.AccountId,
                line.LineNumber,
                line.DebitAmount,
                line.CreditAmount,
                line.Description,
                line.CustomerId,
                line.SupplierId,
                line.TransactionCurrencyCodeSnapshot,
                line.TransactionDebitAmount,
                line.TransactionCreditAmount,
                line.ExchangeRate,
                JournalId = journal.Id,
                journal.JournalNumber,
                journal.JournalType,
                journal.PostingDate,
                journal.DocumentDate,
                JournalDescription = journal.Description,
                journal.SourceModule,
                journal.SourceDocumentType,
                journal.SourceDocumentId
            };

        if (selectedIds is not null)
        {
            var ids = selectedIds.ToArray();
            query = query.Where(x => ids.Contains(x.AccountId));
        }

        if (filter.JournalType.HasValue)
        {
            var journalType = (JournalType)(byte)filter.JournalType.Value;
            var originalLinks = db.Set<JournalEntry>().AsNoTracking().Where(j => j.ReversedJournalId.HasValue).Select(j => j.ReversedJournalId!.Value);
            if (journalType == JournalType.Reversal)
                query = query.Where(x => x.JournalType == JournalType.Reversal || originalLinks.Contains(x.JournalId) || x.SourceDocumentType == "JournalEntryReversal" || x.SourceDocumentType == "CustomerSettlementFxReversal" || x.SourceDocumentType == "SupplierSettlementFxReversal");
            else
                query = query.Where(x => x.JournalType == journalType && !originalLinks.Contains(x.JournalId) && (x.SourceDocumentType == null || (x.SourceDocumentType != "JournalEntryReversal" && x.SourceDocumentType != "CustomerSettlementFxReversal" && x.SourceDocumentType != "SupplierSettlementFxReversal")));
        }

        if (!string.IsNullOrWhiteSpace(filter.SourceModule))
        {
            var source = filter.SourceModule.Trim();
            query = query.Where(x => x.SourceModule == source);
        }

        if (filter.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == filter.CustomerId.Value);
        if (filter.SupplierId.HasValue)
            query = query.Where(x => x.SupplierId == filter.SupplierId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Reference))
        {
            var reference = filter.Reference.Trim();
            query = query.Where(x =>
                x.JournalNumber.Contains(reference) ||
                x.JournalDescription.Contains(reference) ||
                (x.Description != null && x.Description.Contains(reference)) ||
                (x.SourceModule != null && x.SourceModule.Contains(reference)) ||
                (x.SourceDocumentType != null && x.SourceDocumentType.Contains(reference)));
        }

        var movements = await query.ToListAsync(cancellationToken);
        var reversalInfo = await ResolveJournalReversalsAsync(movements.Where(x => x.PostingDate >= fromDate).Select(x => x.JournalId), cancellationToken);
        // Resolve document codes in batches, never expose source entity GUIDs in customer-facing reports.
        var sourceDocuments = await ResolveLedgerDocumentsAsync(
            movements.Where(x => x.PostingDate >= fromDate)
                .Select(x => (x.SourceDocumentType, x.SourceDocumentId)), cancellationToken);
        var accounts = allAccounts
            .Where(x => selectedIds is null || selectedIds.Contains(x.Id))
            .ToList();

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

            var opening = filter.IncludeOpeningBalance
                ? accountMovements
                    .Where(x => x.PostingDate < fromDate)
                    .Sum(x => AccountingReportMath.NaturalDelta(account.NormalBalance, x.DebitAmount, x.CreditAmount))
                : 0m;

            var period = accountMovements
                .Where(x => x.PostingDate >= fromDate && x.PostingDate <= toDate)
                .ToList();

            decimal running = opening;
            var lines = new List<GeneralLedgerLineDto>(period.Count);
            foreach (var movement in period)
            {
                running += AccountingReportMath.NaturalDelta(account.NormalBalance, movement.DebitAmount, movement.CreditAmount);
                lines.Add(new GeneralLedgerLineDto(
                    movement.JournalId,
                    movement.JournalNumber,
                    movement.PostingDate,
                    movement.DocumentDate,
                    ResolveLedgerDescription(
                        movement.Description, movement.JournalDescription,
                        movement.SourceDocumentType, movement.SourceDocumentId,
                        sourceDocuments),
                    movement.SourceModule,
                    movement.SourceDocumentType,
                    movement.SourceDocumentId,
                    movement.LineNumber,
                    movement.DebitAmount,
                    movement.CreditAmount,
                    running,
                    reversalInfo.TryGetValue(movement.JournalId, out var info) && info.IsReversal ? OAS.Contracts.Accounting.Enums.JournalType.Reversal : (OAS.Contracts.Accounting.Enums.JournalType)(byte)movement.JournalType,
                    movement.CustomerId,
                    movement.SupplierId,
                    movement.TransactionCurrencyCodeSnapshot,
                    movement.TransactionDebitAmount,
                    movement.TransactionCreditAmount,
                    movement.ExchangeRate,
                    GetDocumentReference(movement.SourceDocumentType, movement.SourceDocumentId,
                        sourceDocuments),
                    StatementNarrativeFormatter.Resolve(
                        GetDocumentInfo(movement.SourceDocumentType, movement.SourceDocumentId, sourceDocuments)?.Narrative,
                        GetDocumentReference(movement.SourceDocumentType, movement.SourceDocumentId, sourceDocuments),
                        movement.SourceDocumentType, movement.Description, movement.JournalDescription,
                        movement.JournalType == JournalType.Manual),
                    info));
            }

            var debit = period.Sum(x => x.DebitAmount);
            var credit = period.Sum(x => x.CreditAmount);
            totalDebit += debit;
            totalCredit += credit;

            if (!filter.AccountId.HasValue && opening == 0m && debit == 0m && credit == 0m)
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
                running,
                lines,
                account.Level,
                account.ParentAccountId,
                (OAS.Contracts.Accounting.Enums.AccountType)(byte)account.AccountType));
        }

        return new GeneralLedgerReportDto(
            fromDate, toDate, filter.AccountId, accountReports, totalDebit, totalCredit);
    }

    public Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default) =>
        GetTrialBalanceAsync(new TrialBalanceFilterDto(fromDate, toDate), cancellationToken);

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        TrialBalanceFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = await ResolveRangeAsync(
            filter.FromDate, filter.ToDate, filter.AsOfDate, filter.FiscalYearId, filter.FiscalPeriodId, cancellationToken);

        var accounts = await db.Set<Account>()
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new AccountReportMeta(
                x.Id, x.Code, x.NameAr, x.NameEn, x.ParentAccountId, x.Level,
                x.AccountClass, x.AccountType, x.NormalBalance, x.IsPostingAccount))
            .ToListAsync(cancellationToken);

        var movements = await (
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where journal.PostingDate <= toDate && journal.Status == JournalEntryStatus.Posted
            group new { line, journal } by line.AccountId
            into g
            select new AccountMovement(
                g.Key,
                g.Where(x => x.journal.PostingDate < fromDate).Sum(x => x.line.DebitAmount),
                g.Where(x => x.journal.PostingDate < fromDate).Sum(x => x.line.CreditAmount),
                g.Where(x => x.journal.PostingDate >= fromDate && x.journal.PostingDate <= toDate).Sum(x => x.line.DebitAmount),
                g.Where(x => x.journal.PostingDate >= fromDate && x.journal.PostingDate <= toDate).Sum(x => x.line.CreditAmount)))
            .ToListAsync(cancellationToken);

        var direct = movements.ToDictionary(x => x.AccountId);
        var children = accounts
            .Where(x => x.ParentAccountId.HasValue)
            .GroupBy(x => x.ParentAccountId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(a => a.Id).ToArray());

        var rows = new List<TrialBalanceRowDto>();
        foreach (var account in accounts)
        {
            if (filter.PostingAccountsOnly && !account.IsPostingAccount)
                continue;
            if (filter.AccountLevel.HasValue && account.Level > filter.AccountLevel.Value)
                continue;

            AccountMovement movement;
            var aggregate = filter.GroupedTree && children.ContainsKey(account.Id);
            if (aggregate)
            {
                var ids = ResolveAccountSubtree(account.Id, accounts);
                movement = SumMovements(ids, direct);
            }
            else
            {
                movement = direct.TryGetValue(account.Id, out var value)
                    ? value
                    : new AccountMovement(account.Id, 0m, 0m, 0m, 0m);
            }

            var openingSigned = movement.OpeningDebit - movement.OpeningCredit;
            var closingSigned = openingSigned + movement.PeriodDebit - movement.PeriodCredit;

            if (!filter.IncludeZeroBalance && openingSigned == 0m && movement.PeriodDebit == 0m && movement.PeriodCredit == 0m)
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
                movement.PeriodDebit,
                movement.PeriodCredit,
                closingSigned >= 0m ? closingSigned : 0m,
                closingSigned < 0m ? -closingSigned : 0m,
                account.Level,
                account.ParentAccountId,
                (OAS.Contracts.Accounting.Enums.AccountType)(byte)account.AccountType,
                aggregate,
                movement.OpeningDebit,
                movement.OpeningCredit));
        }

        // Totals are calculated from direct account movements, never from header rollups,
        // so grouped-tree display cannot double-count the trial balance.
        var directMovements = accounts.Select(account => direct.TryGetValue(account.Id, out var m)
            ? m
            : new AccountMovement(account.Id, 0m, 0m, 0m, 0m)).ToList();
        var openingNets = directMovements
            .Select(x => x.OpeningDebit - x.OpeningCredit)
            .ToArray();
        var openingDebitTotal = openingNets.Where(x => x >= 0m).Sum();
        var openingCreditTotal = -openingNets.Where(x => x < 0m).Sum();
        var periodDebitTotal = directMovements.Sum(x => x.PeriodDebit);
        var periodCreditTotal = directMovements.Sum(x => x.PeriodCredit);
        var closingNets = directMovements.Select(x =>
            (x.OpeningDebit - x.OpeningCredit) + x.PeriodDebit - x.PeriodCredit).ToArray();
        var closingDebitTotal = closingNets.Where(x => x >= 0m).Sum();
        var closingCreditTotal = -closingNets.Where(x => x < 0m).Sum();

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
            AccountingReportMath.IsBalanced(openingDebitTotal, openingCreditTotal) &&
            AccountingReportMath.IsBalanced(periodDebitTotal, periodCreditTotal) &&
            AccountingReportMath.IsBalanced(closingDebitTotal, closingCreditTotal));
    }

    public async Task<JournalReportDto> GetJournalReportAsync(
        JournalReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        if (filter.PageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(filter.PageNumber));
        if (filter.PageSize is < 1 or > 500)
            throw new ArgumentOutOfRangeException(nameof(filter.PageSize));

        var (fromDate, toDate) = await ResolveRangeAsync(
            filter.FromDate, filter.ToDate, null, filter.FiscalYearId, filter.FiscalPeriodId, cancellationToken);

        var query =
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
            join account in db.Set<Account>().AsNoTracking() on line.AccountId equals account.Id
            where journal.PostingDate >= fromDate && journal.PostingDate <= toDate
            select new
            {
                Journal = journal,
                Line = line,
                AccountCode = account.Code,
                AccountNameAr = account.NameAr
            };

        if (filter.JournalType.HasValue)
        {
            var value = (JournalType)(byte)filter.JournalType.Value;
            var originalLinks = db.Set<JournalEntry>().AsNoTracking().Where(j => j.ReversedJournalId.HasValue).Select(j => j.ReversedJournalId!.Value);
            if (value == JournalType.Reversal)
                query = query.Where(x => x.Journal.JournalType == JournalType.Reversal || originalLinks.Contains(x.Journal.Id) || x.Journal.SourceDocumentType == "JournalEntryReversal" || x.Journal.SourceDocumentType == "CustomerSettlementFxReversal" || x.Journal.SourceDocumentType == "SupplierSettlementFxReversal");
            else
                query = query.Where(x => x.Journal.JournalType == value && !originalLinks.Contains(x.Journal.Id) && (x.Journal.SourceDocumentType == null || (x.Journal.SourceDocumentType != "JournalEntryReversal" && x.Journal.SourceDocumentType != "CustomerSettlementFxReversal" && x.Journal.SourceDocumentType != "SupplierSettlementFxReversal")));
        }
        if (filter.Status.HasValue)
        {
            var value = (JournalEntryStatus)(byte)filter.Status.Value;
            query = query.Where(x => x.Journal.Status == value);
        }
        if (!string.IsNullOrWhiteSpace(filter.SourceModule))
        {
            var value = filter.SourceModule.Trim();
            query = query.Where(x => x.Journal.SourceModule == value);
        }
        if (filter.AccountId is Guid accountId)
        {
            if (filter.IncludeCompleteEntries)
                query = query.Where(x => db.Set<JournalEntryLine>().Any(l => l.JournalEntryId == x.Journal.Id && l.AccountId == accountId));
            else
                query = query.Where(x => x.Line.AccountId == accountId);
        }
        if (filter.UserId.HasValue)
        {
            var userText = filter.UserId.Value.ToString();
            query = query.Where(x => x.Journal.PostedBy == filter.UserId.Value || x.Journal.CreatedBy == userText);
        }
        if (!string.IsNullOrWhiteSpace(filter.JournalNumber))
        {
            var value = filter.JournalNumber.Trim();
            query = query.Where(x => x.Journal.JournalNumber.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(filter.Reference))
        {
            var value = filter.Reference.Trim();
            query = query.Where(x =>
                x.Journal.JournalNumber.Contains(value) ||
                x.Journal.Description.Contains(value) ||
                (x.Line.Description != null && x.Line.Description.Contains(value)) ||
                (x.Journal.SourceModule != null && x.Journal.SourceModule.Contains(value)) ||
                (x.Journal.SourceDocumentType != null && x.Journal.SourceDocumentType.Contains(value)));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var totalDebit = await query.SumAsync(x => (decimal?)x.Line.DebitAmount, cancellationToken) ?? 0m;
        var totalCredit = await query.SumAsync(x => (decimal?)x.Line.CreditAmount, cancellationToken) ?? 0m;

        var page = await query
            .OrderBy(x => x.Journal.PostingDate)
            .ThenBy(x => x.Journal.JournalNumber)
            .ThenBy(x => x.Journal.Id)
            .ThenBy(x => x.Line.LineNumber)
            .ThenBy(x => x.Line.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new JournalReportLineDto(
                x.Journal.Id,
                x.Journal.JournalNumber,
                x.Journal.PostingDate,
                x.Journal.DocumentDate,
                (OAS.Contracts.Accounting.Enums.JournalType)(byte)x.Journal.JournalType,
                (OAS.Contracts.Accounting.Enums.JournalEntryStatus)(byte)x.Journal.Status,
                x.Line.AccountId,
                x.AccountCode,
                x.AccountNameAr,
                x.Line.LineNumber,
                x.Line.Description ?? x.Journal.Description,
                x.Line.DebitAmount,
                x.Line.CreditAmount,
                x.Journal.SourceModule,
                x.Journal.SourceDocumentType,
                x.Journal.SourceDocumentId,
                x.Journal.CreatedBy,
                x.Journal.PostedBy,
                x.Line.CustomerId,
                x.Line.SupplierId,
                null, // Reference is resolved after materialization.
                null, // DisplayDescription is resolved after materialization.
                null)) // Reversal information is resolved after materialization.
            .ToListAsync(cancellationToken);

        var reversalInfo = await ResolveJournalReversalsAsync(page.Select(x => x.JournalEntryId), cancellationToken);
        var documents = await ResolveLedgerDocumentsAsync(page.Select(x => (x.SourceDocumentType, x.SourceDocumentId)), cancellationToken);
        var lines = page.Select(x =>
        {
            var reference = GetDocumentReference(x.SourceDocumentType, x.SourceDocumentId, documents);
            reversalInfo.TryGetValue(x.JournalEntryId, out var info);
            return x with
            {
                JournalType = info?.IsReversal == true ? OAS.Contracts.Accounting.Enums.JournalType.Reversal : x.JournalType,
                Reversal = info,
                Reference = reference,
                DisplayDescription = StatementNarrativeFormatter.Resolve(null, reference, x.SourceDocumentType,
                    x.Description, null, x.JournalType == OAS.Contracts.Accounting.Enums.JournalType.Manual)
            };
        }).ToArray();

        return new JournalReportDto(
            fromDate, toDate, lines, totalDebit, totalCredit, totalDebit - totalCredit,
            totalCount, filter.PageNumber, filter.PageSize);
    }

    private async Task<IReadOnlyDictionary<Guid, JournalReversalInfoDto>> ResolveJournalReversalsAsync(IEnumerable<Guid> journalIds, CancellationToken ct)
    {
        var result = new Dictionary<Guid, JournalReversalInfoDto>();
        foreach (var ids in journalIds.Distinct().Chunk(500))
        {
            var headers = await db.Set<JournalEntry>().AsNoTracking().Where(j => ids.Contains(j.Id)).ToListAsync(ct);
            var linkedIds = headers.Where(j => j.ReversedJournalId.HasValue).Select(j => j.ReversedJournalId!.Value).ToArray();
            var related = await db.Set<JournalEntry>().AsNoTracking().Where(j => linkedIds.Contains(j.Id) || (j.ReversedJournalId.HasValue && ids.Contains(j.ReversedJournalId.Value))).ToListAsync(ct);
            foreach (var pair in JournalReversalInfoResolver.Build(headers, related)) result[pair.Key] = pair.Value;
        }
        return result;
    }

    private async Task<(DateOnly FromDate, DateOnly ToDate)> ResolveRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        DateOnly? asOfDate,
        Guid? fiscalYearId,
        Guid? fiscalPeriodId,
        CancellationToken cancellationToken)
    {
        if (asOfDate.HasValue)
            toDate = asOfDate.Value;
        EnsureDateRange(fromDate, toDate);

        if (fiscalPeriodId.HasValue)
        {
            var period = await db.Set<FiscalPeriod>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == fiscalPeriodId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(FiscalPeriod), fiscalPeriodId.Value);
            if (fiscalYearId.HasValue && period.FiscalYearId != fiscalYearId.Value)
                throw new ConflictException("report_period_year_mismatch", "The selected fiscal period does not belong to the selected fiscal year.");
            fromDate = Max(fromDate, period.StartDate);
            toDate = Min(toDate, period.EndDate);
        }
        else if (fiscalYearId.HasValue)
        {
            var year = await db.Set<FiscalYear>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == fiscalYearId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(FiscalYear), fiscalYearId.Value);
            fromDate = Max(fromDate, year.StartDate);
            toDate = Min(toDate, year.EndDate);
        }

        EnsureDateRange(fromDate, toDate);
        return (fromDate, toDate);
    }

    private static HashSet<Guid> ResolveAccountSubtree(Guid rootId, IReadOnlyCollection<AccountReportMeta> accounts)
    {
        var result = new HashSet<Guid> { rootId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in accounts.Where(x => x.ParentAccountId == current))
            {
                if (result.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }
        return result;
    }

    private static AccountMovement SumMovements(
        IEnumerable<Guid> accountIds,
        IReadOnlyDictionary<Guid, AccountMovement> direct)
    {
        decimal openingDebit = 0m, openingCredit = 0m, periodDebit = 0m, periodCredit = 0m;
        foreach (var id in accountIds)
        {
            if (!direct.TryGetValue(id, out var movement))
                continue;
            openingDebit += movement.OpeningDebit;
            openingCredit += movement.OpeningCredit;
            periodDebit += movement.PeriodDebit;
            periodCredit += movement.PeriodCredit;
        }
        return new AccountMovement(Guid.Empty, openingDebit, openingCredit, periodDebit, periodCredit);
    }

    // The key includes the document type to prevent accidentally resolving an unrelated entity.
    private async Task<Dictionary<(string Type, Guid Id), LedgerDocumentInfo>> ResolveLedgerDocumentsAsync(
        IEnumerable<(string? Type, Guid? Id)> sources, CancellationToken ct)
    {
        var keys = sources.Where(x => x.Id.HasValue && !string.IsNullOrWhiteSpace(x.Type))
            .Select(x => (Type: x.Type!.Trim().ToLowerInvariant(), Id: x.Id!.Value))
            .Distinct().ToArray();
        var resolved = new Dictionary<(string Type, Guid Id), LedgerDocumentInfo>();
        async Task AddAsync(string type, Func<Guid[], Task<Dictionary<Guid, LedgerDocumentInfo>>> loader)
        {
            var ids = keys.Where(x => x.Type == type).Select(x => x.Id).ToArray();
            if (ids.Length == 0) return;
            var codes = await loader(ids);
            foreach (var pair in codes)
                resolved[(type, pair.Key)] = pair.Value;
        }

        await AddAsync("journalentryreversal", async ids => await db.Set<JournalEntry>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.JournalNumber, null), ct));
        await AddAsync("salesinvoice", async ids => await db.Set<SalesInvoice>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.InvoiceCode, x.Description), ct));
        await AddAsync("salesreturn", async ids => await db.Set<SalesReturn>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.ReturnCode, x.Reason), ct));
        await AddAsync("purchasereceipt", async ids => await db.Set<PurchaseReceipt>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.ReceiptCode, x.Notes), ct));
        await AddAsync("purchaseinvoice", async ids => await db.Set<PurchaseInvoice>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.PurchaseInvoiceCode, x.Notes), ct));
        await AddAsync("purchasereturn", async ids => await db.Set<PurchaseReturn>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.ReturnCode, x.Reason), ct));
        await AddAsync("receiptvoucher", async ids => await db.Set<ReceiptVoucher>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.VoucherNumber, x.Description), ct));
        await AddAsync("paymentvoucher", async ids => await db.Set<PaymentVoucher>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => new LedgerDocumentInfo(x.VoucherNumber, x.Description), ct));
        await AddAsync("inventorytransaction", async ids => await db.Set<InventoryTransaction>().AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id,
                x => new LedgerDocumentInfo(x.TransactionNumber,
                    string.IsNullOrWhiteSpace(x.Notes) ? x.Reason : x.Notes), ct));
        return resolved;
    }

    private sealed record LedgerDocumentInfo(string Code, string? Narrative);

    private static LedgerDocumentInfo? GetDocumentInfo(
        string? type, Guid? id, IReadOnlyDictionary<(string Type, Guid Id), LedgerDocumentInfo> documents)
    {
        if (!id.HasValue || string.IsNullOrWhiteSpace(type)) return null;
        return documents.TryGetValue((type.Trim().ToLowerInvariant(), id.Value), out var info) ? info : null;
    }

    private static string? GetDocumentReference(
        string? type, Guid? id, IReadOnlyDictionary<(string Type, Guid Id), LedgerDocumentInfo> documents)
    {
        if (!id.HasValue || string.IsNullOrWhiteSpace(type)) return null;
        var code = GetDocumentInfo(type, id, documents)?.Code;
        return string.IsNullOrWhiteSpace(code) ? null : code; // Never expose a source GUID.
    }

    private static string ResolveLedgerDescription(
        string? lineDescription, string journalDescription, string? sourceType, Guid? sourceId,
        IReadOnlyDictionary<(string Type, Guid Id), LedgerDocumentInfo> documents)
    {
        var original = string.IsNullOrWhiteSpace(lineDescription)
            ? journalDescription : lineDescription.Trim();
        var reference = GetDocumentReference(sourceType, sourceId, documents);
        if (reference is null) return original;
        var normalized = (original ?? string.Empty).Trim();
        // Preserve meaningful accounting narratives. Only replace generated posting labels.
        var type = sourceType!.Trim().ToLowerInvariant();
        // Translate only known generated posting labels, while retaining the
        // original meaning and the source line number for auditability.
        if (normalized.StartsWith("COGS reversal /", StringComparison.OrdinalIgnoreCase))
            return $"عكس تكلفة البضاعة المباعة — مرتجع مبيعات رقم {reference}{FormatSourceLineSuffix(normalized)}";
        if (normalized.StartsWith("COGS /", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("OGS /", StringComparison.OrdinalIgnoreCase))
            return $"تكلفة البضاعة المباعة — فاتورة رقم {reference}{FormatSourceLineSuffix(normalized)}";
        if (normalized.StartsWith("Customer return /", StringComparison.OrdinalIgnoreCase))
            return $"تخفيض ذمم العملاء — مرتجع مبيعات رقم {reference}";
        if (normalized.StartsWith("Sales revenue reversal /", StringComparison.OrdinalIgnoreCase))
            return $"عكس إيرادات مبيعات — مرتجع رقم {reference}";
        if (normalized.StartsWith("Sales tax reversal /", StringComparison.OrdinalIgnoreCase))
            return $"عكس ضريبة المبيعات — مرتجع رقم {reference}";
        if (normalized.StartsWith("Sales tax /", StringComparison.OrdinalIgnoreCase))
            return $"ضريبة مبيعات — فاتورة رقم {reference}";
        if (normalized.StartsWith("Inventory /", StringComparison.OrdinalIgnoreCase))
            return $"حركة مخزون مرتبطة بالمستند {reference}{FormatSourceLineSuffix(normalized)}";
        if (normalized.StartsWith("Inventory receipt", StringComparison.OrdinalIgnoreCase))
            return $"إثبات استلام مشتريات رقم {reference}";
        if (normalized.StartsWith("Inventory return", StringComparison.OrdinalIgnoreCase))
            return $"إثبات مرتجع مخزون رقم {reference}";
        if (normalized.StartsWith("Customer /", StringComparison.OrdinalIgnoreCase))
            return $"ذمم العملاء — المستند {reference}";
        if (normalized.StartsWith("Sales revenue /", StringComparison.OrdinalIgnoreCase))
            return $"إيرادات مبيعات — المستند {reference}";
        if (normalized.StartsWith("Sales invoice ", StringComparison.OrdinalIgnoreCase))
            return $"إثبات فاتورة مبيعات رقم {reference}";
        if (normalized.StartsWith("Sales return ", StringComparison.OrdinalIgnoreCase))
            return $"إثبات مرتجع مبيعات رقم {reference}";
        if (normalized.StartsWith("Receipt voucher ", StringComparison.OrdinalIgnoreCase))
            return $"سند قبض رقم {reference}";
        if (normalized.StartsWith("Payment voucher ", StringComparison.OrdinalIgnoreCase))
            return $"سند صرف رقم {reference}";
        if (string.IsNullOrWhiteSpace(normalized))
            return type switch
            {
                "salesinvoice" => $"فاتورة مبيعات رقم {reference}",
                "salesreturn" => $"مرتجع مبيعات رقم {reference}",
                "purchasereceipt" => $"استلام مشتريات رقم {reference}",
                "purchaseinvoice" => $"فاتورة مشتريات رقم {reference}",
                "purchasereturn" => $"مرتجع مشتريات رقم {reference}",
                "receiptvoucher" => $"سند قبض رقم {reference}",
                "paymentvoucher" => $"سند صرف رقم {reference}",
                _ => $"المستند {reference}"
            };
        return original;
    }

    // Preserve generated line numbers to distinguish entries for the same invoice.
    private static string FormatSourceLineSuffix(string description)
    {
        const string marker = " / line ";
        var index = description.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return string.Empty;
        var numberText = description[(index + marker.Length)..].Trim();
        return int.TryParse(numberText,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var number) && number > 0
            ? $" — البند {number}" : string.Empty;
    }

    private static DateOnly Max(DateOnly left, DateOnly right) => left >= right ? left : right;
    private static DateOnly Min(DateOnly left, DateOnly right) => left <= right ? left : right;

    private sealed record AccountReportMeta(
        Guid Id, string Code, string NameAr, string? NameEn, Guid? ParentAccountId, byte Level,
        AccountClass AccountClass, AccountType AccountType, NormalBalance NormalBalance, bool IsPostingAccount);

    private sealed record AccountMovement(
        Guid AccountId, decimal OpeningDebit, decimal OpeningCredit, decimal PeriodDebit, decimal PeriodCredit);

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

    private sealed record ActivityCandidate(
        Guid EntityId,
        string ActivityType,
        string Reference,
        byte Status,
        string? Description,
        DateTimeOffset OccurredAtUtc);

}
