using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Services;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;

namespace OAS.Client.Accounting.Pages;

public partial class BalanceSheetPage
{
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    private sealed record Filters
    {
        public string Year { get; set; } = "";
        public DateTime AsOf { get; set; } = DateTime.Today;
        public DateTime CompareAsOf { get; set; } = DateTime.Today.AddYears(-1);
        public string Mode { get; set; } = "side";
        public int Level { get; set; }
        public Guid? Currency { get; set; }
        public bool Children { get; set; } = true;
        public bool IncludeZero { get; set; }
    }
    private sealed record Row(string Code, string Name, decimal Current, decimal Previous, bool Summary,
        Guid? AccountId = null, int Depth = 0, bool Aggregate = false);
    private sealed record DisplayRow(Row? First, Row? Second = null);
    private sealed record Cell(string Text, object? Value, bool Summary = false, bool Amount = false,
        bool Negative = false, Guid? AccountId = null, int Depth = 0, string Css = "");
    private Filters _draft = new(), _applied = new();
    private IReadOnlyList<FiscalYearDto> _years = [];
    private IReadOnlyList<CurrencyDto> _currencies = [];
    private BalanceSheetReportDto? _report, _compare;
    private IReadOnlyList<Row> _assetRows = [], _rightRows = [];
    private IReadOnlyList<DisplayRow> _visible = [];
    private Guid? _baseId;
    private decimal _rate = 1m;
    private string _baseName = "العملة الأساسية", _currencyName = "العملة الأساسية";
    private string _search = "", _sort = "code";
    private bool _busy, _loaded, _filtersOpen = true, _descending;
    private string? _error, _validation;
    private int _page = 1, _pageSize = 25;
    private bool Side => _applied.Mode == "side";
    private bool Compare => _applied.Mode == "compare";
    private bool DraftSupportsDetails => _draft.Mode is "side" or "detail";
    private bool Details => (_applied.Mode is "side" or "detail") && _applied.Children;
    private bool CanExport => !_busy && _error is null && _report is not null;
    private bool Unclassified => _report?.Totals.HasUnclassifiedBalances == true || _compare?.Totals.HasUnclassifiedBalances == true;
    private bool Unbalanced => _report?.Totals.IsBalanced == false || _compare?.Totals.IsBalanced == false;
    private BalanceSheetTotalsDto? Totals => _report?.Totals;
    private string Status => _busy ? "جارٍ التحميل" : !CanExport ? "غير متاح" : Unbalanced ? "الميزانية غير متوازنة" : "الميزانية متوازنة";
    private string StatusIcon => Unbalanced || _error is not null ? "fa-solid fa-circle-exclamation" : "fa-solid fa-circle-check";
    private int TotalPages => Math.Max(1, (_visible.Count + _pageSize - 1) / _pageSize);
    private IEnumerable<DisplayRow> PageRows => _visible.Skip((_page - 1) * _pageSize).Take(_pageSize);
    private IEnumerable<int> Levels => (_report?.Accounts ?? []).Where(a => a.AccountClass is AccountClass.Asset or AccountClass.Liability or AccountClass.Equity).Select(a => (int)a.Level).Distinct().Order();
    private string[] Headers => Side
        ? ["رقم حساب الأصل", "الأصول / البيان", "الرصيد", "رقم حساب الالتزام / الملكية", "الالتزامات وحقوق الملكية / البيان", "الرصيد"]
        : Compare ? ["رقم الحساب", "اسم الحساب / البيان", "التاريخ الحالي", "التاريخ السابق", "الفرق", "التغير %"]
        : ["رقم الحساب", "اسم الحساب / البيان", "الرصيد"];
    private IReadOnlyList<(string Label, string Value)> Chips => new List<(string, string)>
    {
        ("نوع التقرير", ModeName(_applied.Mode)), ("كما في", _applied.AsOf.ToString("dd/MM/yyyy")), ("العملة", _currencyName),
        ("السنة المالية", _years.FirstOrDefault(y => y.Id.ToString() == _applied.Year)?.Name ?? "الكل"),
        ("المستوى", Details && _applied.Level != 0 ? $"حتى {_applied.Level}" : "الكل"), ("تفاصيل الحسابات",Details?"نعم":"لا"), ("الحسابات الصفرية",_applied.IncludeZero?"ظاهرة":"مخفية")
    }.Concat(Compare ? new[] { ("تاريخ المقارنة", _applied.CompareAsOf.ToString("dd/MM/yyyy")) } : Array.Empty<(string,string)>()).ToArray();

    protected override async Task OnInitializedAsync() => await ApplyAsync();
    private static async Task<IReadOnlyList<T>> All<T>(Func<PageRequest, Task<PagedResult<T>>> load)
    {
        var list = new List<T>();
        for (var n = 1; ; n++)
        {
            var page = await load(new PageRequest { PageNumber = n, PageSize = 100 });
            list.AddRange(page.Items);
            if (!page.HasNextPage || page.Items.Count == 0) return list;
        }
    }
    private async Task LoadReferencesAsync()
    {
        _years = (await All(p => Accounting.GetFiscalYearsPageAsync(p))).OrderByDescending(y => y.StartDate).ToArray();
        _currencies = await All(p => Accounting.GetCurrenciesPageAsync(p));
        var settings = await Accounting.GetAccountingSettingsAsync();
        _baseId = settings?.BaseCurrencyId;
        _baseName = _currencies.FirstOrDefault(c => c.Id == _baseId)?.NameAr ?? settings?.BaseCurrencyCode ?? "العملة الأساسية";
        _loaded = true;
    }
    private void OnYear(ChangeEventArgs e)
    {
        _draft.Year = e.Value?.ToString() ?? "";
        if (_years.FirstOrDefault(y => y.Id.ToString() == _draft.Year) is { } year)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            _draft.AsOf = (today >= year.StartDate && today <= year.EndDate ? today : year.EndDate).ToDateTime(TimeOnly.MinValue);
        }
    }
    private async Task ResetAsync()
    {
        if (_busy) return;
        _draft = new(); _search = ""; _sort = "code"; _descending = false;
        await ApplyAsync();
    }
    private async Task ApplyAsync()
    {
        if (_busy) return;
        _validation = null;
        var next = _draft with { };
        var date = DateOnly.FromDateTime(next.AsOf);
        var previous = DateOnly.FromDateTime(next.CompareAsOf);
        if (next.Mode == "compare" && previous >= date)
        { _validation = "يجب أن يسبق تاريخ المقارنة تاريخ الميزانية."; return; }
        var yearId = Guid.TryParse(next.Year, out var id) ? (Guid?)id : null;
        if (yearId.HasValue && _years.FirstOrDefault(y => y.Id == yearId) is { } year && (date < year.StartDate || date > year.EndDate))
        { _validation = "تاريخ الميزانية خارج السنة المالية المختارة."; return; }
        _busy = true; _error = null;
        try
        {
            if (!_loaded) await LoadReferencesAsync();
            var report = await Accounting.GetBalanceSheetAsync(new(date, yearId))
                ?? throw new InvalidOperationException("لم يرجع الخادم بيانات الميزانية.");
            BalanceSheetReportDto? comparison = null;
            if (next.Mode == "compare")
                comparison = await Accounting.GetBalanceSheetAsync(new(previous))
                    ?? throw new InvalidOperationException("تعذر تحميل ميزانية تاريخ المقارنة.");
            var rate = 1m; var currencyName = _baseName;
            if (next.Currency is Guid currency && currency != _baseId)
            {
                var c = _currencies.First(x => x.Id == currency);
                var fx = await Accounting.GetEffectiveExchangeRateAsync(currency, report.AsOfDate, ExchangeRateType.Accounting);
                if (fx is null || fx.Rate <= 0) throw new InvalidOperationException("لا يوجد سعر صرف محاسبي صالح في تاريخ الميزانية.");
                rate = fx.Rate; currencyName = $"{c.NameAr} ({c.Code})";
            }
            next.AsOf = report.AsOfDate.ToDateTime(TimeOnly.MinValue);
            _applied = next; _report = report; _compare = comparison; _rate = rate; _currencyName = currencyName;
            BuildRows();
        }
        catch (Exception ex)
        {
            _error = "تعذر تحميل الميزانية العمومية: " + ex.Message;
            _report = null; _compare = null; _assetRows = []; _rightRows = []; _visible = []; _page = 1;
        }
        finally { _busy = false; }
    }

    private void BuildRows()
    {
        if (_report is null) return;
        var report = _report;
        var accounts = report.Accounts.Concat(_compare?.Accounts ?? []).GroupBy(a => a.AccountId).ToDictionary(g => g.Key, g => g.First());
        var current = report.Accounts.ToDictionary(a => a.AccountId, a => a.Amount);
        var prior = _compare?.Accounts.ToDictionary(a => a.AccountId, a => a.Amount) ?? new Dictionary<Guid, decimal>();
        var assets = new List<Row>(); var other = new List<Row>();
        Row Sum(string name, IEnumerable<Guid> ids)
        {
            var keys = ids.Distinct().ToArray();
            return new("", name, keys.Sum(id => current.GetValueOrDefault(id)), keys.Sum(id => prior.GetValueOrDefault(id)), true);
        }
        Guid[] Select(AccountClass cls, FinancialStatementCategory? cat) => accounts.Values.Where(a => a.AccountClass == cls && a.Category == cat).Select(a => a.AccountId).ToArray();
        void Section(List<Row> target, string name, Guid[] ids, bool optional = false)
        {
            if (optional && !ids.Any(id => current.GetValueOrDefault(id) != 0 || prior.GetValueOrDefault(id) != 0) && !_applied.IncludeZero) return;
            target.Add(Sum(name, ids));
            if (!Details) return;
            // Aggregate only within this financial category. A direct account balance is assigned once.
            var idSet = ids.ToHashSet(); var buckets = new Dictionary<Guid, List<Guid>>();
            foreach (var id in ids)
            {
                var a = accounts[id];
                if (!_applied.IncludeZero && current.GetValueOrDefault(id) == 0 && prior.GetValueOrDefault(id) == 0) continue;
                var visited = new HashSet<Guid> { a.AccountId };
                while (_applied.Level > 0 && a.Level > _applied.Level && a.ParentAccountId is Guid p && idSet.Contains(p) && visited.Add(p) && accounts.TryGetValue(p, out var parent)) a = parent;
                if (!buckets.TryGetValue(a.AccountId, out var bucket)) buckets[a.AccountId] = bucket = [];
                bucket.Add(id);
            }
            var details = buckets.Select(pair =>
            {
                var a = accounts[pair.Key]; var sum = Sum(a.Name, pair.Value);
                return new Row(a.Code, a.Name, sum.Current, sum.Previous, false, a.AccountId, Math.Max(0, a.Level - 1), pair.Value.Any(id => id != a.AccountId));
            });
            Func<Row, object> key = r => _sort switch { "name" => r.Name, "amount" => r.Current, _ => r.Code };
            target.AddRange((_descending ? details.OrderByDescending(key) : details.OrderBy(key)).ThenBy(r => r.Code));
        }
        var ca = Select(AccountClass.Asset, FinancialStatementCategory.CurrentAssets);
        var nca = Select(AccountClass.Asset, FinancialStatementCategory.NonCurrentAssets);
        var ua = accounts.Values.Where(a => a.AccountClass == AccountClass.Asset && a.Category is not (FinancialStatementCategory.CurrentAssets or FinancialStatementCategory.NonCurrentAssets)).Select(a => a.AccountId).ToArray();
        var cl = Select(AccountClass.Liability, FinancialStatementCategory.CurrentLiabilities);
        var ncl = Select(AccountClass.Liability, FinancialStatementCategory.NonCurrentLiabilities);
        var ul = accounts.Values.Where(a => a.AccountClass == AccountClass.Liability && a.Category is not (FinancialStatementCategory.CurrentLiabilities or FinancialStatementCategory.NonCurrentLiabilities)).Select(a => a.AccountId).ToArray();
        var eq = accounts.Values.Where(a => a.AccountClass == AccountClass.Equity).Select(a => a.AccountId).ToArray();
        var nominal = accounts.Values.Where(a => a.AccountClass is AccountClass.Revenue or AccountClass.Expense).Select(a => a.AccountId).ToArray();
        var t = report.Totals; var prev = _compare?.Totals;
        Section(assets, "الأصول المتداولة", ca); Section(assets, "الأصول غير المتداولة", nca); Section(assets, "أصول غير مصنفة", ua, true);
        assets.Add(new("", "إجمالي الأصول", t.Assets, prev?.Assets ?? 0, true));
        Section(other, "الالتزامات المتداولة", cl); Section(other, "الالتزامات غير المتداولة", ncl); Section(other, "التزامات غير مصنفة", ul, true);
        other.Add(new("", "إجمالي الالتزامات", t.Liabilities, prev?.Liabilities ?? 0, true));
        Section(other, "حقوق الملكية المسجلة", eq);
        other.Add(Sum("صافي الأرباح / الخسائر غير المقفلة", nominal));
        other.Add(new("", "إجمالي حقوق الملكية", t.Equity, prev?.Equity ?? 0, true));
        other.Add(new("", "إجمالي الالتزامات وحقوق الملكية", t.LiabilitiesAndEquity, prev?.LiabilitiesAndEquity ?? 0, true));
        _assetRows = assets; _rightRows = other;
        FilterRows();
    }
    private IReadOnlyList<DisplayRow> DisplayRows(bool filtered)
    {
        bool Keep(Row r) => !filtered || r.Summary || string.IsNullOrWhiteSpace(_search) || $"{r.Code} {r.Name}".Contains(_search.Trim(), StringComparison.CurrentCultureIgnoreCase);
        var a = _assetRows.Where(Keep).ToArray(); var b = _rightRows.Where(Keep).ToArray();
        if (!Side) return a.Concat(b).Select(r => new DisplayRow(r)).ToArray();
        if (a.Length == 0 || b.Length == 0) return [];
        // Keep the grand totals aligned at the foot of the side-by-side statement.
        var list = new List<DisplayRow>();
        for (var i = 0; i < Math.Max(a.Length, b.Length) - 1; i++)
            list.Add(new(i < a.Length - 1 ? a[i] : null, i < b.Length - 1 ? b[i] : null));
        list.Add(new(a[^1], b[^1]));
        return list;
    }
    private void FilterRows() { _visible = DisplayRows(true); _page = 1; }
    private void OnSearch(string value) { _search = value; FilterRows(); }
    private void SortBy(string key) { _descending = _sort == key && !_descending; _sort = key; BuildRows(); }
    private void GoPage(int n) => _page = Math.Clamp(n, 1, TotalPages);
    private void ChangePageSize(ChangeEventArgs e) { if (int.TryParse(e.Value?.ToString(), out var n) && n is 10 or 25 or 50 or 100) _pageSize = n; _page = 1; }
    private string Money(decimal n) => (n / _rate).ToString("#,##0.00;(#,##0.00);0.00", CultureInfo.InvariantCulture);
    private string Metric(decimal? n) => CanExport && n.HasValue ? Money(n.Value) : "—";
    private Cell MoneyCell(decimal n, bool summary) => new(Money(n), n / _rate, summary, true, n < 0, Css: "oas-report__amount");
    private IEnumerable<Cell> RowCells(Row? row)
    {
        if (row is null) { for (var i = 0; i < 3; i++) yield return new("", null); yield break; }
        yield return new(string.IsNullOrEmpty(row.Code) ? "—" : row.Code, row.Code, row.Summary, Css: "trial-code");
        // Aggregated buckets can cover only a subset of descendants, so do not link them to a full subtree statement.
        yield return new(row.Name, row.Name, row.Summary, AccountId: row.Aggregate ? null : row.AccountId, Depth: row.Depth, Css: "trial-name");
        yield return MoneyCell(row.Current, row.Summary);
        if (Compare)
        {
            yield return MoneyCell(row.Previous, row.Summary);
            var difference = row.Current - row.Previous;
            yield return MoneyCell(difference, row.Summary);
            var percent = row.Previous == 0m ? (decimal?)null : difference / Math.Abs(row.Previous) * 100m;
            var value = percent.HasValue ? percent.Value.ToString("0.00", CultureInfo.InvariantCulture) + "%" : "—";
            yield return new(value, value, row.Summary, true, percent < 0, Css: "oas-report__amount");
        }
    }
    private IEnumerable<Cell> Cells(DisplayRow r) => Side ? RowCells(r.First).Concat(RowCells(r.Second)) : RowCells(r.First);
    private void OpenStatement(Guid id)
    {
        var end = _applied.AsOf;
        var date = DateOnly.FromDateTime(end);
        var start = _years.FirstOrDefault(y => y.StartDate <= date && y.EndDate >= date)?.StartDate ?? new DateOnly(end.Year, 1, 1);
        Nav.NavigateTo($"/accounting/reports/account-statement?accountId={id:D}&fromDate={start:yyyy-MM-dd}&toDate={end:yyyy-MM-dd}&includeChildren=false{(_applied.Currency is Guid currency ? $"&currencyId={currency:D}" : "")}");
    }
    private string Subtitle => string.Join(" | ", Chips.Select(c => $"{c.Label}: {c.Value}"))
        + $" | فرق التوازن بالعملة الأساسية: {Totals?.Difference.ToString("N2", CultureInfo.InvariantCulture)}"
        + (Compare ? $" | فرق المقارنة بالعملة الأساسية: {_compare?.Totals.Difference.ToString("N2", CultureInfo.InvariantCulture)}" : "")
        + (Unclassified ? " | توجد أرصدة غير مصنفة، المجاميع حسب الاستحقاق ورأس المال العامل غير مكتملة." : "")
        + " | التحويل بسعر تاريخ الميزانية الحالية؛ الإجماليات والمخرجات لا تتأثر بالبحث النصي.";
    private async Task ExportAsync()
    {
        if (!CanExport) return;
        try
        {
            var bytes = CustomerDebtsWorkbook.Create(Subtitle, Headers, DisplayRows(false).Select(r => Cells(r).Select(c => c.Value).ToArray()), sheetName: "الميزانية العمومية");
            await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/customer-debts-export.js");
            await module.InvokeVoidAsync("saveWorkbook", Convert.ToBase64String(bytes), "balance-sheet.xlsx");
        }
        catch (Exception ex) { _validation = "تعذر التصدير: " + ex.Message; }
    }
    private async Task PrintAsync()
    {
        if (!CanExport) return;
        try
        {
            var rows = DisplayRows(false).Select(r => Cells(r).ToArray()).ToArray();
            await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/balance-sheet-print.js");
            await module.InvokeVoidAsync("printBalanceSheet", Subtitle, Headers, rows.Select(r => r.Select(c => c.Text).ToArray()).ToArray(), rows.Select(r => r.Select(c => c.Summary).ToArray()).ToArray(), Side);
        }
        catch (Exception ex) { _validation = "تعذرت الطباعة: " + ex.Message; }
    }
    private static string ModeName(string mode) => mode switch { "detail" => "تفصيلي", "summary" => "إجمالي", "compare" => "مقارنة الفترات", _ => "عرض جانبي" };
}
