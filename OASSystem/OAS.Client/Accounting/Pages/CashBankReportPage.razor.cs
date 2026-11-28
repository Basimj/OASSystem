using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Reports;
using OAS.UiLib.Core.Models;
namespace OAS.Client.Accounting.Pages;

public partial class CashBankReportPage
{
    [Inject] private OasApiClient Api { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    private sealed record Filters
    {
        public DateTime From { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime To { get; set; } = DateTime.Today;
        public string Mode { get; set; } = "balances";
        public string Kind { get; set; } = "all";
        public string Account { get; set; } = "";
        public Guid? CashBoxId { get; set; }
        public Guid? Currency { get; set; }
        public Guid? DisplayCurrency { get; set; }
        public string Activity { get; set; } = "all";
        public string MovementType { get; set; } = "";
        public string ShiftState { get; set; } = "all";
        public string Settlement { get; set; } = "all";
        public bool IncludeZero { get; set; }
        public string Operator { get; set; } = "";
    }
    private static readonly Dictionary<string,string> Modes = new() { ["balances"]="إجمالي الأرصدة", ["boxes"]="ملخص الصناديق التشغيلية", ["cash"]="حركة الصناديق", ["bank"]="حركة البنوك", ["currency"]="الأرصدة حسب العملة", ["transfers"]="التحويلات المالية", ["shifts"]="الورديات", ["differences"]="العجز والزيادة" };
    private static readonly string[] DefaultMetricLabels = ["الرصيد الافتتاحي", "إجمالي الوارد", "إجمالي الصادر", "أرصدة الصناديق", "أرصدة البنوك", "إجمالي السيولة"];
    private static readonly string[] Tones = ["blue","green","red","orange","purple","green"];
    private static readonly string[] Icons = ["fa-solid fa-wallet","fa-solid fa-arrow-down","fa-solid fa-arrow-up","fa-solid fa-money-bill","fa-solid fa-landmark","fa-solid fa-coins"];
    private Filters _draft = new(), _applied = new();
    private CashBankReportDto? _report;
    private IReadOnlyList<CashBankReportRow> _visible = [];
    private IReadOnlyList<CashBankReportAccountDto> _accounts = [];
    private IReadOnlyList<CashBankReportCurrencyDto> Currencies = [];
    private IReadOnlyList<CashBankReportOperatorDto> Operators = [];
    private IReadOnlyList<UiSelectOption> OperatorOptions => new[] { new UiSelectOption("", "الكل") }.Concat(Operators.Select(o => new UiSelectOption(o.Id.ToString(), o.Name))).ToArray();
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
    private string EffectiveKind => _draft.Mode is "cash" or "shifts" or "differences" or "boxes" ? "cash" : _draft.Mode == "bank" ? "bank" : _draft.Kind;
    private IReadOnlyList<UiSelectOption> AccountOptions => new[] { new UiSelectOption("", "الكل") }.Concat(_accounts.Where(a => (EffectiveKind == "all" || a.Kind == EffectiveKind)
        && (EffectiveKind == "bank" || string.IsNullOrEmpty(_draft.MovementType) || a.MovementType?.ToString() == _draft.MovementType)
        && (!_draft.CashBoxId.HasValue || a.CashBoxId == _draft.CashBoxId) && (!_draft.Currency.HasValue || a.CurrencyId == _draft.Currency) && (_draft.Activity == "all" || a.IsActive == (_draft.Activity == "active")))
        .Select(a => new UiSelectOption(a.AccountId.ToString(), $"{a.Code} - {a.Name} ({Currencies.FirstOrDefault(c=>c.Id==a.CurrencyId)?.Code??"غير محددة"})"))).ToArray();
    private IEnumerable<CashBankReportAccountDto> BoxOptions => _accounts.Where(a=>a.CashBoxId.HasValue).GroupBy(a=>a.CashBoxId).Select(g=>g.First());
    private IReadOnlyList<UiSelectOption> BoxPickerOptions => new[] { new UiSelectOption("", "الكل") }
        .Concat(BoxOptions.OrderBy(a=>a.CashBoxName??a.Name).Select(a=>new UiSelectOption(a.CashBoxId!.Value.ToString(),a.CashBoxName??a.Name))).ToArray();
    private void OnBox(string? value) { _draft.CashBoxId=Guid.TryParse(value,out var id)?id:null; ClearAccount(); }
    private string[] Chips => [$"نوع التقرير: {Modes[_applied.Mode]}", $"من: {_applied.From:dd/MM/yyyy}", $"إلى: {_applied.To:dd/MM/yyyy}",
        $"الصندوق: {BoxOptions.FirstOrDefault(a=>a.CashBoxId==_applied.CashBoxId)?.CashBoxName??"الكل"}", $"الحساب: {_accounts.FirstOrDefault(a=>a.AccountId.ToString()==_applied.Account)?.Name??"الكل"}", $"نوع الحساب: {(_applied.Kind=="cash"?"صناديق":_applied.Kind=="bank"?"بنوك":"الكل")}",
        $"نوع حركة الصندوق: {(_applied.MovementType switch { "1"=>"وارد فقط", "2"=>"صادر فقط", "3"=>"وارد وصادر", _=>"الكل" })}",
        $"عملة الحساب: {Currencies.FirstOrDefault(c=>c.Id==_applied.Currency)?.Code??"الكل"}", $"حالة الحساب: {(_applied.Activity=="active"?"نشط":_applied.Activity=="inactive"?"غير نشط":"الكل")}",
        _applied.Mode is "shifts" or "differences" ? $"حالة الوردية: {ShiftLabel(_applied.ShiftState)} | التسوية: {SettlementLabel(_applied.Settlement)} | المستخدم: {Operators.FirstOrDefault(o=>o.Id.ToString()==_applied.Operator)?.Name??"الكل"}" : $"عرض الدفتري: {_displayCode}"];
    private static string ShiftLabel(string s) => s switch { "open"=>"مفتوحة", "closing"=>"قيد الإغلاق", "closed"=>"مغلقة", "approved"=>"معتمدة", _=>"الكل" };
    private static string SettlementLabel(string s) => s switch { "settled"=>"مسوّاة", "unsettled"=>"غير مسوّاة", _=>"الكل" };
    protected override Task OnInitializedAsync() => ApplyAsync();
    private void ClearAccount() { if(!AccountOptions.Any(x=>x.Value==_draft.Account)) _draft.Account=""; }
    private void OnAccount(string? value) => _draft.Account = value ?? "";
    private void OnOperator(string? value) => _draft.Operator = value ?? "";
    private void KindChanged() { if (EffectiveKind == "bank") { _draft.MovementType = ""; _draft.CashBoxId = null; } ClearAccount(); }
    private void ModeChanged() { _draft.MovementType = ""; _draft.Operator = ""; _draft.Kind = "all"; _draft.CashBoxId = null; ClearAccount(); _draft.ShiftState = "all"; _draft.Settlement = "all"; }
    private async Task ResetAsync() { if(_busy)return; _draft=new(); _search=""; await ApplyAsync(); }
    private async Task ApplyAsync()
    {
        if(_busy)return;
        _validation=null; var f=_draft with { Kind = EffectiveKind };
        if(f.From>f.To || f.To.Date==DateTime.MaxValue.Date){_validation="حدد فترة صحيحة.";return;}
        _busy=true; _error=null;
        try
        {
            var query=$"api/accounting/reports/cash-bank?fromDate={f.From:yyyy-MM-dd}&toDate={f.To:yyyy-MM-dd}&mode={f.Mode}&kind={f.Kind}&activity={f.Activity}&includeZero={f.IncludeZero.ToString().ToLowerInvariant()}&shiftState={f.ShiftState}&settlementState={f.Settlement}";
            if (f.Kind != "bank" && byte.TryParse(f.MovementType, out var movementType)) query += $"&movementType={movementType}";
            if(f.CashBoxId is Guid boxId && f.Kind != "bank") query += $"&cashBoxId={boxId:D}";
            if(Guid.TryParse(f.Account,out var id))query+=$"&accountId={id:D}";
            if(Guid.TryParse(f.Operator,out var actor))query+=$"&openedBy={actor:D}";
            if(f.Currency is Guid currency)query+=$"&currencyId={currency:D}";
            var report=await Api.GetAsync<CashBankReportDto>(query) ?? throw new InvalidOperationException("لم يرجع الخادم بيانات التقرير.");
            _accounts=report.Accounts; Currencies=report.Currencies; Operators=report.Operators;
            decimal rate=1; var code=report.BaseCurrencyCode; int decimals=report.BaseDecimalPlaces;
            if(f.Mode is not ("shifts" or "differences") && f.DisplayCurrency is Guid display && display!=report.BaseCurrencyId)
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
            case "shift": Nav.NavigateTo($"/accounting/cash-shifts?shiftId={id:D}");break;
            case "transfer": Nav.NavigateTo($"/accounting/cash-transfers?number={Uri.EscapeDataString(c.Text??"")}");break;
        }
    }
    private IEnumerable<CashBankReportRow> AllExportRows => _report is null ? [] : _report.TotalRow is { } total ? _report.Rows.Append(total) : _report.Rows;
    private string Subtitle=>string.Join(" | ",Chips)+$" | تضمين الحسابات الصفرية: {(_applied.IncludeZero?"نعم":"لا")}"+" | القيم الدفترية بعملة العرض بسعر نهاية الفترة. المجاميع تشمل كامل نتائج الفلاتر ولا تتأثر بالبحث. | "+string.Join(" | ",_report?.Warnings??[]);
    private async Task ExportAsync()
    {
        if(!CanExport)return;
        try
        {
            var rows=AllExportRows.Select(r=>r.Cells.Select(ExportValue).ToArray());
            var totals=_report!.Metrics.Select(m=>new object?[]{m.Label,ExportValue(m.Value)});
            var bytes=CustomerDebtsWorkbook.Create(Subtitle,Headers,rows.Concat(totals),sheetName:"الصناديق والبنوك");
            await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/customer-debts-export.js");
            await module.InvokeVoidAsync("saveWorkbook",Convert.ToBase64String(bytes),"cash-bank.xlsx");
        }
        catch(Exception ex){_validation="تعذر التصدير: "+ex.Message;}
    }
    private async Task PrintAsync()
    {
        if(!CanExport)return;
        try
        {
            await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/cash-bank-report-print.js");
            await module.InvokeVoidAsync("printReport",Modes[_applied.Mode],Subtitle,Headers,AllExportRows.Select(r=>r.Cells.Select(Format).ToArray()).ToArray(),_report!.Metrics.Select(m=>new[]{m.Label,Format(m.Value)}).ToArray());
        }
        catch(Exception ex){_validation="تعذرت الطباعة: "+ex.Message;}
    }
}
