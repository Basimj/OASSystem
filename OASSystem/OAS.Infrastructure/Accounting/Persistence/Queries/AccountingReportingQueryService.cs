using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Reports.Services;
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

public sealed class AccountingReportingQueryService(OasDbContext db, TimeProvider timeProvider) : IAccountingReportingQueryService
{
    public async Task<AccountingDashboardDto> GetAccountingDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeProvider.LocalTimeZone);
        var today = DateOnly.FromDateTime(localNow.DateTime);

        var unpostedJournalCount = await db.Set<JournalEntry>()
            .AsNoTracking()
            .LongCountAsync(x =>
                x.Status != JournalEntryStatus.Posted &&
                x.Status != JournalEntryStatus.Reversed,
                cancellationToken);

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

        var attention = new List<AccountingDashboardAttentionDto>(3);
        if (unpostedJournalCount > 0)
        {
            attention.Add(new AccountingDashboardAttentionDto(
                "unposted-journals",
                $"{unpostedJournalCount:N0} قيود تنتظر الترحيل",
                "danger",
                "القيود اليومية"));
        }

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
            activities);
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
            query = query.Where(x => x.JournalType == journalType);
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
                    string.IsNullOrWhiteSpace(movement.Description)
                        ? movement.JournalDescription
                        : movement.Description!,
                    movement.SourceModule,
                    movement.SourceDocumentType,
                    movement.SourceDocumentId,
                    movement.LineNumber,
                    movement.DebitAmount,
                    movement.CreditAmount,
                    running,
                    (OAS.Contracts.Accounting.Enums.JournalType)(byte)movement.JournalType,
                    movement.CustomerId,
                    movement.SupplierId,
                    movement.TransactionCurrencyCodeSnapshot,
                    movement.TransactionDebitAmount,
                    movement.TransactionCreditAmount,
                    movement.ExchangeRate,
                    FormatReference(movement.SourceDocumentType, movement.SourceDocumentId)));
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
                aggregate));
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
            query = query.Where(x => x.Journal.JournalType == value);
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
        if (filter.AccountId.HasValue)
            query = query.Where(x => x.Line.AccountId == filter.AccountId.Value);
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
            .ThenBy(x => x.Line.LineNumber)
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
                null))
            .ToListAsync(cancellationToken);

        var lines = page.Select(x => x with
        {
            Reference = FormatReference(x.SourceDocumentType, x.SourceDocumentId)
        }).ToArray();

        return new JournalReportDto(
            fromDate, toDate, lines, totalDebit, totalCredit, totalDebit - totalCredit,
            totalCount, filter.PageNumber, filter.PageSize);
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

    private static string? FormatReference(string? sourceDocumentType, Guid? sourceDocumentId) =>
        string.IsNullOrWhiteSpace(sourceDocumentType) && !sourceDocumentId.HasValue
            ? null
            : $"{sourceDocumentType}{(sourceDocumentId.HasValue ? $" / {sourceDocumentId:D}" : string.Empty)}";

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
