using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Pages;
using OAS.Client.Accounting.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Reports;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Sales.Pages;

public partial class SalesReportsPage
{
    [Inject] private OasApiClient Api { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    private static readonly Dictionary<string,string> Modes = new() {
        ["Summary"]="إجمالي المبيعات", ["Invoice"]="فواتير المبيعات", ["Line"]="تفاصيل البنود", ["Customer"]="حسب العملاء", ["Product"]="حسب المنتجات",
        ["Category"]="حسب التصنيفات", ["ProductType"]="حسب أنواع المنتجات", ["Brand"]="حسب العلامات التجارية", ["Warehouse"]="حسب المخازن",
        ["Employee"]="حسب موظفي المبيعات", ["Operator"]="حسب المستخدمين", ["Period"]="حسب الفترة", ["Currency"]="حسب العملات", ["PaymentPlan"]="حسب خطة الدفع",
        ["Collections"]="التحصيلات والمتبقي", ["Return"]="مرتجعات المبيعات", ["Discounts"]="الخصومات", ["Taxes"]="الضرائب", ["Cost"]="تكلفة المبيعات", ["Profit"]="الأرباح والربحية" };
    private sealed record Filters
    {
        public string Mode { get; set; } = "Summary"; public string PeriodGrouping { get; set; } = "Day";
        public string Year { get; set; } = ""; public string Period { get; set; } = "";
        public DateTime From { get; set; } = new(DateTime.Today.Year,DateTime.Today.Month,1); public DateTime To { get; set; } = DateTime.Today;
        public string Customer { get; set; } = ""; public string Employee { get; set; } = ""; public string Product { get; set; } = ""; public string Category { get; set; } = "";
        public string ProductType { get; set; } = ""; public string Brand { get; set; } = ""; public string Warehouse { get; set; } = ""; public string Operator { get; set; } = "";
        public string Currency { get; set; } = ""; public string HistoricalBaseCurrency { get; set; } = "*"; public string Plan { get; set; } = ""; public string Settlement { get; set; } = ""; public Guid? DisplayCurrency { get; set; }
    }
    private Filters _draft = new(), _applied = new();
    private SalesReportDto? _report;
    private IReadOnlyList<SalesReportBreakdownDto> _visible = [];
    private IReadOnlyList<SalesReportOptionDto> _options = [];
    private IReadOnlyList<FiscalYearDto> _years = [];
    private IReadOnlyList<FiscalPeriodDto> _periods = [];
    private IReadOnlyList<CurrencyDto> _currencies = [];
    private bool _busy, _filtersOpen = true, _descending, _referencesLoaded;
    private string? _error, _validation, _referenceWarning;
    private string _search = "", _displayCode = "";
    private int _page = 1, _pageSize = 25, _sort = -1, _decimals = 4;
    private decimal _rate = 1m;
    private string BaseCode => _report?.BaseCurrencyCode ?? "العملة الأساسية";
    private bool CanExport => !_busy && _report is not null && _error is null;
    private string Status => _busy ? "جارٍ التحميل" : CanExport ? $"{_visible.Count} سجل" : "غير متاح";
    private int TotalPages => Math.Max(1, (_visible.Count + _pageSize - 1) / _pageSize);
    private IEnumerable<SalesReportBreakdownDto> PageRows => _visible.Skip((_page-1)*_pageSize).Take(_pageSize);
    private IEnumerable<FiscalPeriodDto> PeriodOptions => _periods.Where(p=>p.FiscalYearId.ToString()==_draft.Year).OrderBy(p=>p.StartDate);
    private string[] Chips => [$"نوع التقرير: {Modes[_applied.Mode]}", $"السنة: {_years.FirstOrDefault(y=>y.Id.ToString()==_applied.Year)?.Name??"نطاق مخصص"}", $"الفترة: {_periods.FirstOrDefault(p=>p.Id.ToString()==_applied.Period)?.Name??"الكل"}", $"من: {_applied.From:dd/MM/yyyy}", $"إلى: {_applied.To:dd/MM/yyyy}", $"الأساسية التاريخية: {BaseCode}", $"عملة العرض: {_displayCode}", "المرحلة فقط"];
    private IEnumerable<SalesReportOptionDto> Pick(string kind) => _options.Where(o => o.Kind == kind &&
        (kind is not ("product" or "category") || string.IsNullOrEmpty(_draft.ProductType) || o.TypeKey==_draft.ProductType) &&
        (kind!="product" || string.IsNullOrEmpty(_draft.Category) || o.ParentKey==_draft.Category));
    private IReadOnlyList<UiSelectOption> Options(string kind) => new[]{new UiSelectOption("", "الكل")}.Concat(Pick(kind).Select(o=>new UiSelectOption(o.Key,o.Label))).ToArray();
    private void TypeChanged(){_draft.Category=""; _draft.Product="";}
    private void CategoryChanged()=>_draft.Product="";
    private void HistoricalBaseChanged(){_draft.DisplayCurrency=null;}
    private IEnumerable<string> HistoricalBaseOptions => _options.Where(o=>o.Kind=="historicalBaseCurrency").Select(o=>o.Key)
        .Concat(_draft.HistoricalBaseCurrency=="*" ? Array.Empty<string>() : new[]{_draft.HistoricalBaseCurrency}).Distinct().OrderBy(x=>x);
    private void ModeChanged(){ if(_draft.Mode=="Collections"){_draft.Product="";_draft.Category="";_draft.ProductType="";_draft.Brand="";_draft.Warehouse="";} else _draft.Settlement=""; }
    protected override Task OnInitializedAsync()=>ApplyAsync();
    private static async Task<IReadOnlyList<T>> All<T>(Func<PageRequest,Task<PagedResult<T>>> load)
    { var result = new List<T>(); for(var n=1;;n++){var p=await load(new PageRequest{PageNumber=n,PageSize=100}); result.AddRange(p.Items); if(!p.HasNextPage || p.Items.Count==0)return result;} }
    private async Task LoadReferencesAsync()
    {
        try { _years=(await All(p=>Accounting.GetFiscalYearsPageAsync(p))).OrderByDescending(x=>x.StartDate).ToArray(); _periods=await All(p=>Accounting.GetFiscalPeriodsPageAsync(p)); }
        catch(ApiClientException ex) when(ex.StatusCode is 401 or 403){_referenceWarning="لا تتوفر صلاحية عرض السنة أو الفترة؛ استخدم نطاق التاريخ المخصص.";}
        try { _currencies=await All(p=>Accounting.GetCurrenciesPageAsync(p)); }
        catch(ApiClientException ex) when(ex.StatusCode is 401 or 403){_referenceWarning=(_referenceWarning??"")+" لا تتوفر صلاحية عرض العملات؛ استخدم العملة الأساسية.";}
        _referencesLoaded=true;
    }
    private void OnYear(ChangeEventArgs e){_draft.Year=e.Value?.ToString()??"";_draft.Period="";SetYearDates();}
    private void SetYearDates(){if(_years.FirstOrDefault(y=>y.Id.ToString()==_draft.Year) is {} y){_draft.From=y.StartDate.ToDateTime(TimeOnly.MinValue);_draft.To=y.EndDate.ToDateTime(TimeOnly.MinValue);}}
    private void OnPeriod(ChangeEventArgs e){_draft.Period=e.Value?.ToString()??"";if(PeriodOptions.FirstOrDefault(p=>p.Id.ToString()==_draft.Period) is {} p){_draft.From=p.StartDate.ToDateTime(TimeOnly.MinValue);_draft.To=p.EndDate.ToDateTime(TimeOnly.MinValue);}else SetYearDates();}
    private async Task ResetAsync(){if(_busy)return;_draft=new();_search="";await ApplyAsync();}
    private async Task ApplyAsync()
    {
        if(_busy)return; _validation=null;
        if(_draft.From>_draft.To || _draft.To.Date==DateTime.MaxValue.Date){_validation="حدد فترة صحيحة.";return;}
        _busy=true;_error=null;var f=_draft with {};
        try
        {
            if(!_referencesLoaded)await LoadReferencesAsync();
            var mode=f.Mode=="Period"?f.PeriodGrouping:f.Mode;
            var query=$"api/sales/reports?fromDate={f.From:yyyy-MM-dd}&toDate={f.To:yyyy-MM-dd}&groupBy={mode}&pageSize=1";
            foreach(var pair in new Dictionary<string,string>{["customerId"]=f.Customer,["employeeId"]=f.Employee,["productVariantId"]=f.Product,["categoryId"]=f.Category,["productTypeId"]=f.ProductType,["brandId"]=f.Brand,["warehouseId"]=f.Warehouse,["operatorKey"]=f.Operator,["currencyCode"]=f.Currency,["paymentPlan"]=f.Plan,["settlementStatus"]=f.Settlement,["historicalBaseCurrencyCode"]=f.HistoricalBaseCurrency})
                if(!string.IsNullOrEmpty(pair.Value))query+=$"&{pair.Key}={Uri.EscapeDataString(pair.Value)}";
            var report=await Api.GetAsync<SalesReportDto>(query) ?? throw new InvalidOperationException("لم يرجع الخادم بيانات التقرير.");
            var code=report.BaseCurrencyCode; var decimals=(int)(_currencies.FirstOrDefault(x=>x.Code==code)?.DecimalPlaces??4); decimal rate=1;
            if(f.DisplayCurrency is Guid id && _currencies.FirstOrDefault(c=>c.Id==id) is {} display && display.Code!=code)
            {
                // Current exchange table is relative to the configured base currency, not an arbitrary historic base.
                var settings=await Accounting.GetAccountingSettingsAsync();
                if(settings is null || settings.BaseCurrencyCode!=code)
                {
                    report=report with {Warnings=report.Warnings.Concat(new[]{"عملة العرض المختارة لا يمكن تحويلها بأمان من العملة الأساسية التاريخية؛ تعرض القيم بعملتها التاريخية دون تحويل."}).ToArray()};
                    f.DisplayCurrency=null; _draft.DisplayCurrency=null;
                }
                else
                {
                    var fx=await Accounting.GetEffectiveExchangeRateAsync(id,DateOnly.FromDateTime(f.To));
                    if(fx is null || fx.Rate<=0)throw new InvalidOperationException("لا يوجد سعر صرف معتمد لعملة العرض في نهاية الفترة.");
                    rate=fx.Rate;code=display.Code;decimals=display.DecimalPlaces;
                }
            }
            _rate=rate;_displayCode=code;_decimals=decimals;_report=report;_options=report.Options;_applied=f;_sort=-1;_descending=false;RefreshRows();
        }
        catch(Exception ex){_error="تعذر تحميل التقرير: "+ex.Message;_report=null;_visible=[];_page=1;}
        finally{_busy=false;}
    }
    private sealed record Column(string Key,string Label,Func<SalesReportBreakdownDto,object?> Value);
    private decimal? Money(decimal? n)=>n.HasValue?Math.Round(n.Value/_rate,_decimals,MidpointRounding.AwayFromZero):null;
    private Column M(string key,string label,Func<SalesReportTotalsDto,decimal?> value)=>new(key,label,r=>Money(value(r.Totals)));
    private Column R(string key,string label,Func<SalesReportBreakdownDto,decimal?> value)=>new(key,label,r=>Money(value(r)));
    private Column[] Columns
    {
        get
        {
            var mode=_applied.Mode;var columns=new List<Column>{new("label",mode switch {"Invoice" or "Return" or "Collections" or "Discounts" or "Taxes" or "Line"=>"رقم المستند", "Summary"=>"البيان", _=>"المجموعة"},r=>r.Label)};
            bool detail=mode is "Invoice" or "Return" or "Collections" or "Discounts" or "Taxes" or "Line";
            if(detail){columns.Add(new("date","تاريخ الترحيل",r=>r.PostingDate?.ToString("yyyy-MM-dd")));columns.Add(new("customer","العميل",r=>r.CustomerName));columns.Add(new("kind","النوع",r=>r.DocumentKind=="SalesInvoice"?"فاتورة":"مرتجع"));columns.Add(new("currency","عملة المستند",r=>r.CurrencyCode));}
            if(mode=="Line"){columns.Add(new("line","رقم البند",r=>r.LineNumber));columns.Add(new("item","الصنف",r=>r.ItemName));columns.Add(new("saleWarehouse","مخزن البيع",r=>r.SaleWarehouseName));columns.Add(new("returnWarehouse","مخزن الاسترجاع المثبت بالقيد",r=>r.ReturnWarehouseName));columns.Add(new("unit","الوحدة",r=>r.UnitName));columns.Add(new("sold","الكمية المباعة",r=>r.Totals.SoldQuantity));columns.Add(new("returnedQty","الكمية المرتجعة",r=>r.Totals.ReturnedQuantity));}
            if(mode=="Product"){columns.Add(new("unit","الوحدة",r=>r.UnitName));columns.Add(new("sold","الكمية المباعة",r=>string.IsNullOrEmpty(r.UnitName)?null:(decimal?)r.Totals.SoldQuantity));columns.Add(new("returnedQty","الكمية المرتجعة",r=>string.IsNullOrEmpty(r.UnitName)?null:(decimal?)r.Totals.ReturnedQuantity));}
            if(mode=="Collections")columns.AddRange([R("invoiceTotal","إجمالي الفاتورة مع الضريبة",r=>r.InvoiceTotalBase),R("returnTotal","المرتجعات حتى نهاية الفترة",r=>r.ReturnedTotalBase),R("allocated","الدفعات المخصصة",r=>r.AllocatedBase),R("credit","الأرصدة الدائنة المطبقة",r=>r.CreditAppliedBase),R("outstanding","المتبقي",r=>r.OutstandingBase)]);
            else if(mode=="Discounts")columns.AddRange([M("invoice","إيراد الفاتورة",t=>t.InvoiceRevenueBase),M("discount","خصومات الفواتير",t=>t.InvoiceDiscountsBase),M("returns","المرتجعات",t=>t.ReturnedRevenueBase),M("net","صافي الإيراد",t=>t.NetRevenueBase)]);
            else if(mode=="Taxes")columns.AddRange([M("net","صافي الإيراد",t=>t.NetRevenueBase),M("tax","الضريبة الصافية",t=>t.NetSalesTaxBase)]);
            else if(mode=="Profit")columns.AddRange([M("net","صافي الإيراد",t=>t.NetRevenueBase),M("cost","التكلفة الصافية",t=>t.IsProfitAvailable?t.NetCogsBase:null),M("profit","الربح الإجمالي",t=>t.IsProfitAvailable?t.GrossProfitBase:null),new("margin","هامش الربح",r=>r.Totals.IsGrossMarginAvailable?(decimal?)r.Totals.GrossMarginPercent:null)]);
            else if(mode=="Cost")columns.AddRange([M("invoice","المبيعات",t=>t.InvoiceRevenueBase),M("returns","المرتجعات",t=>t.ReturnedRevenueBase),M("cost","التكلفة الصافية",t=>t.IsProfitAvailable?t.NetCogsBase:null),new("missing","بنود تكلفة غير مكتملة",r=>r.Totals.MissingCostCount)]);
            else
            {
                columns.AddRange([M("invoice","المبيعات",t=>t.InvoiceRevenueBase),M("returns","المرتجعات",t=>t.ReturnedRevenueBase),M("net","صافي المبيعات",t=>t.NetRevenueBase),M("cost","تكلفة المبيعات",t=>t.IsProfitAvailable?t.NetCogsBase:null),M("profit","الربح الإجمالي",t=>t.IsProfitAvailable?t.GrossProfitBase:null),new("margin","هامش الربح",r=>r.Totals.IsGrossMarginAvailable?(decimal?)r.Totals.GrossMarginPercent:null)]);
            }
            if(detail)columns.Add(new("journal","القيد",r=>r.JournalId.HasValue?"عرض القيد":null));
            return columns.ToArray();
        }
    }
    private object? TotalValue(string key)
    {
        if(_report is null)return null; var t=_report.Totals;var rows=_report.Breakdown;
        return key switch {"label"=>"الإجمالي", "invoice"=>Money(t.InvoiceRevenueBase), "returns"=>Money(t.ReturnedRevenueBase),"net"=>Money(t.NetRevenueBase),"cost"=>Money(t.IsProfitAvailable?t.NetCogsBase:null),"profit"=>Money(t.IsProfitAvailable?t.GrossProfitBase:null),"margin"=>t.IsGrossMarginAvailable?(decimal?)t.GrossMarginPercent:null,"discount"=>Money(t.InvoiceDiscountsBase),"tax"=>Money(t.NetSalesTaxBase),"missing"=>t.MissingCostCount,"invoiceTotal"=>Money(rows.Sum(x=>x.InvoiceTotalBase)),"returnTotal"=>Money(rows.Sum(x=>x.ReturnedTotalBase)),"allocated"=>Money(rows.Sum(x=>x.AllocatedBase)),"credit"=>Money(rows.Sum(x=>x.CreditAppliedBase)),"outstanding"=>Money(rows.Sum(x=>x.OutstandingBase)),_=>null};
    }
    private string Format(object? value,bool percent=false)=>value switch { null=>"—", decimal d=>d.ToString("#,##0.####;(#,##0.####);0",CultureInfo.InvariantCulture)+(percent?"%":""), int i=>i.ToString("N0",CultureInfo.InvariantCulture), _=>value.ToString()??"—" };
    private (string Label,string Value,string Tone,string Icon)[] Metrics
    {
        get
        {
            string V(string k,bool percent=false)=>CanExport?Format(TotalValue(k),percent):"—";
            if(_applied.Mode=="Collections")return [("إجمالي الفواتير",V("invoiceTotal"),"blue","fa-solid fa-file-invoice"),("المرتجعات",V("returnTotal"),"red","fa-solid fa-undo"),("الدفعات المخصصة",V("allocated"),"green","fa-solid fa-wallet"),("الأرصدة الدائنة المطبقة",V("credit"),"purple","fa-solid fa-link"),("المتبقي",V("outstanding"),"orange","fa-solid fa-coins"),("عدد الفواتير",CanExport?(_report!.Totals.InvoiceCount.ToString("N0")):"—","blue","fa-solid fa-list")];
            return [("إجمالي المبيعات",V("invoice"),"blue","fa-solid fa-cart-shopping"),("المرتجعات",V("returns"),"red","fa-solid fa-undo"),("صافي المبيعات",V("net"),"green","fa-solid fa-wallet"),("تكلفة المبيعات",V("cost"),"orange","fa-solid fa-box"),("الربح الإجمالي",V("profit"),"green","fa-solid fa-chart-line"),("هامش الربح",V("margin",true),"purple","fa-solid fa-percent")];
        }
    }
    private void RefreshRows()
    {
        IEnumerable<SalesReportBreakdownDto> rows=_report?.Breakdown??[]; var cols=Columns;
        if(!string.IsNullOrWhiteSpace(_search))rows=rows.Where(r=>cols.Any(c=>Format(c.Value(r),c.Key=="margin").Contains(_search.Trim(),StringComparison.CurrentCultureIgnoreCase)));
        if(_sort>=0 && _sort<cols.Length){var c=cols[_sort];rows=_descending?rows.OrderByDescending(c.Value):rows.OrderBy(c.Value);}
        _visible=rows.ToArray();_page=1;
    }
    private void OnSearch(string value){_search=value??"";RefreshRows();}
    private void SortBy(int i){_descending=_sort==i?!_descending:false;_sort=i;RefreshRows();}
    private void GoPage(int page)=>_page=Math.Clamp(page,1,TotalPages);
    private void ChangePageSize(ChangeEventArgs e){if(int.TryParse(e.Value?.ToString(),out var n))_pageSize=n;_page=1;}
    private void OpenDocument(SalesReportBreakdownDto r){if(r.DocumentId is Guid id)Nav.NavigateTo(r.DocumentKind=="SalesInvoice"?$"/sales/invoices?invoiceId={id:D}":$"/sales/returns?returnId={id:D}");}
    private void OpenJournal(SalesReportBreakdownDto r){if(r.JournalId is Guid id)Nav.NavigateTo($"/accounting/journals?journalId={id:D}");}
    private string Subtitle=>string.Join(" | ",Chips)+" | الإجماليات لجميع الفلاتر (البحث للعرض فقط)"+(_rate!=1?$" | سعر عرض نهاية الفترة: {_rate}":"");
    private IEnumerable<object?[]> ExportRows(){var cols=Columns;foreach(var row in _report!.Breakdown)yield return cols.Select(c=>c.Value(row)).ToArray();yield return cols.Select(c=>TotalValue(c.Key)).ToArray();}
    private async Task ExportAsync()
    {
        if(!CanExport)return;
        try{var bytes=CustomerDebtsWorkbook.Create(Subtitle,Columns.Select(c=>c.Label).ToArray(),ExportRows().Concat(Metrics.Select(m=>new object?[]{m.Label,m.Value})).Concat(_report!.Warnings.Select(w=>new object?[]{"ملاحظة",w})),sheetName:"المبيعات"); await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/customer-debts-export.js"); await module.InvokeVoidAsync("saveWorkbook",Convert.ToBase64String(bytes),"sales-report.xlsx");}
        catch(Exception ex){_validation="تعذر التصدير: "+ex.Message;}
    }
    private async Task PrintAsync()
    {
        if(!CanExport)return;
        try{var cols=Columns;await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/sales-report-print.js");await module.InvokeVoidAsync("printReport",Modes[_applied.Mode],Subtitle,cols.Select(c=>c.Label).ToArray(),ExportRows().Select(row=>row.Select((v,i)=>Format(v,cols[i].Key=="margin")).ToArray()).ToArray(),Metrics.Select(m=>new[]{m.Label,m.Value}).Concat(_report!.Warnings.Select(w=>new[]{"ملاحظة",w})).ToArray());}
        catch(Exception ex){_validation="تعذرت الطباعة: "+ex.Message;}
    }
}
