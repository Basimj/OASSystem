using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Services;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;
using Totals = OAS.Client.Accounting.Pages.IncomeStatementPresentation.Totals;

namespace OAS.Client.Accounting.Pages;
public partial class IncomeStatementPage
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
        public DateTime CompareFrom { get; set; } = new(DateTime.Today.Year - 1, 1, 1);
        public DateTime CompareTo { get; set; } = DateTime.Today.AddYears(-1);
        public string Mode { get; set; } = "detail";
        public int Level { get; set; }
        public Guid? Currency { get; set; }
        public Guid? CostCenter { get; set; }
        public bool Children { get; set; } = true;
        public bool IncludeZero { get; set; }
    }
    private sealed record Row(string Code, string Name, decimal[] Values, bool Summary, Guid? AccountId = null, bool Aggregate = false, int Depth = 0);
    private Filters _draft = new(), _applied = new();
    private IReadOnlyList<FiscalYearDto> _years = [];
    private IReadOnlyList<FiscalPeriodDto> _periods = [];
    private IReadOnlyList<CurrencyDto> _currencies = [];
    private IReadOnlyList<CostCenterDto> _centers = [];
    private IncomeStatementReportDto? _report, _compare;
    private Totals _totals = new(0,0,0,0,0,0,0,0);
    private IReadOnlyList<Row> _rows = [];
    private IReadOnlyList<Row> _visible = [];
    private IReadOnlyList<DateOnly> _months = [];
    private Guid? _baseId;
    private decimal _rate = 1m;
    private string _baseName = "العملة الأساسية", _currencyName = "العملة الأساسية";
    private string _search = "", _sort = "code";
    private bool _busy, _loaded, _filtersOpen = true, _descending, _unclassified;
    private string? _error, _validation;
    private int _page = 1, _pageSize = 25;
    private int TotalPages => Math.Max(1, (_visible.Count + _pageSize - 1)/_pageSize);
    private IEnumerable<Row> PageRows => _visible.Skip((_page-1)*_pageSize).Take(_pageSize);
    private bool CanExport => !_busy && _error is null && _report is not null;
    private string Status => _busy ? "جارٍ التحميل" : _error is not null || _report is null ? "غير متاح" : _unclassified ? "توجد حسابات غير مصنفة" : "تم الحساب";
    private string StatusIcon => _unclassified || _error is not null ? "fa-solid fa-circle-exclamation" : "fa-solid fa-circle-check";
    private IEnumerable<FiscalPeriodDto> PeriodOptions => _periods.Where(p => string.IsNullOrEmpty(_draft.Year) || p.FiscalYearId.ToString() == _draft.Year).OrderBy(p => p.StartDate);
    private IEnumerable<int> Levels => (_report?.Accounts ?? []).Select(a => (int)a.Level).Distinct().Order();
    private bool Details => _applied.Mode == "detail" && _applied.Children;
    private string[] Headers => new[] { "رقم الحساب", "اسم الحساب / البيان" }.Concat(_applied.Mode switch
    {
        "compare" => new[] { "الفترة الحالية", "الفترة السابقة", "الفرق", "التغير %" },
        "monthly" => _months.Select(m => m.ToString("MM/yyyy", CultureInfo.InvariantCulture)).Append("الإجمالي").ToArray(),
        "ratio" => new[] { "المبلغ", "النسبة من صافي الإيرادات" },
        _ => new[] { "المبلغ" }
    }).ToArray();
    private IReadOnlyList<(string Label,string Value)> Chips => new List<(string,string)>
    {
        ("نوع التقرير",ModeName(_applied.Mode)), ("الفترة",$"{_applied.From:dd/MM/yyyy} – {_applied.To:dd/MM/yyyy}"), ("العملة",_currencyName),
        ("مركز التكلفة",_centers.FirstOrDefault(c=>c.Id==_applied.CostCenter)?.NameAr ?? "الكل"),
        ("المستوى",!Details || _applied.Level == 0 ? "الكل" : $"حتى {_applied.Level}"), ("تفاصيل الحسابات",Details?"نعم":"لا"), ("الحسابات الصفرية",_applied.IncludeZero?"ظاهرة":"مخفية"), ("السنة",_years.FirstOrDefault(y=>y.Id.ToString()==_applied.Year)?.Name??"نطاق مخصص"), ("الفترة المالية",_periods.FirstOrDefault(p=>p.Id.ToString()==_applied.Period)?.Name??"الكل")
    }.Concat(_applied.Mode == "compare" ? new[] { ("المقارنة",$"{_applied.CompareFrom:dd/MM/yyyy} – {_applied.CompareTo:dd/MM/yyyy}") } : Array.Empty<(string,string)>()).ToArray();
    protected override async Task OnInitializedAsync() => await ApplyAsync();
    private static async Task<IReadOnlyList<T>> All<T>(Func<PageRequest,Task<PagedResult<T>>> load)
    {
        var list=new List<T>();
        for(var n=1;;n++) { var p=await load(new PageRequest{PageNumber=n,PageSize=100}); list.AddRange(p.Items); if(!p.HasNextPage || p.Items.Count==0) return list; }
    }
    private async Task LoadReferencesAsync()
    {
        _years=(await All(p=>Accounting.GetFiscalYearsPageAsync(p))).OrderByDescending(y=>y.StartDate).ToArray();
        _periods=await All(p=>Accounting.GetFiscalPeriodsPageAsync(p));
        _currencies=await All(p=>Accounting.GetCurrenciesPageAsync(p));
        // A user may have report permission but not cost-center directory permission.
        try { _centers=await All(p=>Accounting.GetCostCentersPageAsync(p)); } catch { _centers=[]; }
        var settings=await Accounting.GetAccountingSettingsAsync(); _baseId=settings?.BaseCurrencyId;
        _baseName=_currencies.FirstOrDefault(c=>c.Id==_baseId)?.NameAr ?? settings?.BaseCurrencyCode ?? "العملة الأساسية";
        _loaded=true;
    }
    private void OnYear(ChangeEventArgs e) { _draft.Year=e.Value?.ToString()??""; _draft.Period=""; if(_years.FirstOrDefault(y=>y.Id.ToString()==_draft.Year) is {} y){ _draft.From=y.StartDate.ToDateTime(TimeOnly.MinValue); _draft.To=y.EndDate.ToDateTime(TimeOnly.MinValue); } }
    private void OnPeriod(ChangeEventArgs e) { _draft.Period=e.Value?.ToString()??""; if(_periods.FirstOrDefault(p=>p.Id.ToString()==_draft.Period) is {} p){ _draft.Year=p.FiscalYearId.ToString(); _draft.From=p.StartDate.ToDateTime(TimeOnly.MinValue); _draft.To=p.EndDate.ToDateTime(TimeOnly.MinValue); } }
    private static Guid? Id(string s)=>Guid.TryParse(s,out var id)?id:null;
    private async Task ResetAsync(){if(_busy)return; _draft=new();_search="";_sort="code";_descending=false;await ApplyAsync();}
    private async Task ApplyAsync()
    {
        if(_busy)return; _validation=null; var next=_draft with{};
        if(next.From.Date>next.To.Date || (next.Mode=="compare" && next.CompareFrom.Date>next.CompareTo.Date)){_validation="تاريخ البداية يجب ألا يتجاوز تاريخ النهاية.";return;}
        if(next.Mode=="monthly" && (next.To.Year-next.From.Year)*12+next.To.Month-next.From.Month>=12){_validation="اختر فترة لا تتجاوز 12 شهراً للعرض الشهري.";return;}
        _busy=true;_error=null;
        try
        {
            if(!_loaded)await LoadReferencesAsync();
            var report=await Accounting.GetIncomeStatementAsync(new(DateOnly.FromDateTime(next.From),DateOnly.FromDateTime(next.To),Id(next.Year),Id(next.Period),next.CostCenter)) ?? throw new InvalidOperationException("لم يرجع الخادم بيانات القائمة.");
            IncomeStatementReportDto? comparison=null;
            if(next.Mode=="compare")comparison=await Accounting.GetIncomeStatementAsync(new(DateOnly.FromDateTime(next.CompareFrom),DateOnly.FromDateTime(next.CompareTo),CostCenterId:next.CostCenter)) ?? throw new InvalidOperationException("تعذر تحميل فترة المقارنة.");
            var rate=1m; var currencyName=_baseName;
            if(next.Currency is Guid currency && currency!=_baseId){var c=_currencies.First(x=>x.Id==currency);var fx=await Accounting.GetEffectiveExchangeRateAsync(currency,report.ToDate,ExchangeRateType.Accounting);if(fx is null || fx.Rate<=0)throw new InvalidOperationException("لا يوجد سعر صرف محاسبي صالح.");rate=fx.Rate;currencyName=$"{c.NameAr} ({c.Code})";}
            next.From=report.FromDate.ToDateTime(TimeOnly.MinValue);next.To=report.ToDate.ToDateTime(TimeOnly.MinValue);
            _applied=next;_report=report;_compare=comparison;_rate=rate;_currencyName=currencyName;
            _months=Months(report.FromDate,report.ToDate).ToArray();
            _totals=IncomeStatementPresentation.Sum(report.Accounts,report.Movements);
            _unclassified=report.Movements.Any(m=>m.Amount!=0 && report.Accounts.Any(a=>a.AccountId==m.AccountId && a.Category is null));
            if(comparison is not null) _unclassified |= comparison.Movements.Any(m=>m.Amount!=0 && comparison.Accounts.Any(a=>a.AccountId==m.AccountId && a.Category is null));
            BuildRows();
        }
        catch(Exception ex){_error="تعذر تحميل قائمة الدخل: "+ex.Message;_report=null;_compare=null;_rows=[];_visible=[];_page=1;_unclassified=false;}
        finally{_busy=false;}
    }
    private static IEnumerable<DateOnly> Months(DateOnly from,DateOnly to){for(var d=new DateOnly(from.Year,from.Month,1);d<=to;d=d.AddMonths(1)){yield return d;if(d.Year==to.Year&&d.Month==to.Month)yield break;}}
    private void BuildRows()
    {
        if(_report is null)return;
        var report=_report;
        var accounts=report.Accounts.Concat(_compare?.Accounts ?? []).GroupBy(a=>a.AccountId).ToDictionary(g=>g.Key,g=>g.First());
        var current=report.Movements.GroupBy(m=>m.AccountId).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Amount));
        var previous=_compare?.Movements.GroupBy(m=>m.AccountId).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Amount)) ?? new Dictionary<Guid,decimal>();
        var months=report.Movements.GroupBy(m=>(m.AccountId,m.Year,m.Month)).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Amount));
        decimal[] Values(IEnumerable<Guid> ids){var a=ids.ToArray();var sum=a.Sum(id=>current.GetValueOrDefault(id));return _applied.Mode switch {"compare"=>[sum,a.Sum(id=>previous.GetValueOrDefault(id))],"monthly"=>_months.Select(m=>a.Sum(id=>months.GetValueOrDefault((id,m.Year,m.Month)))).Append(sum).ToArray(),_=>[sum]};}
        Guid[] Category(params FinancialStatementCategory[] c)=>accounts.Values.Where(a=>a.Category.HasValue&&c.Contains(a.Category.Value)).Select(a=>a.AccountId).ToArray();
        var rev=Category(FinancialStatementCategory.OperatingRevenue,FinancialStatementCategory.RevenueReductions);var cost=Category(FinancialStatementCategory.CostOfSales);var op=Category(FinancialStatementCategory.OperatingExpenses);var otherRev=Category(FinancialStatementCategory.OtherRevenue);var otherExp=Category(FinancialStatementCategory.OtherExpenses);var tax=Category(FinancialStatementCategory.IncomeTax);
        var uncRev=accounts.Values.Where(a=>a.Category is null&&a.AccountClass==AccountClass.Revenue).Select(a=>a.AccountId).ToArray();var uncExp=accounts.Values.Where(a=>a.Category is null&&a.AccountClass==AccountClass.Expense).Select(a=>a.AccountId).ToArray();
        var list=new List<Row>();
        void Sum(string name,IEnumerable<Guid> ids)=>list.Add(new("",name,Values(ids),true));
        void Section(string name,Guid[] ids,bool optional=false)
        {
            if(optional&&!ids.Any(id=>current.GetValueOrDefault(id)!=0||previous.GetValueOrDefault(id)!=0)&&!_applied.IncludeZero)return;
            Sum(name,ids); if(!Details)return;
            var idset=ids.ToHashSet(); var buckets=new Dictionary<Guid,List<Guid>>();
            foreach(var id in ids)
            {
                var a=accounts[id]; if(!_applied.IncludeZero && !Values([id]).Any(v=>v!=0))continue;
                // Same-category ancestors only; each direct movement is assigned once at the requested level.
                var seen=new HashSet<Guid>{a.AccountId};
                while(_applied.Level>0&&a.Level>_applied.Level&&a.ParentAccountId is Guid p&&idset.Contains(p)&&seen.Add(p)&&accounts.TryGetValue(p,out var parent))a=parent;
                if(!buckets.TryGetValue(a.AccountId,out var bucket))buckets[a.AccountId]=bucket=[];bucket.Add(id);
            }
            var detail=buckets.Select(kv=>{var a=accounts[kv.Key];return new Row(a.Code,a.Name,Values(kv.Value),false,a.AccountId,kv.Value.Any(id=>id!=a.AccountId),Math.Max(0,a.Level-1));});
            Func<Row,object> key=r=>_sort switch{"name"=>r.Name,"amount"=>r.Values[0],_=>r.Code};
            list.AddRange((_descending?detail.OrderByDescending(key):detail.OrderBy(key)).ThenBy(r=>r.Code));
        }
        Section("صافي الإيرادات التشغيلية",rev);Section("تكلفة المبيعات",cost);var gross=rev.Concat(cost).ToArray();Sum("مجمل الربح",gross);
        Section("المصروفات التشغيلية",op);var operating=gross.Concat(op).ToArray();Sum("الربح التشغيلي",operating);
        Section("الإيرادات الأخرى",otherRev);Section("المصروفات الأخرى",otherExp);Section("إيرادات غير مصنفة",uncRev,true);Section("مصروفات غير مصنفة",uncExp,true);
        var before=operating.Concat(otherRev).Concat(otherExp).Concat(uncRev).Concat(uncExp).ToArray();Sum("الربح قبل الضريبة",before);Section("ضريبة الدخل",tax);
        Sum("صافي الربح / الخسارة",before.Concat(tax));_rows=list;FilterRows();
    }
    private void FilterRows(){_visible=_rows.Where(r=>r.Summary||string.IsNullOrWhiteSpace(_search)||$"{r.Code} {r.Name}".Contains(_search.Trim(),StringComparison.CurrentCultureIgnoreCase)).ToArray();_page=1;}
    private void OnSearch(string value){_search=value;FilterRows();}
    private void SortBy(string key){_descending=_sort==key&&!_descending;_sort=key;BuildRows();}
    private void GoPage(int n)=>_page=Math.Clamp(n,1,TotalPages);
    private void ChangePageSize(ChangeEventArgs e){if(int.TryParse(e.Value?.ToString(),out var n)&&n is 10 or 25 or 50 or 100)_pageSize=n;_page=1;}
    private string Money(decimal n)=>(n/_rate).ToString("#,##0.00;(#,##0.00);0.00",CultureInfo.InvariantCulture);
    private string Metric(decimal n)=>CanExport?Money(n):"—";
    private decimal? Ratio(decimal n)=>_totals.Revenue>0?n/_totals.Revenue*100m:null;
    private static decimal? Change(decimal a,decimal b)=>b==0?null:(a-b)/Math.Abs(b)*100m;
    private string Percent(decimal? n)=>n.HasValue?n.Value.ToString("0.00",CultureInfo.InvariantCulture)+"%":"—";
    private IEnumerable<(string Text,bool Negative,object? Value)> Cells(Row r)
    {
        IEnumerable<decimal> amounts=r.Values;
        if(_applied.Mode=="compare") amounts=amounts.Append(r.Values[0]-r.Values[1]);
        foreach(var amount in amounts)yield return (Money(amount),amount<0,amount/_rate);
        if(_applied.Mode=="compare"){var v=Change(r.Values[0],r.Values[1]);yield return(Percent(v),v<0,v.HasValue?(object)Percent(v):null);}
        if(_applied.Mode=="ratio"){var v=Ratio(r.Values[0]);yield return(Percent(v),v<0,v.HasValue?(object)Percent(v):null);}
    }
    private void OpenStatement(Row r){if(r.AccountId is Guid id)Nav.NavigateTo($"/accounting/reports/account-statement?accountId={id:D}&fromDate={_applied.From:yyyy-MM-dd}&toDate={_applied.To:yyyy-MM-dd}&includeChildren={r.Aggregate.ToString().ToLowerInvariant()}{(_applied.Currency is Guid currency?$"&currencyId={currency:D}":"")}");}
    private string Subtitle => string.Join(" | ",Chips.Select(c=>$"{c.Label}: {c.Value}"))+(_unclassified?" | توجد حسابات غير مصنفة؛ المجاميع الوسيطة مؤقتة":"")+" | التحويل بسعر نهاية الفترة الحالية؛ الإجماليات لا تتأثر بالبحث؛ المخرجات تشمل صفوف البحث: "+_search+"."+(_applied.CostCenter.HasValue?" | تشمل القيود المسندة للمركز فقط.":"");
    private async Task ExportAsync(){if(!CanExport)return;try{var bytes=CustomerDebtsWorkbook.Create(Subtitle,Headers,_visible.Select(r=>new object?[]{r.Code,r.Name}.Concat(Cells(r).Select(c=>c.Value)).ToArray()),sheetName:"قائمة الدخل");await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/customer-debts-export.js");await module.InvokeVoidAsync("saveWorkbook",Convert.ToBase64String(bytes),"income-statement.xlsx");}catch(Exception ex){_validation="تعذر التصدير: "+ex.Message;}}
    private async Task PrintAsync(){if(!CanExport)return;try{await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/income-statement-print.js");await module.InvokeVoidAsync("printIncomeStatement",Subtitle,Headers,_visible.Select(r=>new[]{r.Code,r.Name}.Concat(Cells(r).Select(c=>c.Text)).ToArray()).ToArray(),_visible.Select(r=>r.Summary).ToArray());}catch(Exception ex){_validation="تعذرت الطباعة: "+ex.Message;}}
    private static string ModeName(string mode)=>mode switch{"summary"=>"إجمالي","compare"=>"مقارنة الفترات","monthly"=>"شهري","ratio"=>"تحليل النسب",_=>"تفصيلي"};
}
