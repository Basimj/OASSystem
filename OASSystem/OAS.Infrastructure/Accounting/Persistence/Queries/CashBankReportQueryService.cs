using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;
using OAS.Domain.Identity.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

// Read-only financial report; no posting or historical data changes.
public sealed class CashBankReportQueryService(OasDbContext db) : ICashBankReportQueryService
{
    private const int MaxRows = 50000;
    private const int MaxLedger = 250000;
    private static CashBankReportCell T(string? text, Guid? link = null, string? kind = null) => new(Text: text, LinkId: link, LinkKind: kind);
    private static CashBankReportCell N(decimal? value) => new(Amount: value, Format: "native");
    private static CashBankReportCell B(decimal? value) => new(Amount: value, Format: "base");
    private static CashBankReportCell C(int value) => new(Amount: value, Format: "count");
    private static CashBankReportMetric M(string name, CashBankReportCell value) => new(name, value);
    private static CashBankReportRow R(params CashBankReportCell[] cells) => new(cells);
    private static string Date(DateOnly d) => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    private static string DateTimeText(DateTime? d) => d?.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) ?? "—";
    private static string Kind(string k) => k == "cash" ? "صندوق" : "بنك";
    private static decimal? Sum(IEnumerable<decimal?> values) => CashBankReportMath.Sum(values);
    private static void Limit(int count, int max) { if (count > max) throw new ConflictException("cash_bank_report_limit", "حجم التقرير يتجاوز الحد المسموح. حدد حساباً أو فترة أقصر."); }
    private sealed record Line(Guid AccountId, Guid JournalId, string Number, DateOnly Date, DateTime? PostedAt, int LineNumber,
        JournalType Type, string? SourceType, string? Description, Guid? BaseId, Guid? TxId, decimal? TxDebit, decimal? TxCredit,
        decimal Debit, decimal Credit, Guid? ShiftId);
    private sealed record Balance(CashBankReportAccountDto Account, decimal? Open, decimal? In, decimal? Out,
        decimal? BaseOpen, decimal? BaseIn, decimal? BaseOut)
    {
        public decimal? Close => Open + In - Out;
        public decimal? BaseClose => BaseOpen + BaseIn - BaseOut;
    }
    private sealed record TransferEvent(CashTransfer Transfer, JournalEntry Journal, bool Reversal);

    public async Task<CashBankReportDto> GetAsync(CashBankReportFilterDto f, CancellationToken ct)
    {
        var config = await db.Set<AccountingSettings>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException("accounting_settings_required", "حدد إعدادات المحاسبة أولاً.");
        var currencies = await db.Set<Currency>().AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new CashBankReportCurrencyDto(x.Id, x.Code, x.NameAr, x.DecimalPlaces)).ToListAsync(ct);
        var baseCurrency = currencies.FirstOrDefault(x => x.Id == config.BaseCurrencyId)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية غير محددة.");
        var cash = await db.Set<CashAccount>().AsNoTracking().ToListAsync(ct);
        var banks = await db.Set<BankAccount>().AsNoTracking().ToListAsync(ct);
        var definitions = cash.Select(x => new CashBankReportAccountDto(x.AccountId, x.CashBox?.Code ?? x.Code, x.CashBox?.Name ?? x.Name, "cash", x.CurrencyId, x.IsActive, (byte)x.MovementType, x.CashBoxId, x.CashBox?.Name ?? x.Name))
            .Concat(banks.Select(x => new CashBankReportAccountDto(x.AccountId, x.Code, x.BankName + " - " + x.AccountName, "bank", x.CurrencyId, x.IsActive))).ToArray();
        var warnings = new List<string>();
        var groups = definitions.GroupBy(x => x.AccountId).ToArray();
        if (groups.Any(g => g.Count() != 1)) warnings.Add("استُبعدت الحسابات المحاسبية المشتركة من الأرصدة لتجنب تكرارها. التقرير غير شامل حتى تصحيح الربط.");
        var accounts = groups.Where(g => g.Count() == 1).Select(g => g.Single()).OrderBy(x => x.Kind).ThenBy(x => x.Name).ToArray();
        var selected = accounts.Where(x => (f.Kind == "all" || x.Kind == f.Kind) && (!f.AccountId.HasValue || x.AccountId == f.AccountId)
            && (f.Mode == "bank" || f.Kind == "bank" || !f.MovementType.HasValue || x.MovementType == f.MovementType)
            && (!f.CashBoxId.HasValue || x.CashBoxId == f.CashBoxId) && (!f.CurrencyId.HasValue || x.CurrencyId == f.CurrencyId) && (f.Activity == "all" || x.IsActive == (f.Activity == "active"))
            && (f.Mode is not ("cash" or "shifts" or "differences" or "boxes") || x.Kind == "cash") && (f.Mode != "bank" || x.Kind == "bank")).ToArray();
        var ids = selected.Select(x => x.AccountId).ToArray();
        string CurrencyCode(Guid? id) => currencies.FirstOrDefault(x => x.Id == id)?.Code ?? "غير محددة";
        CashBankReportDto Result(string[] headers, IReadOnlyList<CashBankReportRow> rows, CashBankReportMetric[] metrics)
        {
            Limit(rows.Count, MaxRows);
            return new(baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces, accounts, currencies, headers, rows, metrics, warnings.Distinct().ToArray());
        }
        if (f.Mode == "transfers")
        {
            // Period membership is taken from posted journals, not the current document status.
            var allTransferAccounts = f.Kind == "all" && !f.AccountId.HasValue && !f.CashBoxId.HasValue && !f.CurrencyId.HasValue && f.Activity == "all" && !f.MovementType.HasValue;
            var transfers = await db.Set<CashTransfer>().AsNoTracking().Where(x => (allTransferAccounts || ids.Contains(x.SourceAccountId) || ids.Contains(x.DestinationAccountId))
                && ((x.TransferDate >= f.FromDate && x.TransferDate <= f.ToDate) || (x.ReversalDate >= f.FromDate && x.ReversalDate <= f.ToDate)))
                .Take(MaxRows + 1).ToListAsync(ct);
            Limit(transfers.Count, MaxRows);
            var journalIds = transfers.SelectMany(x => new[] { x.JournalEntryId, x.ReversalJournalEntryId }).OfType<Guid>().Distinct().ToArray();
            var journals = await db.Set<JournalEntry>().AsNoTracking().Where(x => journalIds.Contains(x.Id) && x.Status == JournalEntryStatus.Posted
                && x.PostingDate >= f.FromDate && x.PostingDate <= f.ToDate).ToDictionaryAsync(x => x.Id, ct);
            var events = new List<TransferEvent>();
            foreach (var t in transfers)
            {
                if (t.JournalEntryId is Guid j && journals.TryGetValue(j, out var original)) events.Add(new(t, original, false));
                else if (t.TransferDate >= f.FromDate && t.TransferDate <= f.ToDate && (t.Status is CashTransferStatus.Posted or CashTransferStatus.Reversed))
                    warnings.Add("يوجد تحويل مرحل بلا قيد أصلي مرحل ضمن تاريخ التحويل. راجع مستنداته؛ النتائج غير مكتملة.");
                if (t.ReversalJournalEntryId is Guid r && journals.TryGetValue(r, out var reversal)) events.Add(new(t, reversal, true));
                else if (t.ReversalDate >= f.FromDate && t.ReversalDate <= f.ToDate && t.Status == CashTransferStatus.Reversed)
                    warnings.Add("يوجد تحويل معكوس بلا قيد عكس مرحل ضمن تاريخ العكس. راجع مستنداته؛ النتائج غير مكتملة.");
            }
            Limit(events.Count, MaxRows);
            decimal? Value(TransferEvent e, decimal? value) => e.Journal.BaseCurrencyId == baseCurrency.Id ? (e.Reversal ? -value : value) : null;
            var rows = events.OrderBy(x => x.Journal.PostingDate).ThenBy(x => x.Journal.JournalNumber).Select(e =>
            {
                var t = e.Transfer; var sign = e.Reversal ? -1 : 1;
                return R(T(Date(e.Journal.PostingDate)), T(t.TransferNumber, t.Id, "transfer"), T(e.Reversal ? "عكس" : "تحويل"),
                    T(t.SourceName), T(t.SourceCurrencyCode), N(sign * t.SourceAmount), T(t.DestinationName), T(t.DestinationCurrencyCode),
                    N(sign * t.DestinationAmount), N(sign * t.FeeAmount), B(Value(e, t.SourceBaseAmount)), B(Value(e, t.DestinationBaseAmount)),
                    B(Value(e, t.FeeBaseAmount)), B(Value(e, t.FxDifferenceBase)), T(e.Journal.JournalNumber, e.Journal.Id, "journal"), T(t.Reference));
            }).ToArray();
            if (events.Any(e => e.Journal.BaseCurrencyId != baseCurrency.Id || !e.Transfer.SourceBaseAmount.HasValue || !e.Transfer.DestinationBaseAmount.HasValue || !e.Transfer.FeeBaseAmount.HasValue || !e.Transfer.FxDifferenceBase.HasValue))
                warnings.Add("بعض التحويلات لا تحتوي على قيم دفترية موثوقة بالعملة الأساسية الحالية؛ تظهر المبالغ المفقودة دون تقدير.");
            warnings.Add("التحويلات المرحلة فقط، والعكس يظهر بقيم سالبة في تاريخ ترحيله. الرسوم الأصلية بعملة المصدر. المجاميع تشمل كامل التحويل المطابق حتى عند اختيار أحد طرفيه.");
            var sent = Sum(events.Select(e => Value(e, e.Transfer.SourceBaseAmount)));
            var received = Sum(events.Select(e => Value(e, e.Transfer.DestinationBaseAmount)));
            return Result(["التاريخ", "رقم التحويل", "الحركة", "المصدر", "عملة المصدر", "إجمالي الصادر", "الوجهة", "عملة الوجهة", "المستلم", "الرسوم الأصلية", "الصادر الدفتري", "المستلم الدفتري", "الرسوم الدفترية", "فرق الصرف", "القيد", "المرجع"], rows,
                [M("عدد الحركات", C(events.Count)), M("صافي الصادر الدفتري", B(sent)), M("صافي المستلم الدفتري", B(received)),
                 M("صافي الرسوم", B(Sum(events.Select(e => Value(e, e.Transfer.FeeBaseAmount))))), M("صافي فرق الصرف", B(Sum(events.Select(e => Value(e, e.Transfer.FxDifferenceBase))))), M("صافي أثر التحويلات", B(received - sent))]);
        }
        if (f.Mode is "shifts" or "differences")
        {
            var cashIds = cash.Where(x => ids.Contains(x.AccountId)).Select(x => x.Id).ToArray();
            var fromUtc = f.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endUtc = f.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var q = db.Set<CashShift>().AsNoTracking().Where(x => cashIds.Contains(x.CashAccountId));
            // Only expose names of users who have opened a cash shift, not the identity directory.
            var actorIds = db.Set<CashShift>().AsNoTracking().Select(x => x.OpenedBy).Distinct();
            var operators = await db.Set<UserAccount>().AsNoTracking().Where(x => actorIds.Contains(x.Id)).OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.UserName)
                .Select(x => new CashBankReportOperatorDto(x.Id, x.FirstName + " " + x.LastName + " (" + x.UserName + ")")).ToListAsync(ct);
            var operatorNames = operators.ToDictionary(x => x.Id, x => x.Name);
            if (f.OpenedBy.HasValue) q = q.Where(x => x.OpenedBy == f.OpenedBy.Value);
            q = f.Mode == "shifts" ? q.Where(x => x.OpenedAtUtc >= fromUtc && x.OpenedAtUtc < endUtc)
                : q.Where(x => x.ClosedAtUtc >= fromUtc && x.ClosedAtUtc < endUtc && x.DifferenceAmount.HasValue && x.DifferenceAmount != 0);
            if (f.ShiftState != "all") { var state = Enum.Parse<CashShiftStatus>(f.ShiftState, true); q = q.Where(x => x.Status == state); }
            if (f.Mode == "differences" && f.SettlementState != "all")
            {
                var settled = f.SettlementState == "settled";
                q = q.Where(x => (x.SettlementJournalEntryId != null && x.SettlementDate != null && x.SettlementDate <= f.ToDate) == settled);
            }
            var shifts = await q.OrderBy(x => x.OpenedAtUtc).ThenBy(x => x.ShiftNumber).Take(MaxRows + 1).ToListAsync(ct);
            Limit(shifts.Count, MaxRows);
            var lookup = cash.ToDictionary(x => x.Id);
            bool Settled(CashShift s) => s.SettlementJournalEntryId.HasValue && s.SettlementDate is DateOnly date && date <= f.ToDate;
            string Status(CashShift s) => s.Status switch { CashShiftStatus.Open => "مفتوحة", CashShiftStatus.Closing => "قيد الإغلاق", CashShiftStatus.Closed => "مغلقة", CashShiftStatus.Approved => "معتمدة", _ => "غير معروفة" };
            string Settlement(CashShift s) => s.DifferenceAmount is null or 0 ? "—" : Settled(s) ? "مسوّاة" : "غير مسوّاة";
            var linkedIds = shifts.Select(x => x.Id).ToArray();
            var movementCounts = await (from l in db.Set<JournalEntryLine>().AsNoTracking()
                join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
                where l.CashShiftId.HasValue && linkedIds.Contains(l.CashShiftId.Value) && j.Status == JournalEntryStatus.Posted
                group l by l.CashShiftId.Value into g select new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
            var rows = shifts.Select(s =>
            {
                var a = lookup[s.CashAccountId];
                var cells = new List<CashBankReportCell> { T(s.ShiftNumber, s.Id, "shift"), T(s.CashBoxShiftId?.ToString("N")), T(a.CashBox?.Name ?? a.Name), T(CurrencyCode(a.CurrencyId)), T(DateTimeText(s.OpenedAtUtc)), T(DateTimeText(s.ClosedAtUtc)), T(operatorNames.GetValueOrDefault(s.OpenedBy) ?? s.OpenedBy.ToString()), T(Status(s)) };
                if (f.Mode == "shifts") cells.AddRange([N(s.BookOpeningBalance), N(s.OpeningBalance), N(s.BookOpeningBalance.HasValue ? s.OpeningBalance - s.BookOpeningBalance : null), T(s.OpeningDifferenceReason), s.UsesLinkedMovements ? C(movementCounts.GetValueOrDefault(s.Id)) : T("تاريخية")]);
                cells.AddRange([N(s.ExpectedClosingBalance), N(s.ActualClosingBalance), N(s.DifferenceAmount), T(Settlement(s)), T(Settled(s) ? Date(s.SettlementDate!.Value) : null), T(Settled(s) ? "قيد التسوية" : null, Settled(s) ? s.SettlementJournalEntryId : null, "journal")]);
                return new CashBankReportRow(cells);
            }).ToArray();
            var headers = new List<string> { "رقم الوردية", "معرف الوردية المجمعة", "الصندوق", "العملة", "الافتتاح UTC", "الإغلاق UTC", "فتح بواسطة", "الحالة الحالية" };
            if (f.Mode == "shifts") headers.AddRange(["الدفتري عند الافتتاح", "الجرد الافتتاحي", "فرق الافتتاح", "سبب الفرق", "الحركات المرتبطة"]);
            headers.AddRange(["المتوقع", "الجرد الفعلي", "فرق الإغلاق", "التسوية حتى التاريخ", "تاريخ التسوية", "قيد التسوية"]);
            warnings.Add("تواريخ الورديات بالتوقيت العالمي UTC. الورديات حسب تاريخ الافتتاح، والفروقات حسب تاريخ الإغلاق؛ حالة الوردية والجرد كما هو مسجل حالياً. التسوية حتى تاريخ نهاية التقرير.");
            if (shifts.Any(s => !s.UsesLinkedMovements)) warnings.Add("الورديات التاريخية تعرض القيم المسجلة فقط، ولا يعاد إسناد حركاتها بالتوقيت.");
            var currencyIds = shifts.Select(s => lookup[s.CashAccountId].CurrencyId).Distinct().ToArray();
            var single = currencyIds.Length == 1 && currencyIds[0].HasValue;
            var unit = single ? " (" + CurrencyCode(currencyIds[0]) + ")" : "";
            var differences = shifts.Where(s => s.DifferenceAmount is not null and not 0).ToArray();
            if (!single && shifts.Count > 0) warnings.Add("لا تجمع فروقات الورديات بعملات مختلفة أو غير محددة. اختر عملة واحدة لعرض إجماليات المبالغ.");
            return Result(headers.ToArray(), rows, [M("عدد الورديات", C(shifts.Select(s=>s.CashBoxShiftId??s.Id).Distinct().Count())), M("ورديات مفتوحة أو قيد الإغلاق", C(shifts.Where(s => s.Status is CashShiftStatus.Open or CashShiftStatus.Closing).Select(s=>s.CashBoxShiftId??s.Id).Distinct().Count())),
                M("فروقات مسوّاة", C(differences.Count(Settled))), M("فروقات غير مسوّاة", C(differences.Count(s => !Settled(s)))),
                M("إجمالي العجز" + unit, N(single ? differences.Where(s => s.DifferenceAmount < 0).Sum(s => -s.DifferenceAmount) : null)),
                M("إجمالي الزيادة" + unit, N(single ? differences.Where(s => s.DifferenceAmount > 0).Sum(s => s.DifferenceAmount) : null))]) with { Operators = operators };
        }
        var lines = await (from l in db.Set<JournalEntryLine>().AsNoTracking()
            join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
            where ids.Contains(l.AccountId) && j.Status == JournalEntryStatus.Posted && j.PostingDate <= f.ToDate
            select new Line(l.AccountId, j.Id, j.JournalNumber, j.PostingDate, j.PostedAtUtc, l.LineNumber, j.JournalType,
                j.SourceDocumentType, l.Description ?? j.Description, j.BaseCurrencyId, l.TransactionCurrencyId,
                l.TransactionDebitAmount, l.TransactionCreditAmount, l.DebitAmount, l.CreditAmount, l.CashShiftId)).Take(MaxLedger + 1).ToListAsync(ct);
        Limit(lines.Count, MaxLedger);
        var byAccount = lines.ToLookup(x => x.AccountId);
        (decimal? Debit, decimal? Credit) Native(CashBankReportAccountDto a, Line l) => CashBankReportMath.Native(a.CurrencyId, l.BaseId, l.TxId, l.TxDebit, l.TxCredit, l.Debit, l.Credit);
        // Legacy journals without a base snapshot still store debit/credit in ledger units; disclose that assumption.
        decimal? Base(Line l, bool debit) => l.BaseId.HasValue && l.BaseId != baseCurrency.Id ? null : debit ? l.Debit : l.Credit;
        if (lines.Any(l => l.BaseId is null)) warnings.Add("توجد قيود قديمة بلا لقطة للعملة الأساسية؛ استُخدمت مبالغ المدين والدائن الدفترية باعتبارها بالعملة الأساسية الحالية.");
        if (lines.Any(l => l.BaseId.HasValue && l.BaseId != baseCurrency.Id)) warnings.Add("توجد قيود بعملة أساسية تاريخية مختلفة؛ الإجماليات الدفترية المتأثرة غير متاحة.");
        var balances = selected.Select(a =>
        {
            var previous = byAccount[a.AccountId].Where(l => l.Date < f.FromDate).ToArray();
            var period = byAccount[a.AccountId].Where(l => l.Date >= f.FromDate).ToArray();
            decimal? NativeSum(IEnumerable<Line> source, Func<(decimal? Debit, decimal? Credit), decimal?> selector) => a.CurrencyId.HasValue ? Sum(source.Select(l => selector(Native(a, l)))) : null;
            return new Balance(a, NativeSum(previous, n => n.Debit - n.Credit), NativeSum(period, n => n.Debit), NativeSum(period, n => n.Credit),
                Sum(previous.Select(l => Base(l, true) - Base(l, false))), Sum(period.Select(l => Base(l, true))), Sum(period.Select(l => Base(l, false))));
        }).ToArray();
        if (balances.Any(b => b.Close is null)) warnings.Add("بعض الحسابات بلا عملة أو بلا مبالغ أصلية موثوقة في القيود؛ يظهر الرصيد الأصلي غير متاح، ولا يعاد احتسابه بسعر الصرف الحالي.");
        var metrics = new[] { M("الرصيد الافتتاحي الدفتري", B(Sum(balances.Select(b => b.BaseOpen)))), M("إجمالي الوارد الدفتري", B(Sum(balances.Select(b => b.BaseIn)))), M("إجمالي الصادر الدفتري", B(Sum(balances.Select(b => b.BaseOut)))),
            M("أرصدة الصناديق", B(Sum(balances.Where(b => b.Account.Kind == "cash").Select(b => b.BaseClose)))), M("أرصدة البنوك", B(Sum(balances.Where(b => b.Account.Kind == "bank").Select(b => b.BaseClose)))), M("إجمالي السيولة الدفترية", B(Sum(balances.Select(b => b.BaseClose)))) };
        warnings.Add("الوارد والصادر حركات دفترية إجمالية تشمل التحويلات الداخلية والأرصدة الافتتاحية المرحلة خلال الفترة؛ لا تمثل إيرادات أو مصروفات. لا تجمع العملات الأصلية المختلفة.");
        if (f.Mode is "cash" or "bank")
        {
            var rows = new List<CashBankReportRow>();
            foreach (var b in balances)
            {
                var a = b.Account; var running = b.Open; var runningBase = b.BaseOpen;
                foreach (var l in byAccount[a.AccountId].Where(l => l.Date >= f.FromDate).OrderBy(l => l.Date).ThenBy(l => l.PostedAt).ThenBy(l => l.JournalId).ThenBy(l => l.LineNumber))
                {
                    var n = Native(a, l); running += n.Debit - n.Credit; runningBase += Base(l, true) - Base(l, false);
                    var type = l.SourceType switch { "CashTransfer" => "تحويل داخلي", "CashTransferReversal" => "عكس تحويل", _ => l.Type switch { JournalType.Opening => "افتتاحي", JournalType.Reversal => "عكسي", JournalType.Closing => "إقفال", _ => "حركة أخرى" } };
                    rows.Add(R(T(a.Name, a.AccountId, "account"), T(CurrencyCode(a.CurrencyId)), T(Date(l.Date)), T(l.Number, l.JournalId, "journal"), T(type), T(l.Description), N(n.Debit), N(n.Credit), N(running), B(Base(l, true)), B(Base(l, false)), B(runningBase), T(l.ShiftId.HasValue ? "عرض الوردية" : null, l.ShiftId, "shift")));
                    Limit(rows.Count, MaxRows);
                }
            }
            return Result(["الحساب", "العملة", "التاريخ", "القيد", "نوع الحركة", "البيان", "وارد أصلي", "صادر أصلي", "الرصيد الأصلي", "وارد دفتري", "صادر دفتري", "الرصيد الدفتري", "الوردية"], rows, metrics);
        }
        var singleCurrency = balances.Select(b => b.Account.CurrencyId).Distinct().ToArray();
        var nativeTotals = singleCurrency.Length == 1 && singleCurrency[0].HasValue;
        decimal? NativeTotal(Func<Balance, decimal?> value) => nativeTotals ? Sum(balances.Select(value)) : null;
        CashBankReportCell[] BalanceTotals() => [N(NativeTotal(b => b.Open)), N(NativeTotal(b => b.In)), N(NativeTotal(b => b.Out)), N(NativeTotal(b => b.Close)),
            B(Sum(balances.Select(b => b.BaseOpen))), B(Sum(balances.Select(b => b.BaseIn))), B(Sum(balances.Select(b => b.BaseOut))), B(Sum(balances.Select(b => b.BaseClose)))];
        var display = balances.Where(b => f.IncludeZero || b.Close != 0 || b.BaseOpen != 0 || b.BaseIn != 0 || b.BaseOut != 0).ToArray();
        if (f.Mode == "boxes")
        {
            var rows = display.Where(b=>b.Account.CashBoxId.HasValue).GroupBy(b=>b.Account.CashBoxId).OrderBy(g=>g.First().Account.Name)
                .Select(g=>R(T(g.First().Account.Code), T(g.First().Account.CashBoxName ?? g.First().Account.Name), C(g.Count()),
                    B(Sum(g.Select(b=>b.BaseOpen))), B(Sum(g.Select(b=>b.BaseIn))), B(Sum(g.Select(b=>b.BaseOut))), B(Sum(g.Select(b=>b.BaseClose))))).ToArray();
            warnings.Add("ملخص الصندوق التشغيلي بالعملة الدفترية فقط؛ اعرض الأرصدة الأصلية في تقرير إجمالي الأرصدة مع اختيار الصندوق.");
            return Result(["الرمز", "الصندوق التشغيلي", "عدد حسابات العملات المعروضة", "افتتاحي دفتري", "وارد دفتري", "صادر دفتري", "ختامي دفتري"], rows, metrics) with
            { TotalRow = R(T("الإجمالي"), T(null), C(display.Length), B(Sum(balances.Select(b=>b.BaseOpen))), B(Sum(balances.Select(b=>b.BaseIn))), B(Sum(balances.Select(b=>b.BaseOut))), B(Sum(balances.Select(b=>b.BaseClose)))) };
        }
        if (f.Mode == "currency")
        {
            var rows = display.GroupBy(b => b.Account.CurrencyId).OrderBy(g => CurrencyCode(g.Key)).Select(g => R(T(CurrencyCode(g.Key)), C(g.Count()), N(Sum(g.Select(b => b.Open))), N(Sum(g.Select(b => b.In))), N(Sum(g.Select(b => b.Out))), N(Sum(g.Select(b => b.Close))), B(Sum(g.Select(b => b.BaseOpen))), B(Sum(g.Select(b => b.BaseIn))), B(Sum(g.Select(b => b.BaseOut))), B(Sum(g.Select(b => b.BaseClose))))).ToArray();
            return Result(["العملة", "عدد الحسابات المعروضة", "افتتاحي أصلي", "وارد أصلي", "صادر أصلي", "ختامي أصلي", "افتتاحي دفتري", "وارد دفتري", "صادر دفتري", "ختامي دفتري"], rows, metrics) with { TotalRow = new CashBankReportRow([T("الإجمالي"), C(display.Length), .. BalanceTotals()]) };
        }
        return Result(["الرمز", "الصندوق / البنك", "النوع", "العملة", "افتتاحي أصلي", "وارد أصلي", "صادر أصلي", "ختامي أصلي", "افتتاحي دفتري", "وارد دفتري", "صادر دفتري", "ختامي دفتري"],
            display.Select(b => R(T(b.Account.Code), T(b.Account.Name, b.Account.AccountId, "account"), T(Kind(b.Account.Kind)), T(CurrencyCode(b.Account.CurrencyId)), N(b.Open), N(b.In), N(b.Out), N(b.Close), B(b.BaseOpen), B(b.BaseIn), B(b.BaseOut), B(b.BaseClose))).ToArray(), metrics) with { TotalRow = new CashBankReportRow([T("الإجمالي"), T(null), T(null), T(nativeTotals ? CurrencyCode(singleCurrency[0]) : "عملات متعددة"), .. BalanceTotals()]) };
    }
}
