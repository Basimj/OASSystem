using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Common.Pagination;
using OAS.UiLib.Core.Models;
namespace OAS.Client.Accounting.Pages;

public partial class VoucherReportPage
{
    [Inject] private OasApiClient Api { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    private sealed record Filters
    {
        public string Year { get; set; } = "";
        public string Period { get; set; } = "";
        public string Settlement { get; set; } = "";
        public DateTime From { get; set; } = new(DateTime.Today.Year,DateTime.Today.Month,1);
        public DateTime To { get; set; } = DateTime.Today;
        public string Mode { get; set; } = "all";
        public string Kind { get; set; } = "all";
        public string Status { get; set; } = "posted";
        public string Account { get; set; } = "";
        public Guid? Currency { get; set; }
        public Guid? DisplayCurrency { get; set; }
        public int? Method { get; set; }
        public int? PartyType { get; set; }
        public string Party { get; set; } = "";
        public Guid? PostedBy { get; set; }
    }
    private static readonly Dictionary<string,string> Modes = new() { ["all"]="إجمالي السندات", ["lines"]="تفاصيل البنود", ["customers"]="حسب العملاء", ["suppliers"]="حسب الموردين", ["employees"]="حسب الموظفين", ["accounts"]="حسب الحساب المحاسبي", ["settlements"]="حسب الصندوق والبنك", ["methods"]="حسب وسيلة الدفع", ["currency"]="حسب العملة" };
    private static readonly string[] DefaultMetricLabels = ["عدد سندات القبض","عدد سندات الصرف","صافي المقبوضات","صافي المدفوعات","السندات غير المرحلة","صافي حركة السندات"];
    private static readonly string[] Tones = ["blue","green","red","orange","purple","green"];
    private static readonly string[] Icons = ["fa-solid fa-wallet","fa-solid fa-arrow-down","fa-solid fa-arrow-up","fa-solid fa-money-bill","fa-solid fa-landmark","fa-solid fa-coins"];
    private Filters _draft = new(), _applied = new();
    private CashBankReportDto? _report;
    private IReadOnlyList<CashBankReportRow> _visible = [];
    private IReadOnlyList<CashBankReportAccountDto> _accounts = [], _settlements = [];
    private IReadOnlyList<FiscalYearDto> _years = [];
    private IReadOnlyList<FiscalPeriodDto> _periods = [];
    private bool _referencesLoaded;
    private string? _referenceWarning;
    private IEnumerable<FiscalPeriodDto> PeriodOptions => _periods.Where(p=>p.FiscalYearId.ToString()==_draft.Year).OrderBy(p=>p.StartDate);
    private bool PartyMode => _draft.Mode is "customers" or "suppliers" or "employees";
    private string PartyLabel => _draft.Mode switch { "customers"=>"اسم العميل", "suppliers"=>"اسم المورد", "employees"=>"اسم الموظف", _=>"اسم الطرف (في السند)" };
    private IReadOnlyList<UiSelectOption> SettlementOptions => new[] { new UiSelectOption("", "الكل") }
        .Concat(_settlements.Where(a=>a.CashBoxId.HasValue).GroupBy(a=>a.CashBoxId).Select(g=>new UiSelectOption($"box:{g.Key:D}", $"صندوق - {g.First().CashBoxName} (كل العملات)")))
        .Concat(_settlements.Select(a=>new UiSelectOption($"{a.Kind}:{a.AccountId:D}", $"{(a.Kind=="cash"?"صندوق":"بنك")} - {a.Code} - {a.Name} ({Currencies.FirstOrDefault(c=>c.Id==a.CurrencyId)?.Code??"—"})"))).ToArray();
    private IReadOnlyList<CashBankReportCurrencyDto> Currencies = [];
    private IReadOnlyList<CashBankReportOperatorDto> Operators = [];
    private bool _busy, _filtersOpen = true, _descending;
    private string _search = "", _displayCode = "";
    private string? _error, _validation;
    private int _page = 1, _pageSize = 25, _sort = -1, _decimals = 2;
    private decimal _rate = 1;
    private string BaseCode => _report?.BaseCurrencyCode ?? "العملة الأساسية";
    private bool CanExport => !_busy && _report is not null && _error is null;
    private string Status => _busy ? "جارٍ التحميل" : CanExport ? $"{_report!.Rows.Count} سجل" : "غير متاح";
    private int TotalPages => Math.Max(1, (_visible.Count + _pageSize - 1) / _pageSize);
    private IEnumerable<CashBankReportRow> PageRows => _visible.Skip((_page-1)*_pageSize).Take(_pageSize);
    private string[] Headers => _report?.Headers.ToArray() ?? ["الحساب", "البيان"];
    private IReadOnlyList<UiSelectOption> AccountOptions => new[] { new UiSelectOption("", "الكل") }.Concat(_accounts.Select(a => new UiSelectOption(a.AccountId.ToString(), $"{a.Code} - {a.Name}"))).ToArray();
    private string[] Chips => [$"نوع التقرير: {Modes[_applied.Mode]}", $"السنة: {_years.FirstOrDefault(y=>y.Id.ToString()==_applied.Year)?.Name??"نطاق مخصص"}", $"الفترة: {_periods.FirstOrDefault(p=>p.Id.ToString()==_applied.Period)?.Name??"الكل"}", $"الصندوق / البنك: {_settlements.FirstOrDefault(a=>$"{a.Kind}:{a.AccountId:D}"==_applied.Settlement)?.Name??"الكل"}", $"من: {_applied.From:dd/MM/yyyy}", $"إلى: {_applied.To:dd/MM/yyyy}", $"الحساب: {_accounts.FirstOrDefault(a=>a.AccountId.ToString()==_applied.Account)?.Name??"الكل"}", $"عملة السند: {Currencies.FirstOrDefault(c=>c.Id==_applied.Currency)?.Code??"الكل"}", $"عملة العرض: {_displayCode}", $"الحالة: {StatusLabel(_applied.Status)}", $"الطرف: {_applied.Party}", $"نوع الطرف: {_applied.PartyType?.ToString()??"الكل"}", $"وسيلة الدفع: {_applied.Method?.ToString()??"الكل"}", $"المستخدم المرحّل: {Operators.FirstOrDefault(x=>x.Id==_applied.PostedBy)?.Name??"الكل"}", $"النوع: {(_applied.Kind=="receipt"?"قبض":_applied.Kind=="payment"?"صرف":"الكل")}"];
    private static string StatusLabel(string s)=>s switch {"posted"=>"مرحل", "draft"=>"مسودة", "approved"=>"معتمد", "cancelled"=>"ملغى", "reversed"=>"معكوس", _=>"الكل"};
    protected override Task OnInitializedAsync() => ApplyAsync();
    private void OnAccount(string? value) => _draft.Account = value ?? "";
    private void OnSettlement(string? value) => _draft.Settlement = value ?? "";
    private void ModeChanged() { _draft.PartyType=null; _draft.Party=""; }
    private static async Task<IReadOnlyList<T>> All<T>(Func<PageRequest,Task<PagedResult<T>>> load)
    {
        var result = new List<T>();
        for(var n=1;;n++) { var page=await load(new PageRequest{PageNumber=n,PageSize=100}); result.AddRange(page.Items); if(!page.HasNextPage || page.Items.Count==0)return result; }
    }
    private async Task LoadReferencesAsync()
    {
        try
        {
            _years=(await All(p=>Accounting.GetFiscalYearsPageAsync(p))).OrderByDescending(y=>y.StartDate).ToArray();
        }
        catch(ApiClientException ex) when(ex.StatusCode is 401 or 403)
        { _referenceWarning="لا تتوفر صلاحية عرض السنوات المالية؛ استخدم نطاق التاريخ المخصص."; _referencesLoaded=true; return; }
        try { _periods=await All(p=>Accounting.GetFiscalPeriodsPageAsync(p)); }
        catch(ApiClientException ex) when(ex.StatusCode is 401 or 403)
        { _referenceWarning="لا تتوفر صلاحية عرض الفترات المالية؛ اختر السنة كاملة أو نطاقًا مخصصًا."; }
        _referencesLoaded=true;
    }
    private void OnYear(ChangeEventArgs e)
    {
        _draft.Year=e.Value?.ToString()??""; _draft.Period="";
        SetYearDates();
    }
    private void SetYearDates()
    {
        if(_years.FirstOrDefault(y=>y.Id.ToString()==_draft.Year) is {} y)
        { _draft.From=y.StartDate.ToDateTime(TimeOnly.MinValue); _draft.To=y.EndDate.ToDateTime(TimeOnly.MinValue); }
    }
    private void OnPeriod(ChangeEventArgs e)
    {
        _draft.Period=e.Value?.ToString()??"";
        if(PeriodOptions.FirstOrDefault(p=>p.Id.ToString()==_draft.Period) is {} p)
        { _draft.From=p.StartDate.ToDateTime(TimeOnly.MinValue); _draft.To=p.EndDate.ToDateTime(TimeOnly.MinValue); }
        else SetYearDates();
    }
    private async Task ResetAsync() { if(_busy)return; _draft=new(); _search=""; await ApplyAsync(); }
    private async Task ApplyAsync()
    {
        if(_busy)return;
        _validation=null; var f=_draft with { };
        if(f.Mode is "customers" or "suppliers" or "employees") f.PartyType=null;
        if(f.From>f.To || f.To.Date==DateTime.MaxValue.Date){_validation="حدد فترة صحيحة.";return;}
        _busy=true; _error=null;
        try
        {
            if(!_referencesLoaded)await LoadReferencesAsync();
            var query=$"api/accounting/reports/vouchers?fromDate={f.From:yyyy-MM-dd}&toDate={f.To:yyyy-MM-dd}&mode={f.Mode}&kind={f.Kind}&status={f.Status}";
            if(Guid.TryParse(f.Account,out var id))query+=$"&accountId={id:D}";
            var settlement=f.Settlement.Split(':');
            if(settlement.Length==2 && Guid.TryParse(settlement[1],out var definitionId))
            {
                if(settlement[0]=="cash") query+=$"&cashAccountId={definitionId:D}";
                else if(settlement[0]=="box") query+=$"&cashBoxId={definitionId:D}";
                else if(settlement[0]=="bank") query+=$"&bankAccountId={definitionId:D}";
            }
            if(f.Currency is Guid currency)query+=$"&currencyId={currency:D}";
            if(f.Method is int method)query+=$"&method={method}";
            if(f.PartyType is int partyType)query+=$"&partyType={partyType}";
            if(!string.IsNullOrWhiteSpace(f.Party))query+=$"&party={Uri.EscapeDataString(f.Party.Trim())}";
            if(f.PostedBy is Guid postedBy)query+=$"&postedBy={postedBy:D}";
            var report=await Api.GetAsync<CashBankReportDto>(query) ?? throw new InvalidOperationException("لم يرجع الخادم بيانات التقرير.");
            _accounts=report.Accounts; _settlements=report.SettlementAccounts; Currencies=report.Currencies; Operators=report.Operators;
            decimal rate=1; var code=report.BaseCurrencyCode; int decimals=report.BaseDecimalPlaces;
            if(f.DisplayCurrency is Guid display && display!=report.BaseCurrencyId)
            {
                var c=Currencies.FirstOrDefault(x=>x.Id==display) ?? throw new InvalidOperationException("عملة العرض غير متوفرة.");
                var fx=await Accounting.GetEffectiveExchangeRateAsync(display, DateOnly.FromDateTime(f.To));
                if(fx is null || fx.Rate<=0)throw new InvalidOperationException("لا يوجد سعر صرف معتمد لعملة العرض في نهاية الفترة.");
                rate=fx.Rate; code=c.Code; decimals=c.DecimalPlaces;
            }
            _rate=rate; _displayCode=code; _decimals=decimals; _report=report; _applied=f; _sort=-1; _descending=false; RefreshRows();
        }
        catch(Exception ex){_error="تعذر تحميل التقرير: "+ex.Message;_report=null;_visible=[];_page=1;}
        finally{_busy=false;}
    }
    private decimal? DisplayAmount(CashBankReportCell c) => c.Format=="base"?c.Amount/_rate:c.Amount;
    private string Format(CashBankReportCell c)
    {
        if(c.Format=="text")return string.IsNullOrWhiteSpace(c.Text)?"—":c.Text;
        if(DisplayAmount(c) is not decimal v)return "—";
        return v.ToString(c.Format=="count"?"N0":c.Format=="base"?$"N{_decimals}":"#,##0.######;(#,##0.######);0",CultureInfo.InvariantCulture);
    }
    private object? ExportValue(CashBankReportCell c) => c.Format=="text"?c.Text:DisplayAmount(c);
    private void RefreshRows()
    {
        IEnumerable<CashBankReportRow> rows=_report?.Rows??[];
        if(!string.IsNullOrWhiteSpace(_search))rows=rows.Where(r=>r.Cells.Any(c=>Format(c).Contains(_search.Trim(),StringComparison.CurrentCultureIgnoreCase)));
        if(_sort>=0&&_sort<Headers.Length)
        {
            var index=_sort;
            if(_report?.Rows.FirstOrDefault()?.Cells[index].Format!="text")
                rows=_descending?rows.OrderByDescending(r=>DisplayAmount(r.Cells[index])):rows.OrderBy(r=>DisplayAmount(r.Cells[index]));
            else rows=_descending?rows.OrderByDescending(r=>r.Cells[index].Text):rows.OrderBy(r=>r.Cells[index].Text);
        }
        _visible=rows.ToArray();_page=1;
    }
    private void OnSearch(string value){_search=value;RefreshRows();}
    private void SortBy(int index){_descending=_sort==index&&!_descending;_sort=index;RefreshRows();}
    private void GoPage(int page)=>_page=Math.Clamp(page,1,TotalPages);
    private void ChangePageSize(ChangeEventArgs e){if(int.TryParse(e.Value?.ToString(),out var n)&&n is 10 or 25 or 50 or 100)_pageSize=n;_page=1;}
    private void OpenLink(CashBankReportCell c)
    {
        if(c.LinkId is not Guid id)return;
        switch(c.LinkKind)
        {
            case "account": Nav.NavigateTo($"/accounting/reports/account-statement?accountId={id:D}&fromDate={_applied.From:yyyy-MM-dd}&toDate={_applied.To:yyyy-MM-dd}&includeChildren=false"+(_applied.DisplayCurrency is Guid currency?$"&currencyId={currency:D}":""));break;
            case "journal": Nav.NavigateTo($"/accounting/journals?journalId={id:D}");break;
            case "receipt": Nav.NavigateTo($"/accounting/receipt-vouchers?voucherId={id:D}");break;
            case "payment": Nav.NavigateTo($"/accounting/payment-vouchers?voucherId={id:D}");break;

        }
    }
    private IEnumerable<CashBankReportRow> AllExportRows => _report is null ? [] : _report.TotalRow is { } total ? _report.Rows.Append(total) : _report.Rows;
    private string Subtitle=>string.Join(" | ",Chips)+" | القيم الدفترية بعملة العرض بسعر نهاية الفترة. المجاميع تشمل كامل نتائج الفلاتر ولا تتأثر بالبحث. | "+string.Join(" | ",_report?.Warnings??[]);
    private async Task ExportAsync()
    {
        if(!CanExport)return;
        try
        {
            var rows=AllExportRows.Select(r=>r.Cells.Select(ExportValue).ToArray());
            var totals=_report!.Metrics.Select(m=>new object?[]{m.Label,ExportValue(m.Value)});
            var bytes=CustomerDebtsWorkbook.Create(Subtitle,Headers,rows.Concat(totals),sheetName:"سندات القبض والصرف");
            await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/customer-debts-export.js");
            await module.InvokeVoidAsync("saveWorkbook",Convert.ToBase64String(bytes),"vouchers.xlsx");
        }
        catch(Exception ex){_validation="تعذر التصدير: "+ex.Message;}
    }
    private async Task PrintAsync()
    {
        if(!CanExport)return;
        try
        {
            await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/voucher-report-print.js");
            await module.InvokeVoidAsync("printReport",Modes[_applied.Mode],Subtitle,Headers,AllExportRows.Select(r=>r.Cells.Select(Format).ToArray()).ToArray(),_report!.Metrics.Select(m=>new[]{m.Label,Format(m.Value)}).ToArray());
        }
        catch(Exception ex){_validation="تعذرت الطباعة: "+ex.Message;}
    }
}
