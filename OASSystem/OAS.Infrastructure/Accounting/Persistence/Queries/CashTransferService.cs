using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashTransfers;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.Rules;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class CashTransferService(OasDbContext db, ISequenceNumberGenerator sequences,
    IExchangeRateResolver rates, ICurrencyRoundingService rounding, ICurrentUser user, TimeProvider clock) : ICashTransferService, ICashShiftSettlementService
{
    private static ConflictException Error(string code, string message) => new(code, message);
    private Guid Actor() => Guid.TryParse(user.UserId, out var id) ? id : throw new ForbiddenException();
    private async Task<(AccountingSettings Settings, Currency Base)> BaseAsync(CancellationToken ct)
    {
        var config = await db.Set<AccountingSettings>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == AccountingSettings.SingletonId, ct)
            ?? throw Error("accounting_settings_required", "حدد إعدادات المحاسبة والعملة الأساسية.");
        var currency = await db.Set<Currency>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == config.BaseCurrencyId, ct)
            ?? throw Error("base_currency_missing", "العملة الأساسية غير متاحة.");
        if (!currency.IsActive || currency.DecimalPlaces > 4) throw Error("cash_transfer_currency_precision", "العملة الأساسية غير نشطة أو دقتها تتجاوز أربع منازل.");
        return (config, currency);
    }
    private async Task<List<CashTransferAccountDto>> AccountsAsync(CancellationToken ct)
    {
        var cash = await (from x in db.Set<CashAccount>().AsNoTracking()
                          join c in db.Set<Currency>().AsNoTracking() on x.CurrencyId equals (Guid?)c.Id
                          join a in db.Set<Account>().AsNoTracking() on x.AccountId equals a.Id
                          where x.IsActive && c.IsActive && c.DecimalPlaces <= 4 && a.IsActive && a.IsPostingAccount && a.AccountClass == AccountClass.Asset
                          select new CashTransferAccountDto(a.Id, x.Name, "Cash", c.Id, c.Code, c.DecimalPlaces)).ToListAsync(ct);
        var banks = await (from x in db.Set<BankAccount>().AsNoTracking()
                           join c in db.Set<Currency>().AsNoTracking() on x.CurrencyId equals (Guid?)c.Id
                           join a in db.Set<Account>().AsNoTracking() on x.AccountId equals a.Id
                           where x.IsActive && c.IsActive && c.DecimalPlaces <= 4 && a.IsActive && a.IsPostingAccount && a.AccountClass == AccountClass.Asset
                           select new CashTransferAccountDto(a.Id, x.BankName + " - " + x.AccountName, "Bank", c.Id, c.Code, c.DecimalPlaces)).ToListAsync(ct);
        return cash.Concat(banks).GroupBy(x => x.AccountId).Where(g => g.Count() == 1).Select(g => g.Single()).OrderBy(x => x.Name).ToList();
    }
    public async Task<CashTransferLookupsDto> LookupsAsync(CancellationToken ct, DateOnly? date = null)
    {
        var (_, b) = await BaseAsync(ct);
        var fees = await db.Set<Account>().AsNoTracking().Where(x => x.IsActive && x.IsPostingAccount && x.AllowManualPosting && !x.IsControlAccount && x.AccountClass == AccountClass.Expense && x.AccountType == AccountType.Posting)
            .OrderBy(x => x.Code).Select(x => new CashTransferFeeAccountDto(x.Id, x.Code + " - " + x.NameAr)).ToListAsync(ct);
        var accounts = await AccountsAsync(ct);
        if (date is DateOnly asOf && accounts.Count > 0)
        {
            // One ledger read for all eligible accounts; use the same calculation as posting.
            var rows = (await ReadBalanceRowsAsync(accounts.Select(x => x.AccountId).ToList(), ct)).ToLookup(x => x.AccountId);
            var currencyIds = accounts.Select(x => x.CurrencyId).Distinct().ToList();
            // Same active Accounting rate and date rule as ExchangeRateResolver.
            // Missing rates are a normal preview state, not an exception.
            var latest = await db.Set<ExchangeRate>().AsNoTracking()
                .Where(x => currencyIds.Contains(x.CurrencyId) && x.IsActive && x.RateType == ExchangeRateType.Accounting && x.RateDate <= asOf)
                .GroupBy(x => x.CurrencyId)
                .Select(g => g.OrderByDescending(x => x.RateDate).First())
                .ToListAsync(ct);
            var rateByCurrency = latest.ToDictionary(x => x.CurrencyId);
            for (var i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];
                var rate = rateByCurrency.GetValueOrDefault(account.CurrencyId);
                account = account with
                {
                    AccountingRate = account.CurrencyId == b.Id ? 1m : rate?.Rate,
                    RateDate = account.CurrencyId == b.Id ? asOf : rate?.RateDate
                };
                var balance = CalculateBalance(rows[account.AccountId], account.CurrencyId, b.Id, asOf);
                account = balance.ErrorCode is null
                    ? account with { BookBalance = balance.Native, BookBaseBalance = balance.Base }
                    : account with { BalanceError = balance.ErrorMessage };
                accounts[i] = account;
            }
        }
        return new(accounts, fees, b.Code) { BaseDecimalPlaces = b.DecimalPlaces };
    }
    public async Task<CashTransferPageDto> ListAsync(int page, string? search, CancellationToken ct, string? type = null, int? status = null)
    {
        page = Math.Clamp(page, 1, 1000000);
        var q = db.Set<CashTransfer>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); q = q.Where(x => x.TransferNumber.Contains(term) || x.SourceName.Contains(term) || x.DestinationName.Contains(term) || x.Description.Contains(term) || (x.Reference != null && x.Reference.Contains(term))); }
        // Include inactive definitions when classifying historical transfers; filter before paging.
        var cashIds = db.Set<CashAccount>().Select(x => x.AccountId);
        var bankIds = db.Set<BankAccount>().Select(x => x.AccountId);
        q = type switch
        {
            null or "" => q,
            "cash-cash" => q.Where(x => cashIds.Contains(x.SourceAccountId) && cashIds.Contains(x.DestinationAccountId)),
            "cash-bank" => q.Where(x => cashIds.Contains(x.SourceAccountId) && bankIds.Contains(x.DestinationAccountId)),
            "bank-cash" => q.Where(x => bankIds.Contains(x.SourceAccountId) && cashIds.Contains(x.DestinationAccountId)),
            "bank-bank" => q.Where(x => bankIds.Contains(x.SourceAccountId) && bankIds.Contains(x.DestinationAccountId)),
            "fx" => q.Where(x => x.SourceCurrencyId != x.DestinationCurrencyId),
            _ => throw Error("cash_transfer_type_invalid", "نوع التحويل غير صالح.")
        };
        if (status.HasValue)
        {
            if (status.Value < 1 || status.Value > 5) throw Error("cash_transfer_status_invalid", "حالة التحويل غير صالحة.");
            var selectedStatus = (CashTransferStatus)status.Value;
            q = q.Where(x => x.Status == selectedStatus);
        }
        var count = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.TransferDate).ThenByDescending(x => x.TransferNumber).Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        if (items.Count == 0) return new(Array.Empty<CashTransferDto>(), count, page);
        var ids = items.SelectMany(x => new[] { x.SourceAccountId, x.DestinationAccountId }).Distinct().ToList();
        var cash = (await cashIds.Where(x => ids.Contains(x)).ToListAsync(ct)).ToHashSet();
        var banks = (await bankIds.Where(x => ids.Contains(x)).ToListAsync(ct)).ToHashSet();
        string? Kind(Guid id) => cash.Contains(id) == banks.Contains(id) ? null : cash.Contains(id) ? "Cash" : "Bank";
        return new(items.Select(x => new CashTransferDto(x.Id, x.TransferNumber, x.TransferDate, x.SourceAccountId, x.DestinationAccountId,
            x.SourceName, x.DestinationName, x.SourceCurrencyCode, x.DestinationCurrencyCode, x.SourceAmount, x.DestinationAmount, x.FeeAmount,
            x.FeeAccountId, x.Reference, x.Description, (int)x.Status, x.JournalEntryId, x.ReversalJournalEntryId, x.ReversalDate, x.ReversalReason,
            x.SourceBaseAmount, x.DestinationBaseAmount, x.FeeBaseAmount, x.FxDifferenceBase, Convert.ToBase64String(x.RowVersion), Kind(x.SourceAccountId), Kind(x.DestinationAccountId), x.SourceCurrencyId != x.DestinationCurrencyId)).ToList(), count, page);
    }
    private async Task<CashTransfer> LoadAsync(Guid id, string? rowVersion, CancellationToken ct)
    {
        var transfer = await db.Set<CashTransfer>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(CashTransfer), id);
        if (string.IsNullOrWhiteSpace(rowVersion) || rowVersion != Convert.ToBase64String(transfer.RowVersion))
            throw new ConcurrencyException("تغير التحويل. أعد تحميله.");
        return transfer;
    }
    private async Task<(CashTransferAccountDto Source, CashTransferAccountDto Destination)> ResolveAccounts(Guid source, Guid destination, CancellationToken ct)
    {
        var accounts = await AccountsAsync(ct);
        var a = accounts.SingleOrDefault(x => x.AccountId == source);
        var b = accounts.SingleOrDefault(x => x.AccountId == destination);
        if (a is null || b is null || source == destination) throw Error("cash_transfer_accounts_invalid", "اختر حسابين نقديين مختلفين ونشطين.");
        return (a, b);
    }
    private async Task ValidateCounterAccount(Guid id, AccountClass expected, CancellationToken ct, bool manual = false)
    {
        var account = await db.Set<Account>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (account is null || !account.CanReceivePosting(manual) || account.AccountClass != expected || account.IsControlAccount || account.AccountType != AccountType.Posting)
            throw Error("cash_transfer_counter_account_invalid", "الحساب المقابل للرسوم أو فروق الصرف غير صالح.");
        if (await db.Set<CashAccount>().AnyAsync(x => x.AccountId == id, ct) || await db.Set<BankAccount>().AnyAsync(x => x.AccountId == id, ct))
            throw Error("cash_transfer_counter_account_invalid", "لا يجوز استخدام حساب نقدي للرسوم أو فروق الصرف.");
    }
    private void ValidateAmounts(CashTransferAccountDto a, CashTransferAccountDto b, decimal source, decimal dest, decimal fee, Guid? feeAccount)
    {
        if (source <= 0 || dest <= 0 || source >= 1000000000000000m || dest >= 1000000000000000m || fee < 0 || fee >= source ||
            rounding.Round(source, a.DecimalPlaces) != source || rounding.Round(dest, b.DecimalPlaces) != dest || rounding.Round(fee, a.DecimalPlaces) != fee ||
            (fee > 0) != feeAccount.HasValue || (a.CurrencyId == b.CurrencyId && source != dest + fee))
            throw Error("cash_transfer_amount_invalid", "تحقق من المبالغ والرسوم ودقة العملات. عند تطابق العملتين يجب أن يساوي الصادر الوارد مضافًا إليه الرسوم.");
    }
    private async Task<FiscalPeriod> PeriodAsync(DateOnly date, CancellationToken ct)
    {
        var periods = await db.Set<FiscalPeriod>().AsNoTracking().Where(x => x.StartDate <= date && x.EndDate >= date).Take(2).ToListAsync(ct);
        if (periods.Count != 1 || !periods[0].CanPostAccounting()) throw Error("cash_transfer_period_invalid", "تاريخ التحويل أو العكس يجب أن يقع ضمن فترة مالية مفتوحة واحدة.");
        return periods[0];
    }
    public async Task<Guid> SaveAsync(Guid? id, CashTransferRequest r, CancellationToken ct)
    {
        await LockAsync(db, ct);
        if (string.IsNullOrWhiteSpace(r.Description) || r.Description.Trim().Length > 400 || r.Reference?.Length > 100)
            throw Error("cash_transfer_description_invalid", "أدخل بيانًا لا يتجاوز 400 حرف ومرجعًا لا يتجاوز 100 حرف.");
        var (a, b) = await ResolveAccounts(r.SourceAccountId, r.DestinationAccountId, ct);
        ValidateAmounts(a, b, r.SourceAmount, r.DestinationAmount, r.FeeAmount, r.FeeAccountId);
        if (r.FeeAccountId is Guid feeAccount) await ValidateCounterAccount(feeAccount, AccountClass.Expense, ct, true);
        await PeriodAsync(r.Date, ct);
        CashTransfer transfer;
        if (id.HasValue) { transfer = await LoadAsync(id.Value, r.RowVersion, ct); if (transfer.Status != CashTransferStatus.Draft) throw Error("cash_transfer_not_draft", "لا يعدل إلا التحويل المسودة."); }
        else { var seq = await sequences.NextAsync($"CashTransfer-{r.Date.Year}", ct); transfer = CashTransfer.Create(Guid.NewGuid(), $"TR-{r.Date.Year:0000}-{seq:000000}"); db.Add(transfer); }
        transfer.SetDetails(r.Date, a.AccountId, b.AccountId, a.CurrencyId, b.CurrencyId, a.Name, b.Name, a.CurrencyCode, b.CurrencyCode,
            r.SourceAmount, r.DestinationAmount, r.FeeAmount, r.FeeAccountId, r.Reference?.Trim(), r.Description.Trim());
        return transfer.Id;
    }
    internal static async Task LockAsync(OasDbContext db, CancellationToken ct)
    {
        if (!db.Database.IsSqlServer()) return;
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Cash transfer must run inside a transaction.");
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'OAS.CashShift.Posting.v1', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
            IF @result < 0 THROW 51052, 'Cash posting lock unavailable. Retry the operation.', 1;
            """, ct);
    }
    public async Task<Guid> ActionAsync(Guid id, string action, CashTransferActionRequest r, CancellationToken ct)
    {
        await LockAsync(db, ct);
        var transfer = await LoadAsync(id, r.RowVersion, ct);
        var actor = Actor(); var now = clock.GetUtcNow().UtcDateTime;
        switch (action)
        {
            case "approve" when transfer.Status == CashTransferStatus.Draft:
                await PeriodAsync(transfer.TransferDate, ct); transfer.Approve(actor, now); break;
            case "cancel" when transfer.Status is CashTransferStatus.Draft or CashTransferStatus.Approved:
                transfer.Cancel(); break;
            case "post" when transfer.Status == CashTransferStatus.Approved:
                await PostAsync(transfer, actor, now, ct); break;
            case "reverse" when transfer.Status == CashTransferStatus.Posted:
                await ReverseAsync(transfer, r, actor, now, ct); break;
            default: throw Error("cash_transfer_status_invalid", "الإجراء غير متاح للحالة الحالية.");
        }
        return transfer.Id;
    }
    // Conservative carrying-value policy: weighted average of posted native/base balances.
    // Reject ambiguous historical amounts, backdated foreign withdrawals and negative carrying balances.
    private sealed record BalanceRow(Guid AccountId, string JournalNumber, DateOnly PostingDate, Guid? BaseCurrencyId,
        Guid? TransactionCurrencyId, decimal? TransactionDebitAmount, decimal? TransactionCreditAmount, decimal DebitAmount, decimal CreditAmount);

    private Task<List<BalanceRow>> ReadBalanceRowsAsync(List<Guid> accountIds, CancellationToken ct) =>
        (from l in db.Set<JournalEntryLine>().AsNoTracking()
         join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
         where accountIds.Contains(l.AccountId) && j.Status == JournalEntryStatus.Posted
         select new BalanceRow(l.AccountId, j.JournalNumber, j.PostingDate, j.BaseCurrencyId, l.TransactionCurrencyId,
             l.TransactionDebitAmount, l.TransactionCreditAmount, l.DebitAmount, l.CreditAmount)).ToListAsync(ct);

    private sealed record BalanceResult(decimal Native, decimal Base, string? ErrorCode = null, string? ErrorMessage = null);

    internal async Task<(decimal Native, decimal Base)> BalanceAsync(Guid account, Guid currency, Guid baseId, DateOnly date, CancellationToken ct)
    {
        var balance = CalculateBalance(await ReadBalanceRowsAsync([account], ct), currency, baseId, date);
        if (balance.ErrorCode is string code) throw Error(code, balance.ErrorMessage!);
        return (balance.Native, balance.Base);
    }

    private static BalanceResult CalculateBalance(IEnumerable<BalanceRow> rows, Guid currency, Guid baseId, DateOnly date)
    {
        if (rows.Any(x => x.PostingDate > date))
            return new(0, 0, "cash_transfer_backdate_invalid", "توجد حركات مرحلة بعد التاريخ المختار. لا يسمح بالتحويل بأثر رجعي لهذا الحساب.");
        decimal native = 0, amount = 0;
        foreach (var r in rows)
        {
            if (r.BaseCurrencyId.HasValue && r.BaseCurrencyId != baseId)
                return new(0, 0, "cash_transfer_base_history_invalid", "العملة الأساسية التاريخية للحساب مختلفة.");
            if (!CashShiftBalanceService.TryResolveMovement(currency, r.BaseCurrencyId, r.TransactionCurrencyId,
                    r.TransactionDebitAmount, r.TransactionCreditAmount, r.DebitAmount, r.CreditAmount, out var movement))
                return new(0, 0, "cash_shift_movement_currency_unknown", $"لا يمكن تحديد رصيد الحساب بعملته من القيد {r.JournalNumber}. راجع الحساب.");
            native += movement;
            amount += r.DebitAmount - r.CreditAmount;
        }
        return new(native, amount);
    }
    private async Task<JournalEntry> NewJournal(DateOnly date, string description, string type, Guid source, Currency b, CancellationToken ct, JournalType journalType = JournalType.Automatic)
    {
        var period = await PeriodAsync(date, ct);
        var seq = await sequences.NextAsync($"JournalEntry-{date.Year}", ct);
        var j = JournalEntry.Create(Guid.NewGuid(), $"JV-{date.Year:0000}-{seq:000000}", journalType, date, date, period.Id,
            description, "Accounting", type, source, JournalEntryStatus.Draft);
        j.SetBaseCurrencySnapshot(b.Id, b.Code, b.DecimalPlaces);
        return j;
    }
    private static void AddLine(JournalEntry j, Guid account, decimal baseAmount, decimal native, Guid currency, string code, byte decimals, bool debit, DateOnly date, decimal rate, string role)
    {
        if (baseAmount <= 0 || baseAmount >= 1000000000000000m || native <= 0 || rate <= 0 || rate >= 100000000000m || Math.Round(rate, 8) <= 0)
            throw Error("cash_transfer_value_invalid", "القيمة أو سعر الصرف خارج النطاق المحاسبي.");
        j.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), j.Id, j.Lines.Count + 1, account,
            debit ? baseAmount : 0m, debit ? 0m : baseAmount, currency, code, decimals, debit ? native : 0m, debit ? 0m : native,
            Math.Round(rate, 8, MidpointRounding.AwayFromZero), date, ExchangeRateType.Accounting, ExchangeRateSource.System,
            j.Description, null, null, null, null, null, null, null, null, role));
    }
    private void Complete(JournalEntry j, Guid actor, DateTime now)
    {
        JournalPostingRules.EnsureCurrencySnapshots(j, j.Lines);
        j.SetPendingApproval(); j.Approve(actor, now); j.Post(actor, now); db.Add(j);
    }
    private async Task PostAsync(CashTransfer t, Guid actor, DateTime now, CancellationToken ct)
    {
        var (a, b) = await ResolveAccounts(t.SourceAccountId, t.DestinationAccountId, ct);
        if (a.CurrencyId != t.SourceCurrencyId || b.CurrencyId != t.DestinationCurrencyId) throw Error("cash_transfer_currency_changed", "تغيرت عملة الحساب منذ إعداد التحويل.");
        ValidateAmounts(a, b, t.SourceAmount, t.DestinationAmount, t.FeeAmount, t.FeeAccountId);
        if (t.FeeAccountId is Guid feeAccount) await ValidateCounterAccount(feeAccount, AccountClass.Expense, ct, true);
        var (settings, baseCurrency) = await BaseAsync(ct);
        var sourceBalance = await BalanceAsync(a.AccountId, a.CurrencyId, baseCurrency.Id, t.TransferDate, ct);
        if (sourceBalance.Native < t.SourceAmount || sourceBalance.Base <= 0) throw Error("cash_transfer_insufficient_balance", "الرصيد الدفتري المتاح في الحساب المصدر لا يكفي للتحويل.");
        var carryingRate = a.CurrencyId == baseCurrency.Id ? 1m : sourceBalance.Base / sourceBalance.Native;
        var sourceBase = t.SourceAmount == sourceBalance.Native && a.CurrencyId != baseCurrency.Id ? sourceBalance.Base : rounding.Round(t.SourceAmount * carryingRate, baseCurrency.DecimalPlaces);
        if (sourceBase > sourceBalance.Base) throw Error("cash_transfer_insufficient_balance", "القيمة الدفترية لا تكفي للتحويل.");
        var feeBase = rounding.Round(t.FeeAmount * carryingRate, baseCurrency.DecimalPlaces);
        decimal destBase; decimal destRate; DateOnly destRateDate;
        if (a.CurrencyId == b.CurrencyId)
        {
            destBase = sourceBase - feeBase; destRate = destBase / t.DestinationAmount; destRateDate = t.TransferDate;
        }
        else
        {
            var rate = await rates.ResolveAsync(b.CurrencyId, t.TransferDate, cancellationToken: ct);
            destRate = rate.Rate; destRateDate = rate.RateDate;
            destBase = rounding.CalculateBaseAmount(t.DestinationAmount, destRate, b.DecimalPlaces, baseCurrency.DecimalPlaces);
        }
        if (t.FeeAmount > 0 && feeBase <= 0) throw Error("cash_transfer_fee_invalid", "الرسوم أصغر من دقة العملة الأساسية.");
        var difference = destBase + feeBase - sourceBase;
        var journal = await NewJournal(t.TransferDate, $"تحويل مالي {t.TransferNumber}", "CashTransfer", t.Id, baseCurrency, ct);
        AddLine(journal, a.AccountId, sourceBase, t.SourceAmount, a.CurrencyId, a.CurrencyCode, a.DecimalPlaces, false, t.TransferDate, carryingRate, "TransferSource");
        AddLine(journal, b.AccountId, destBase, t.DestinationAmount, b.CurrencyId, b.CurrencyCode, b.DecimalPlaces, true, destRateDate, destRate, "TransferDestination");
        if (t.FeeAccountId is Guid feeId) AddLine(journal, feeId, feeBase, t.FeeAmount, a.CurrencyId, a.CurrencyCode, a.DecimalPlaces, true, t.TransferDate, carryingRate, "TransferFee");
        if (difference != 0)
        {
            var account = (difference > 0 ? settings.ExchangeGainAccountId : settings.ExchangeLossAccountId) ?? throw Error("cash_transfer_fx_account_missing", "حدد حساب أرباح أو خسائر فروق العملات.");
            await ValidateCounterAccount(account, difference > 0 ? AccountClass.Revenue : AccountClass.Expense, ct);
            AddLine(journal, account, Math.Abs(difference), Math.Abs(difference), baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces, difference < 0, t.TransferDate, 1m, "TransferFxDifference");
        }
        Complete(journal, actor, now); t.Post(journal.Id, sourceBase, destBase, feeBase, difference);
    }
    private async Task ReverseAsync(CashTransfer t, CashTransferActionRequest r, Guid actor, DateTime now, CancellationToken ct)
    {
        if (r.ReversalDate is not DateOnly date || date < t.TransferDate || string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Trim().Length > 400)
            throw Error("cash_transfer_reverse_invalid", "حدد تاريخ العكس وسببًا صحيحًا.");
        var (a, b) = await ResolveAccounts(t.SourceAccountId, t.DestinationAccountId, ct);
        if (a.CurrencyId != t.SourceCurrencyId || b.CurrencyId != t.DestinationCurrencyId) throw Error("cash_transfer_currency_changed", "تغيرت عملة أحد الحسابات.");
        var (_, baseCurrency) = await BaseAsync(ct);
        var original = await db.Set<JournalEntry>().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == t.JournalEntryId, ct)
            ?? throw Error("cash_transfer_journal_missing", "القيد الأصلي غير موجود.");
        if (original.Status != JournalEntryStatus.Posted || original.ReversedJournalId.HasValue || original.BaseCurrencyId != baseCurrency.Id || original.SourceDocumentType != "CashTransfer" || original.SourceDocumentId != t.Id)
            throw Error("cash_transfer_journal_invalid", "القيد الأصلي غير صالح للعكس.");
        var balance = await BalanceAsync(b.AccountId, b.CurrencyId, baseCurrency.Id, date, ct);
        if (balance.Native < t.DestinationAmount || balance.Base < t.DestinationBaseAmount.GetValueOrDefault()) throw Error("cash_transfer_insufficient_balance", "لا يكفي رصيد الوجهة لعكس التحويل.");
        var reversal = await NewJournal(date, $"عكس تحويل مالي {t.TransferNumber}", "CashTransferReversal", t.Id, baseCurrency, ct, JournalType.Reversal);
        foreach (var line in original.Lines.OrderBy(x => x.LineNumber))
        {
            var account = await db.Set<Account>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == line.AccountId, ct);
            if (account is null || !account.CanReceivePosting()) throw Error("cash_transfer_reverse_account_invalid", "أحد حسابات القيد الأصلي غير صالح للترحيل.");
            reversal.AddLine(line.CreateOpposite(Guid.NewGuid(), reversal.Id));
        }
        Complete(reversal, actor, now); original.MarkReversed(reversal.Id); t.Reverse(reversal.Id, date, r.Reason.Trim());
    }
    private async Task<CashShift> GetSettleableShift(Guid id, CancellationToken ct)
    {
        var shift = await db.Set<CashShift>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(CashShift), id);
        if (shift.Status != CashShiftStatus.Approved || shift.SettlementJournalEntryId.HasValue || !shift.DifferenceAmount.HasValue || shift.DifferenceAmount == 0 || !shift.UsesLinkedMovements)
            throw Error("cash_shift_settlement_invalid", "التسوية متاحة للورديات المعتمدة ذات الفرق غير المسوى والمرتبطة بالحركات مباشرة. الورديات التاريخية تحتاج مراجعة مستقلة.");
        return shift;
    }
    public async Task<IReadOnlyList<CashShiftSettlementAccountDto>> SettlementAccountsAsync(Guid shiftId, CancellationToken ct)
    {
        var shift = await GetSettleableShift(shiftId, ct);
        var deficit = shift.DifferenceAmount < 0;
        var accountClass = deficit ? AccountClass.Expense : AccountClass.Revenue;
        var secondaryClass = deficit ? AccountClass.Asset : AccountClass.Liability;
        var cashIds = db.Set<CashAccount>().Select(x => x.AccountId);
        var bankIds = db.Set<BankAccount>().Select(x => x.AccountId);
        return await db.Set<Account>().AsNoTracking().Where(x => x.IsActive && x.IsPostingAccount && x.AllowManualPosting && !x.IsControlAccount && x.AccountType == AccountType.Posting &&
                (x.AccountClass == accountClass || x.AccountClass == secondaryClass) && !cashIds.Contains(x.Id) && !bankIds.Contains(x.Id))
            .OrderBy(x => x.Code).Select(x => new CashShiftSettlementAccountDto(x.Id, x.Code + " - " + x.NameAr)).ToListAsync(ct);
    }
    public async Task<Guid> SettleAsync(Guid shiftId, CashShiftSettlementRequest request, CancellationToken ct)
    {
        await LockAsync(db, ct);
        var shift = await GetSettleableShift(shiftId, ct);
        if (request.RowVersion != Convert.ToBase64String(shift.RowVersion)) throw new ConcurrencyException("تغيرت الوردية. أعد تحميلها.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 400 || shift.ClosedAtUtc is not DateTime closed || request.Date < DateOnly.FromDateTime(closed))
            throw Error("cash_shift_settlement_details_invalid", "حدد تاريخ التسوية بعد الإغلاق وسببًا لا يتجاوز 400 حرف.");
        if (await db.Set<CashShift>().AnyAsync(x => x.CashAccountId == shift.CashAccountId && (x.Status == CashShiftStatus.Open || x.Status == CashShiftStatus.Closing), ct))
            throw Error("cash_shift_settlement_active", "أغلق أي وردية نشطة للصندوق قبل تسوية فرق وردية سابقة.");
        var box = await db.Set<CashAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == shift.CashAccountId, ct)
            ?? throw new NotFoundException(nameof(CashAccount), shift.CashAccountId);
        if (!box.IsActive || box.CurrencyId is not Guid currencyId) throw Error("cash_shift_settlement_cash_invalid", "الصندوق غير نشط أو عملته غير محددة.");
        var currency = await db.Set<Currency>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == currencyId, ct);
        if (currency is null || !currency.IsActive || currency.DecimalPlaces > 4) throw Error("cash_shift_settlement_currency_invalid", "عملة الصندوق غير متاحة.");
        var options = await SettlementAccountsAsync(shiftId, ct);
        if (!options.Any(x => x.AccountId == request.AccountId)) throw Error("cash_shift_settlement_account_invalid", "اختر حساب تسوية صالحًا من القائمة.");
        var (_, b) = await BaseAsync(ct);
        var shortage = shift.DifferenceAmount!.Value < 0; var amount = Math.Abs(shift.DifferenceAmount.Value);
        var ledger = await BalanceAsync(box.AccountId, currency.Id, b.Id, request.Date, ct);
        decimal rate; DateOnly rateDate;
        if (shortage)
        {
            if (ledger.Native < amount || ledger.Base <= 0) throw Error("cash_shift_settlement_balance_invalid", "الرصيد الدفتري لا يكفي لإثبات العجز. راجع الحساب.");
            rate = currency.Id == b.Id ? 1m : ledger.Base / ledger.Native; rateDate = request.Date;
        }
        else
        {
            var resolved = await rates.ResolveAsync(currency.Id, request.Date, cancellationToken: ct);
            rate = resolved.Rate; rateDate = resolved.RateDate;
        }
        var baseAmount = shortage && currency.Id != b.Id && amount == ledger.Native ? ledger.Base : rounding.Round(amount * rate, b.DecimalPlaces);
        var journal = await NewJournal(request.Date, $"تسوية فرق الوردية {shift.ShiftNumber}", "CashShiftSettlement", shift.Id, b, ct);
        AddLine(journal, box.AccountId, baseAmount, amount, currency.Id, currency.Code, currency.DecimalPlaces, !shortage, rateDate, rate, "ShiftAdjustment");
        AddLine(journal, request.AccountId, baseAmount, amount, currency.Id, currency.Code, currency.DecimalPlaces, shortage, rateDate, rate, "ShiftAdjustmentCounterpart");
        Complete(journal, Actor(), clock.GetUtcNow().UtcDateTime);
        shift.RecordSettlement(journal.Id, request.AccountId, request.Date, request.Reason);
        return journal.Id;
    }
}
