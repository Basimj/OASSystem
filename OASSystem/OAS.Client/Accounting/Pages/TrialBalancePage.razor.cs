using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Services;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.UiLib.Core.Models;
using Amounts = OAS.Client.Accounting.Pages.TrialBalancePresentation.Amounts;

namespace OAS.Client.Accounting.Pages;

public partial class TrialBalancePage
{
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private sealed record Filters
    {
        public string Year { get; set; } = "";
        public string Period { get; set; } = "";
        public DateTime From { get; set; } = new(DateTime.Today.Year, 1, 1);
        public DateTime To { get; set; } = DateTime.Today;
        public string Account { get; set; } = "";
        public int Class { get; set; }
        public int Level { get; set; }
        public string Mode { get; set; } = "all";
        public string View { get; set; } = "posting";
        public Guid? Currency { get; set; }
        public string Balance { get; set; } = "all";
        public bool Children { get; set; } = true;
        public bool IncludeZero { get; set; }
        public bool HideClosingZero { get; set; }
    }
    private sealed record Group(string Label, string Debit, string Credit);
    private sealed record Col(string Key, string Label, string Css);
    private sealed record Row(TrialBalanceRowDto Account, Amounts Amounts, bool Aggregate, int Depth);
    private static readonly Group[] Comprehensive = [new("الرصيد الافتتاحي", "od", "oc"), new("حركة الفترة", "pd", "pc"), new("الرصيد الختامي", "cd", "cc")];
    private Group[] Groups => _applied.Mode switch
    {
        "balances" => [Comprehensive[2]], "movement" => [Comprehensive[1]],
        "gross" => [new("إجمالي الحركات حتى نهاية الفترة", "gd", "gc")], _ => Comprehensive
    };
    private Col[] Columns => Groups.SelectMany(g => new[] { new Col(g.Debit, "مدين", "oas-report__debit"), new Col(g.Credit, "دائن", "oas-report__credit") }).ToArray();
    private int ColumnCount => 2 + Columns.Length;
    private Filters _draft = new(), _applied = new();
    private IReadOnlyList<AccountDto> _accounts = [];
    private IReadOnlyList<FiscalYearDto> _years = [];
    private IReadOnlyList<FiscalPeriodDto> _periods = [];
    private IReadOnlyList<CurrencyDto> _currencies = [];
    private Dictionary<Guid, AccountDto> _meta = new();
    private Dictionary<Guid, TrialBalanceRowDto> _direct = new();
    private TrialBalanceReportDto? _report;
    private IReadOnlyList<Row> _visible = [], _exportRows = [];
    private Amounts _totals = TrialBalancePresentation.SumDirect([]);
    private Guid? _baseId;
    private string _baseName = "العملة الأساسية", _currencyName = "العملة الأساسية";
    private decimal _rate = 1m;
    private bool _busy = true, _referencesLoaded, _filtersOpen = true, _descending, _collapsed;
    private string? _error, _validation;
    private string _search = "", _sortKey = "code";
    private int _page = 1, _pageSize = 25;
    private bool IsTree => _applied.View != "posting";
    private bool CanExport => !_busy && _error is null && _exportRows.Count > 0;
    private int TotalPages => Math.Max(1, (_visible.Count + _pageSize - 1) / _pageSize);
    private IEnumerable<Row> PageRows => _visible.Skip((_page - 1) * _pageSize).Take(_pageSize);
    private string Status => _busy ? "جارٍ التحميل" : _error is not null || _report is null ? "غير متاح" : _report.IsBalanced ? "الميزان العام متوازن" : "الميزان العام غير متوازن";
    private string StatusIcon => _busy ? "fa-solid fa-hourglass-half" : _error is not null || _report is null || !_report.IsBalanced ? "fa-solid fa-circle-exclamation" : "fa-solid fa-circle-check";
    private string StatusClass => !_busy && _error is null && _report is { IsBalanced: false } ? "trial-unbalanced" : _busy || _error is not null ? "trial-pending" : "";
    private IEnumerable<FiscalPeriodDto> PeriodOptions => _periods.Where(p => string.IsNullOrEmpty(_draft.Year) || p.FiscalYearId.ToString() == _draft.Year).OrderBy(p => p.StartDate);
    private int[] Levels => _accounts.Select(a => (int)a.Level).Distinct().Order().ToArray();
    private IReadOnlyList<UiSelectOption> AccountOptions => new[] { new UiSelectOption("", "جميع الحسابات") }.Concat(_accounts.OrderBy(a => a.Code).Select(a => new UiSelectOption(a.Id.ToString(), $"{a.Code} - {a.NameAr}"))).ToArray();
    private IReadOnlyList<(string Label, string Value)> Chips
    {
        get
        {
            var chips = new List<(string Label, string Value)>
            {
                ("النوع", ModeName(_applied.Mode)), ("العرض", ViewName(_applied.View)),
                ("الحساب", _accounts.FirstOrDefault(a => a.Id.ToString() == _applied.Account) is { } a ? $"{a.Code} - {a.NameAr}" : "الكل"),
                ("الفترة", $"{_applied.From:dd/MM/yyyy} – {_applied.To:dd/MM/yyyy}"), ("العملة", _currencyName),
                ("التصنيف", ClassName(_applied.Class)), ("المستوى", _applied.View == "roots" ? "رئيسي" : _applied.Level == 0 ? "الكل" : $"حتى {_applied.Level}")
            };
            if (_applied.Balance != "all") chips.Add(("الرصيد", BalanceName(_applied.Balance)));
            if (!string.IsNullOrEmpty(_applied.Year)) chips.Add(("السنة", _years.FirstOrDefault(y => y.Id.ToString() == _applied.Year)?.Name ?? "—"));
            if (!string.IsNullOrEmpty(_applied.Period)) chips.Add(("الفترة المالية", _periods.FirstOrDefault(p => p.Id.ToString() == _applied.Period)?.Name ?? "—"));
            if (!string.IsNullOrEmpty(_applied.Account)) chips.Add(("الفرعية", _applied.Children ? "نعم" : "لا"));
            if (_applied.IncludeZero) chips.Add(("دون حركة", "تضمين"));
            if (_applied.HideClosingZero) chips.Add(("ختامي صفري", "إخفاء"));
            return chips;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _busy = false;
        await ApplyAsync();
    }
    private static async Task<IReadOnlyList<T>> LoadAll<T>(Func<PageRequest, Task<PagedResult<T>>> loader)
    {
        var list = new List<T>();
        for (var page = 1; ; page++)
        {
            var result = await loader(new PageRequest { PageNumber = page, PageSize = 100 });
            list.AddRange(result.Items);
            if (!result.HasNextPage) return list;
        }
    }
    private async Task LoadReferencesAsync()
    {
        _accounts = await LoadAll(p => Accounting.GetAccountsPageAsync(p));
        _meta = _accounts.ToDictionary(a => a.Id);
        _years = (await LoadAll(p => Accounting.GetFiscalYearsPageAsync(p))).OrderByDescending(y => y.StartDate).ToArray();
        _periods = await LoadAll(p => Accounting.GetFiscalPeriodsPageAsync(p));
        _currencies = await LoadAll(p => Accounting.GetCurrenciesPageAsync(p));
        var settings = await Accounting.GetAccountingSettingsAsync();
        _baseId = settings?.BaseCurrencyId;
        _baseName = _currencies.FirstOrDefault(c => c.Id == _baseId)?.NameAr ?? settings?.BaseCurrencyCode ?? "العملة الأساسية";
        _referencesLoaded = true;
    }
    private void OnAccount(string? value) => _draft.Account = value ?? "";
    private void OnYear(ChangeEventArgs e)
    {
        _draft.Year = e.Value?.ToString() ?? "";
        _draft.Period = "";
        SetFiscalDates();
    }
    private void OnPeriod(ChangeEventArgs e)
    {
        _draft.Period = e.Value?.ToString() ?? "";
        SetFiscalDates();
    }
    private void SetFiscalDates()
    {
        var range = OAS.Client.Accounting.Common.FiscalReportRange.Resolve(_years, _periods, _draft.Year, _draft.Period);
        if (range.HasValue) (_draft.From, _draft.To) = range.Value;
    }
    private static Guid? Id(string value) => Guid.TryParse(value, out var id) ? id : null;
    private async Task ResetAsync()
    {
        if (_busy) return;
        _draft = new(); _search = ""; _sortKey = "code"; _descending = false;
        await ApplyAsync();
    }
    private async Task ApplyAsync()
    {
        if (_busy) return;
        _validation = null;
        var next = _draft with { };
        if (next.From.Date > next.To.Date) { _validation = "تاريخ البداية يجب ألا يتجاوز تاريخ النهاية."; return; }
        if (next.Balance == "zero") next.HideClosingZero = false;
        _busy = true; _error = null;
        try
        {
            if (!_referencesLoaded) await LoadReferencesAsync();
            // All direct account balances are computed in SQL; do not request server rollups.
            var report = await Accounting.GetTrialBalanceAsync(new TrialBalanceFilterDto(
                DateOnly.FromDateTime(next.From), DateOnly.FromDateTime(next.To), FiscalYearId: Id(next.Year), FiscalPeriodId: Id(next.Period), IncludeZeroBalance: true))
                ?? throw new InvalidOperationException("لم يُرجع الخادم بيانات ميزان المراجعة.");
            if (report.Rows.Any(r => r.IsAggregate)) throw new InvalidOperationException("تعذر الحصول على الأرصدة المباشرة للحسابات.");
            if (next.Mode == "gross" && report.Rows.Any(r => r.PreviousDebitTotal is null || r.PreviousCreditTotal is null))
                throw new InvalidOperationException("حدّث خدمة ميزان المراجعة في الخادم لدعم العرض بالمجاميع.");
            var rate = 1m;
            var currencyName = _baseName;
            if (next.Currency is Guid currencyId && currencyId != _baseId)
            {
                var currency = _currencies.FirstOrDefault(c => c.Id == currencyId && c.IsActive) ?? throw new InvalidOperationException("العملة غير متاحة.");
                var effective = await Accounting.GetEffectiveExchangeRateAsync(currencyId, report.ToDate, ExchangeRateType.Accounting);
                if (effective is null || effective.Rate <= 0m) throw new InvalidOperationException("لا يوجد سعر صرف محاسبي صالح حتى نهاية الفترة.");
                rate = effective.Rate; currencyName = $"{currency.NameAr} ({currency.Code})";
            }
            next.From = report.FromDate.ToDateTime(TimeOnly.MinValue); next.To = report.ToDate.ToDateTime(TimeOnly.MinValue);
            _report = report; _direct = report.Rows.ToDictionary(r => r.AccountId);
            _applied = next; _rate = rate; _currencyName = currencyName; _collapsed = false;
            if (_sortKey != "code" && _sortKey != "name" && !Columns.Any(c => c.Key == _sortKey)) { _sortKey = "code"; _descending = false; }
            BuildRows();
        }
        catch (Exception ex)
        {
            _error = "تعذر تحميل ميزان المراجعة: " + ex.Message;
            _report = null; _direct.Clear(); _visible = []; _exportRows = []; _totals = TrialBalancePresentation.SumDirect([]); _page = 1;
        }
        finally { _busy = false; }
    }

    // Walk ancestors once for each account; a visited set also prevents malformed cycles.
    private IEnumerable<TrialBalanceRowDto> Path(TrialBalanceRowDto row)
    {
        var seen = new HashSet<Guid>();
        var current = row;
        while (seen.Add(current.AccountId))
        {
            yield return current;
            if (current.ParentAccountId is not Guid parent || !_direct.TryGetValue(parent, out var next)) yield break;
            current = next;
        }
    }
    private bool InAccountScope(TrialBalanceRowDto r, Guid? selected) => selected is null || r.AccountId == selected || (_applied.Children && Path(r).Any(a => a.AccountId == selected));
    private bool InClass(TrialBalanceRowDto r) => _applied.Class == 0 || (int)r.AccountClass == _applied.Class;
    private bool MatchesText(TrialBalanceRowDto r) => string.IsNullOrWhiteSpace(_search) || Normalize($"{r.AccountCode} {r.AccountNameAr} {r.AccountNameEn}").Contains(Normalize(_search.Trim()), StringComparison.CurrentCultureIgnoreCase);
    private static string Normalize(string value) => new(value.Select(c => c is >= '\u0660' and <= '\u0669' ? (char)('0' + c - '\u0660') : c is >= '\u06f0' and <= '\u06f9' ? (char)('0' + c - '\u06f0') : c).ToArray());
    private bool MatchesBalance(TrialBalanceRowDto r) => _applied.Balance switch { "debit" => r.ClosingDebit > 0, "credit" => r.ClosingCredit > 0, "zero" => r.ClosingDebit == 0 && r.ClosingCredit == 0, _ => true };
    private bool HasActivity(TrialBalanceRowDto r) => r.OpeningDebit != 0 || r.OpeningCredit != 0 || r.PeriodDebit != 0 || r.PeriodCredit != 0 || (_applied.Mode == "gross" && (r.PreviousDebitTotal.GetValueOrDefault() != 0 || r.PreviousCreditTotal.GetValueOrDefault() != 0));
    private void BuildRows()
    {
        var selected = Id(_applied.Account);
        bool Scope(TrialBalanceRowDto r) => InAccountScope(r, selected) && InClass(r);
        var source = _direct.Values.Where(r => Scope(r)
            && (IsTree || (_meta.TryGetValue(r.AccountId, out var a) ? a.IsPostingAccount : r.AccountType == AccountType.Posting))
            && (IsTree || _applied.Level == 0 || r.Level <= _applied.Level)
            && (_applied.IncludeZero || HasActivity(r))
            && (!_applied.HideClosingZero || r.ClosingDebit != 0 || r.ClosingCredit != 0)
            && MatchesBalance(r)
            && (MatchesText(r) || (IsTree && Path(r).Any(a => Scope(a) && MatchesText(a)))))
            .ToArray();
        _totals = TrialBalancePresentation.SumDirect(source);
        if (!IsTree)
        {
            _exportRows = Sorted(source.Select(r => new Row(r, TrialBalancePresentation.SumDirect([r]), false, 0))).ToArray();
            _visible = _exportRows; _page = 1; return;
        }
        // Each direct account contributes once to the totals; only presentation rows roll up.
        var buckets = new Dictionary<Guid, List<TrialBalanceRowDto>>();
        var parentMap = new Dictionary<Guid, Guid?>();
        foreach (var r in source)
        {
            var path = Path(r).Where(Scope).Reverse().ToArray();
            if (_applied.View == "roots") path = path.Take(1).ToArray();
            else if (_applied.Level > 0) path = path.Where(a => a.Level <= _applied.Level).ToArray();
            // When the chosen subtree starts below the requested depth, preserve its root.
            if (path.Length == 0) path = Path(r).Where(Scope).Reverse().Take(1).ToArray();
            Guid? parent = null;
            foreach (var a in path)
            {
                if (!buckets.TryGetValue(a.AccountId, out var items)) buckets[a.AccountId] = items = new();
                items.Add(r); parentMap[a.AccountId] = parent; parent = a.AccountId;
            }
        }
        var nodes = buckets.Select(kv => new Row(_direct[kv.Key], TrialBalancePresentation.SumDirect(kv.Value, true), kv.Value.Any(r => r.AccountId != kv.Key), 0)).ToArray();
        var children = nodes.Where(n => parentMap[n.Account.AccountId].HasValue).ToLookup(n => parentMap[n.Account.AccountId]!.Value);
        var ordered = new List<Row>();
        var visited = new HashSet<Guid>();
        void Walk(Row node, int depth)
        {
            if (!visited.Add(node.Account.AccountId)) return;
            ordered.Add(node with { Depth = depth });
            foreach (var child in Sorted(children[node.Account.AccountId])) Walk(child, depth + 1);
        }
        foreach (var root in Sorted(nodes.Where(n => parentMap[n.Account.AccountId] is null))) Walk(root, 0);
        foreach (var node in Sorted(nodes)) if (!visited.Contains(node.Account.AccountId)) Walk(node, 0);
        _exportRows = ordered;
        _visible = _collapsed ? ordered.Where(r => r.Depth == 0).ToArray() : ordered;
        _page = 1;
    }
    private IEnumerable<Row> Sorted(IEnumerable<Row> rows)
    {
        Func<Row, object?> key = r => _sortKey switch { "code" => r.Account.AccountCode, "name" => r.Account.AccountNameAr, _ => r.Amounts.Value(_sortKey) };
        return (_descending ? rows.OrderByDescending(key) : rows.OrderBy(key)).ThenBy(r => r.Account.AccountCode, StringComparer.Ordinal);
    }
    private void OnSearch(string value) { _search = value; if (!_busy && _error is null) BuildRows(); }
    private void SortBy(string key) { if (_busy || _error is not null) return; _descending = _sortKey == key && !_descending; _sortKey = key; BuildRows(); }
    private void ToggleTree() { _collapsed = !_collapsed; _visible = _collapsed ? _exportRows.Where(r => r.Depth == 0).ToArray() : _exportRows; _page = 1; }
    private void GoPage(int page) => _page = Math.Clamp(page, 1, TotalPages);
    private void ChangePageSize(ChangeEventArgs e) { if (int.TryParse(e.Value?.ToString(), out var n) && n is 10 or 25 or 50 or 100) _pageSize = n; _page = 1; }
    private string Money(decimal? value, bool dash = true) => value is null || (dash && value.Value == 0) ? "—" : (value.Value / _rate).ToString("N2", CultureInfo.InvariantCulture);
    private string Metric(decimal value) => _busy || _error is not null || _report is null ? "—" : Money(value, false);
    private void OpenStatement(Row row) => Nav.NavigateTo($"/accounting/reports/account-statement?accountId={row.Account.AccountId:D}&fromDate={_applied.From:yyyy-MM-dd}&toDate={_applied.To:yyyy-MM-dd}&includeChildren={row.Aggregate.ToString().ToLowerInvariant()}{(_applied.Currency is Guid currency ? $"&currencyId={currency:D}" : "")}");
    private string Subtitle => $"{Status} | {string.Join(" | ", Chips.Select(c => $"{c.Label}: {c.Value}"))} | البحث: {(string.IsNullOrWhiteSpace(_search) ? "الكل" : _search)} | الإجماليات من الحسابات المباشرة دون تكرار التجميع.";
    private string[] ExportHeaders => new[] { "رقم الحساب", "اسم الحساب" }.Concat(Groups.SelectMany(g => new[] { g.Label + " - مدين", g.Label + " - دائن" })).ToArray();
    private object? ExportValue(decimal? value) => value.HasValue ? value.Value / _rate : null;
    private object?[] ExportRow(Row row) => new object?[] { row.Account.AccountCode, new string(' ', Math.Min(row.Depth, 12) * 3) + row.Account.AccountNameAr }.Concat(Columns.Select(c => ExportValue(row.Amounts.Value(c.Key)))).ToArray();
    private async Task ExportAsync()
    {
        if (!CanExport) return;
        try
        {
            var totals = new object?[] { "", "إجمالي النتائج" }.Concat(Columns.Select(c => ExportValue(_totals.Value(c.Key)))).ToArray();
            var bytes = CustomerDebtsWorkbook.Create(Subtitle, ExportHeaders, _exportRows.Select(ExportRow).Append(totals), sheetName: "ميزان المراجعة", pairedHeaderGroups: Groups.Select(g => g.Label).ToArray());
            await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/customer-debts-export.js");
            await module.InvokeVoidAsync("saveWorkbook", Convert.ToBase64String(bytes), "trial-balance.xlsx");
        }
        catch (Exception ex) { _validation = "تعذر التصدير: " + ex.Message; }
    }
    private async Task PrintAsync()
    {
        if (!CanExport) return;
        try
        {
            var rows = _exportRows.Select(r => new[] { r.Account.AccountCode, new string('\u00a0', Math.Min(r.Depth, 12)*3) + r.Account.AccountNameAr }.Concat(Columns.Select(c => Money(r.Amounts.Value(c.Key)))).ToArray()).ToArray();
            await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/trial-balance-print.js");
            await module.InvokeVoidAsync("printTrialBalance", Subtitle, Groups.Select(g => g.Label).ToArray(), rows, Columns.Select(c => Money(_totals.Value(c.Key), false)).ToArray(), _exportRows.Select(r => r.Aggregate).ToArray());
        }
        catch (Exception ex) { _validation = "تعذرت الطباعة: " + ex.Message; }
    }
    private static string ModeName(string value) => value switch { "balances" => "بالأرصدة", "gross" => "بالمجاميع", "movement" => "بالحركة", _ => "شامل" };
    private static string ViewName(string value) => value switch { "tree" => "شجرة الحسابات", "roots" => "الحسابات الرئيسية", _ => "حسابات الترحيل" };
    private static string BalanceName(string value) => value switch { "debit" => "مدين", "credit" => "دائن", "zero" => "صفري", _ => "الكل" };
    private static string ClassName(int value) => value switch { 1 => "الأصول", 2 => "الالتزامات", 3 => "حقوق الملكية", 4 => "الإيرادات", 5 => "المصروفات", _ => "الكل" };
}
