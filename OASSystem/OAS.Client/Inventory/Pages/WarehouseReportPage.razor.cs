using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Pages;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Reports;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Inventory.Pages;

public partial class WarehouseReportPage
{
    [Inject] private IAccountingClientService Accounting {get;set;}=default!;
    [Inject] private OasApiClient Api {get;set;}=default!;
    [Inject] private IJSRuntime JS {get;set;}=default!;
    [Inject] private IApiFeedbackService ApiFeedback {get;set;}=default!;
    private sealed record Filters
    {
        public string Mode {get;set;}="summary";
        public string Warehouse {get;set;}="";
        public string Variant {get;set;}="";
        public string Product {get;set;}="";
        public string Category {get;set;}="";
        public DateTime From {get;set;}=new(DateTime.UtcNow.Year,1,1);
        public DateTime To {get;set;}=DateTime.UtcNow.Date;
        public Guid? Currency {get;set;}
        public bool IncludeInactive {get;set;}=true;
        public bool HideZero {get;set;}
        public string StockState {get;set;}="all";
        public string Source {get;set;}="";
        public string Destination {get;set;}="";
        public int TransferStatus {get;set;}
        public string CountNumber {get;set;}="";
        public int CountStatus {get;set;}
        public string Variance {get;set;}="all";
    }
    private sealed record Col(string Key,string Label,string Kind="text",string Css="",bool Sum=false);
    private sealed record MetricDef(string Label,string Value,string Tone,string Icon);
    private sealed record Row(Guid WarehouseId,Guid? VariantId,Guid? UnitId,Guid? DocumentId,Dictionary<string,object?> Cells,string Search, int Status=0, Guid? Source=null, Guid? Destination=null);
    private Filters _draft=new(), _applied=new();
    private WarehouseReportDto? _data;
    private bool _busy=true,_filtersOpen=true,_descending;
    private string _search="",_sortKey="warehouseCode",_baseName="العملة الأساسية",_currencyName="العملة الأساسية";
    private string? _error,_validation;
    private Guid? _baseId;
    private decimal _rate=1;
    private int _page=1,_pageSize=25;
    private List<CurrencyDto> _currencies=[];
    private List<Row> _rows=[];
    private static readonly Col[] SummaryCols=[new("warehouseCode","رمز المخزن"),new("warehouseName","اسم المخزن"),new("items","عدد الأصناف","int","",true),new("availableCount","الأصناف المتوفرة","int","pos",true),new("outCount","الأصناف النافدة","int","neg",true),new("reservedValue","قيمة المخزون المحجوز","money","",true),new("availableValue","قيمة المخزون المتاح","money","",true),new("value","إجمالي قيمة المخزون","money","",true),new("last","آخر حركة"),new("status","الحالة")];
    private static readonly Col[] ItemCols=[new("code","رمز الصنف"),new("name","اسم الصنف"),new("unit","الوحدة")];
    private static readonly Col[] DetailCols=[new("code","رمز الصنف"),new("name","اسم الصنف"),new("product","المنتج"),new("unit","الوحدة"),new("onHand","الرصيد الفعلي","qty","",true),new("reserved","المحجوز","qty","neg",true),new("available","المتاح","qty","pos",true),new("onOrder","قيد الطلب","qty","info",true),new("avgCost","متوسط التكلفة","money"),new("value","قيمة المخزون","money","",true),new("last","آخر حركة")];
    private Col[] FlowCols => [..ItemCols, new("opening","رصيد الافتتاح",FlowKind,"",true),new("purchase","المشتريات",FlowKind,"pos",true),new("salesReturn","مرتجع المبيعات",FlowKind,"pos",true),new("transferIn","تحويل وارد",FlowKind,"pos",true),new("otherIn","توريد وتسويات",FlowKind,"pos",true),new("sales","المبيعات",FlowKind,"neg",true),new("purchaseReturn","مرتجع المشتريات",FlowKind,"neg",true),new("transferOut","تحويل صادر",FlowKind,"neg",true),new("otherOut","صرف وتسويات",FlowKind,"neg",true),new("closing","الرصيد الختامي",FlowKind,"",true)];
    private static readonly Col[] TransferCols=[new("number","رقم التحويل"),new("date","التاريخ"),new("source","المخزن المصدر"),new("destination","المخزن المستلم"),new("code","رمز الصنف"),new("name","اسم الصنف"),new("unit","الوحدة"),new("quantity","الكمية","qty","",true),new("cost","تكلفة التحويل المرحلة","money","",true),new("status","الحالة")];
    private static readonly Col[] CountCols=[new("number","رقم الجرد"),new("date","تاريخ الجرد"),new("code","رمز الصنف"),new("name","اسم الصنف"),new("unit","الوحدة"),new("system","الرصيد الدفتري","qty"),new("counted","الكمية بالجرد","qty"),new("difference","فرق الكمية","qty"),new("avgCost","متوسط التكلفة","money"),new("variance","قيمة الفرق","money","",true),new("status","الحالة")];
    private Col[] Columns=>_applied.Mode switch{"details"=>DetailCols,"qty" or "cost"=>FlowCols,"transfers"=>TransferCols,"counts"=>CountCols,_=>IsCurrent(_applied)?SummaryCols:SummaryCols.Select(c=>c.Key=="availableCount"?c with {Label="أصناف برصيد موجب"}:c).ToArray()};
    private string FlowKind=>_applied.Mode=="qty"?"qty":"money";
    private int IdentityCount=>_applied.Mode switch{"summary"=>2,"details"=>4,"transfers"=>7,"counts"=>5,_=>3};
    private bool Grouped=>_applied.Mode is "details" or "qty" or "cost" or "counts";
    private static bool IsRange(string mode)=>mode is "qty" or "cost" or "transfers" or "counts";
    private static bool IsBalance(string mode)=>mode is "summary" or "details";
    private static bool IsCurrent(Filters f)=>IsBalance(f.Mode)&&f.To.Date==DateTime.UtcNow.Date;
    private bool CanExport=>!_busy&&_error is null&&_rows.Count>0;
    private int TotalPages=>Math.Max(1,(int)Math.Ceiling(_rows.Count/(double)_pageSize));
    private IEnumerable<Row> PageRows=>_rows.Skip((_page-1)*_pageSize).Take(_pageSize);
    private IReadOnlyList<WarehouseReportWarehouse> Warehouses=>_data?.Warehouses??[];
    private IReadOnlyList<MovementReportOption> Categories=>_data?.Categories??[];
    private IReadOnlyList<MovementReportItem> Items=>_data?.Items??[];
    private IReadOnlyList<UiSelectOption> WarehouseOptions=>[new("","جميع المخازن"),..Warehouses.Where(w=>_draft.IncludeInactive||w.IsActive).Select(w=>new UiSelectOption(w.Id.ToString(),$"{w.Code} - {w.Name}"))];
    private IReadOnlyList<UiSelectOption> VariantOptions=>[new("","جميع الأصناف"),..Items.Where(i=>_draft.Category==""||i.CategoryId.ToString()==_draft.Category).Select(i=>new UiSelectOption(i.Id.ToString(),$"{i.Code} - {i.Name}"))];
    private IReadOnlyList<UiSelectOption> ProductOptions=>Items.Where(i=>i.Id.ToString()==_draft.Variant).Select(i=>new UiSelectOption(i.ProductId.ToString(),$"{i.ProductCode} - {i.ProductName}")).ToArray();
    private void OnWarehouse(string? v)=>_draft.Warehouse=v??"";
    private void OnVariant(string? v){_draft.Variant=v??"";_draft.Product=Items.FirstOrDefault(i=>i.Id.ToString()==_draft.Variant)?.ProductId.ToString()??"";}
    private void OnProduct(string? v){if(ProductOptions.Any(i=>i.Value==v))_draft.Product=v!;}
    private void OnCategory(ChangeEventArgs e){_draft.Category=e.Value?.ToString()??"";if(!VariantOptions.Any(x=>x.Value==_draft.Variant))OnVariant("");}
    private string WarehouseName(Guid? id)=>Warehouses.FirstOrDefault(w=>w.Id==id)?.Name??"—";
    private string WarehouseCaption(Guid id)=>Warehouses.FirstOrDefault(w=>w.Id==id) is { } w?$"{w.Name} — {w.Code}":"مخزن غير محدد";
    private (string Label,string Value)[] Chips
    {
        get
        {
            var f=_applied;
            var list=new List<(string,string)>{("المخزن",Guid.TryParse(f.Warehouse,out var w)?WarehouseName(w):"الكل"),("نوع التقرير",ModeName(f.Mode)),("الصنف",Items.FirstOrDefault(i=>i.Id.ToString()==f.Variant)?.Name??"الكل"),("المنتج",Items.FirstOrDefault(i=>i.ProductId.ToString()==f.Product)?.ProductName??"الكل"),("التصنيف",Categories.FirstOrDefault(c=>c.Id.ToString()==f.Category)?.Name??"الكل"),(IsRange(f.Mode)?"الفترة":"إلى تاريخ",IsRange(f.Mode)?$"{f.From:dd/MM/yyyy} – {f.To:dd/MM/yyyy}":$"{f.To:dd/MM/yyyy}" )};
            if(f.Mode!="qty")list.Add(("العملة",_currencyName));
            if(IsBalance(f.Mode))list.Add(("المخزون",!IsCurrent(f)&&f.StockState=="available"?"رصيد موجب":StockName(f.StockState)));
            if(f.Mode=="transfers"){list.Add(("المصدر",Guid.TryParse(f.Source,out var s)?WarehouseName(s):"الكل"));list.Add(("المستلم",Guid.TryParse(f.Destination,out var d)?WarehouseName(d):"الكل"));list.Add(("الحالة",f.TransferStatus==0?"الكل":TransferState(f.TransferStatus)));}
            if(f.Mode=="counts"){list.Add(("الجرد",string.IsNullOrWhiteSpace(f.CountNumber)?"الكل":f.CountNumber));list.Add(("الحالة",f.CountStatus==0?"الكل":CountState(f.CountStatus)));list.Add(("الفرق",VarianceName(f.Variance)));}
            list.Add(("المخازن غير النشطة",f.IncludeInactive?"مضمنة":"مستبعدة"));
            if(f.Mode is "summary" or "details" or "qty" or "cost")list.Add(("الصفرية",f.HideZero?"مخفية":"ظاهرة"));
            return list.ToArray();
        }
    }
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var settings=await Accounting.GetAccountingSettingsAsync();_baseId=settings?.BaseCurrencyId;
            for(var p=1;;p++){var r=await Accounting.GetCurrenciesPageAsync(new PageRequest{PageNumber=p,PageSize=100});_currencies.AddRange(r.Items);if(!r.HasNextPage)break;}
            _baseName=_currencies.FirstOrDefault(x=>x.Id==_baseId)?.NameAr??settings?.BaseCurrencyCode??"العملة الأساسية";
        }
        catch { _currencies=[]; }
        _busy=false;await ApplyAsync();
    }
    private async Task ResetAsync(){_draft=new();_search="";await ApplyAsync();}
    private async Task OpenWarehouseAsync(Guid id){_draft=_applied with {Mode="details",Warehouse=id.ToString()};_search="";await ApplyAsync();}
    private async Task ApplyAsync()
    {
        if(_busy)return;_validation=null;
        var next=_draft with {};
        if(next.To.Year==9999||next.To.Date>DateTime.UtcNow.Date|| (IsRange(next.Mode)&&next.From.Date>next.To.Date)){_validation="تحقق من التاريخ والفترة المحددة.";return;}
        if(!IsCurrent(next)&&next.StockState=="reserved")next.StockState="all";
        _busy=true;_error=null;
        try
        {
            var from=DateOnly.FromDateTime(IsRange(next.Mode)?next.From:next.To);var to=DateOnly.FromDateTime(next.To);
            var rate=1m;var currency=_baseName;
            if(next.Mode!="qty"&&next.Currency is Guid id&&id!=_baseId)
            {
                var c=_currencies.FirstOrDefault(x=>x.Id==id&&x.IsActive)??throw new InvalidOperationException("العملة غير متاحة.");
                var r=await Accounting.GetEffectiveExchangeRateAsync(id,to,ExchangeRateType.Accounting);
                if(r is null||r.Rate<=0)throw new InvalidOperationException("لا يوجد سعر صرف محاسبي صالح.");
                rate=r.Rate;currency=$"{c.NameAr} ({c.Code})";
            }
            var url=$"api/inventory/reports/warehouses?mode={next.Mode}&fromDate={from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&toDate={to.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}&current={IsCurrent(next).ToString().ToLowerInvariant()}";
            foreach(var x in new[]{("warehouseId",next.Warehouse),("variantId",next.Variant),("productId",next.Product),("categoryId",next.Category)})if(Guid.TryParse(x.Item2,out var g))url+=$"&{x.Item1}={g:D}";
            _data=await Api.GetAsync<WarehouseReportDto>(url)??throw new InvalidOperationException("لم يرجع الخادم بيانات التقرير.");
            _applied=next;_rate=rate;_currencyName=currency;
            if(!Columns.Any(c=>c.Key==_sortKey)){_sortKey=Columns[0].Key;_descending=false;}
            BuildRows();
        }
        catch(ApiClientException ex){_error="تعذر تحميل بيانات التقرير.";_rows=[];ApiFeedback.Show(ex.Error);}
        catch(Exception ex){_error=ex.Message;_rows=[];}
        finally{_busy=false;}
    }
    private bool Allowed(Guid? id)=>id.HasValue&&Warehouses.Any(w=>w.Id==id&&(_applied.IncludeInactive||w.IsActive));
    private static decimal? SumKnown(IEnumerable<decimal?> values){var a=values.ToArray();return a.Any(v=>!v.HasValue)?null:a.Sum(v=>v.GetValueOrDefault());}
    private bool StockMatches(WarehouseReportStock s)
    {
        var available=s.Reserved.HasValue?s.OnHand-s.Reserved.Value:s.OnHand;
        return (_applied.StockState switch{"available"=>available>0,"out"=>s.OnHand<=0,"reserved"=>s.OnHand>0&&s.Reserved>=s.OnHand,_=>true})&&
            (!_applied.HideZero||s.OnHand!=0||s.Reserved.GetValueOrDefault()!=0||s.OnOrder.GetValueOrDefault()!=0||s.Value!=0);
    }
    private static decimal? ReservedValue(WarehouseReportStock s)=>s.Reserved.HasValue&&s.AverageCost.HasValue?s.Reserved*s.AverageCost:null;
    private void BuildRows()
    {
        if(_data is null||_error is not null)return;
        var items=Items.ToDictionary(i=>i.Id);var list=new List<Row>();
        Dictionary<string,object?> ItemCells(MovementReportItem i)=>new(){["code"]=i.Code,["name"]=i.Name,["product"]=i.ProductName,["unit"]=i.UnitName};
        string SearchItem(MovementReportItem i)=>$"{i.Code} {i.Name} {i.ProductCode} {i.ProductName} {i.UnitName}";
        var stocks=_data.Stocks.Where(s=>Allowed(s.WarehouseId)&&StockMatches(s)).ToArray();
        if(_applied.Mode=="summary")
        {
            var byWarehouse=stocks.ToLookup(s=>s.WarehouseId);
            foreach(var w in Warehouses.Where(w=>Allowed(w.Id)&&(_applied.Warehouse==""||w.Id.ToString()==_applied.Warehouse)))
            {
                var a=byWarehouse[w.Id].ToArray();
                if(a.Length==0&&(_applied.HideZero||_applied.StockState!="all"||_applied.Variant!=""||_applied.Category!=""))continue;
                var rv=IsCurrent(_applied)?SumKnown(a.Select(ReservedValue)):null;
                var value=SumKnown(a.Select(x=>x.Value));
                var availableCount=IsCurrent(_applied)?a.Count(x=>x.OnHand-x.Reserved.GetValueOrDefault()>0):a.Count(x=>x.OnHand>0);
                list.Add(new(w.Id,null,null,null,new(){["warehouseCode"]=w.Code,["warehouseName"]=w.Name,["items"]=a.Length,["availableCount"]=availableCount,["outCount"]=a.Count(x=>x.OnHand<=0),["reservedValue"]=rv,["availableValue"]=value-rv,["value"]=value,["last"]=a.Select(x=>x.LastMovement).DefaultIfEmpty().Max(),["status"]=w.IsActive?"نشط":"غير نشط"},$"{w.Code} {w.Name}",w.IsActive?2:1));
            }
        }
        else if(_applied.Mode=="details")
        {
            foreach(var s in stocks)
            {
                if(!items.TryGetValue(s.VariantId,out var i))continue;var c=ItemCells(i);
                c["onHand"]=s.OnHand;c["reserved"]=s.Reserved;c["available"]=s.Reserved.HasValue?s.OnHand-s.Reserved.Value:null;c["onOrder"]=s.OnOrder;c["avgCost"]=s.AverageCost;c["value"]=s.Value;c["last"]=s.LastMovement;
                list.Add(new(s.WarehouseId,i.Id,i.UnitId,null,c,$"{WarehouseCaption(s.WarehouseId)} {SearchItem(i)}"));
            }
        }
        else if(_applied.Mode is "qty" or "cost")
        {
            foreach(var s in _data.Movements.Where(s=>Allowed(s.WarehouseId)))
            {
                if(!items.TryGetValue(s.VariantId,out var i))continue;
                var a=_applied.Mode=="qty"?s.Quantity:s.Cost;
                if(_applied.HideZero&&a.Opening==0&&a.Closing==0&&a.In==0&&a.Out==0)continue;
                var c=ItemCells(i);c["opening"]=a.Opening;c["purchase"]=a.Purchases;c["salesReturn"]=a.SalesReturns;c["transferIn"]=a.TransferIn;c["otherIn"]=a.OtherIn;c["sales"]=a.Sales;c["purchaseReturn"]=a.PurchaseReturns;c["transferOut"]=a.TransferOut;c["otherOut"]=a.OtherOut;c["closing"]=a.Closing;c["in"]=a.In;c["out"]=a.Out;
                list.Add(new(s.WarehouseId,i.Id,i.UnitId,null,c,$"{WarehouseCaption(s.WarehouseId)} {SearchItem(i)}"));
            }
        }
        else if(_applied.Mode=="transfers")
        {
            foreach(var t in _data.Transfers.Where(t=>Allowed(t.SourceId)&&Allowed(t.DestinationId)))
            {
                if(!items.TryGetValue(t.VariantId,out var i)||(_applied.Source!=""&&t.SourceId?.ToString()!=_applied.Source)||(_applied.Destination!=""&&t.DestinationId?.ToString()!=_applied.Destination)||(_applied.TransferStatus!=0&&t.Status!=_applied.TransferStatus))continue;
                var c=ItemCells(i);c["number"]=t.Number;c["date"]=t.Date;c["source"]=WarehouseName(t.SourceId);c["destination"]=WarehouseName(t.DestinationId);c["quantity"]=t.Quantity;c["cost"]=t.Cost;c["status"]=TransferState(t.Status);
                list.Add(new(t.SourceId??Guid.Empty,i.Id,i.UnitId,t.Id,c,$"{t.Number} {c["source"]} {c["destination"]} {SearchItem(i)}",t.Status,t.SourceId,t.DestinationId));
            }
        }
        else
        {
            foreach(var t in _data.Counts.Where(t=>Allowed(t.WarehouseId)))
            {
                if(!items.TryGetValue(t.VariantId,out var i)||(_applied.CountStatus!=0&&t.Status!=_applied.CountStatus)||!t.Number.Contains(_applied.CountNumber.Trim(),StringComparison.OrdinalIgnoreCase))continue;
                if(!(_applied.Variance switch{"plus"=>t.Difference>0,"minus"=>t.Difference<0,"equal"=>t.Difference==0,"pending"=>!t.Difference.HasValue,_=>true}))continue;
                var c=ItemCells(i);c["number"]=t.Number;c["date"]=t.Date;c["system"]=t.SystemQuantity;c["counted"]=t.CountedQuantity;c["difference"]=t.Difference;c["avgCost"]=t.AverageCost;c["variance"]=t.Variance;c["status"]=CountState(t.Status);
                list.Add(new(t.WarehouseId,i.Id,i.UnitId,t.Id,c,$"{WarehouseCaption(t.WarehouseId)} {t.Number} {SearchItem(i)}",t.Status));
            }
        }
        _rows=list.Where(r=>Normalize(r.Search).Contains(Normalize(_search.Trim()),StringComparison.CurrentCultureIgnoreCase)).ToList();Sort();_page=1;
    }
    private static string Normalize(string s)=>new(s.Select(c=>c is >= '\u0660' and <= '\u0669'?(char)('0'+c-'\u0660'):c is >= '\u06f0' and <= '\u06f9'?(char)('0'+c-'\u06f0'):c).ToArray());
    private Guid GroupKey(Row r)=>Grouped?r.WarehouseId:Guid.Empty;
    private static object? Cell(Row r,string key)=>r.Cells.TryGetValue(key,out var value)?value:null;
    private void OnSearch(string s){_search=s;if(!_busy)BuildRows();}
    private void SortBy(string key){_descending=_sortKey==key&&!_descending;_sortKey=key;Sort();_page=1;}
    private void Sort(){_rows=_rows.GroupBy(GroupKey).OrderBy(g=>Grouped?WarehouseCaption(g.Key):"").SelectMany(g=>(_descending?g.OrderByDescending(r=>Cell(r,_sortKey)):g.OrderBy(r=>Cell(r,_sortKey))).ThenBy(r=>r.Search)).ToList();}
    private void GoPage(int p)=>_page=Math.Clamp(p,1,TotalPages);
    private void ChangePageSize(ChangeEventArgs e){if(int.TryParse(e.Value?.ToString(),out var n)&&n is 10 or 25 or 50 or 100)_pageSize=n;_page=1;}
    private object? Total(IReadOnlyCollection<Row> rows,Col c)
    {
        if(!c.Sum||rows.Count==0)return null;
        if(c.Kind=="qty"&&(rows.Any(r=>!r.UnitId.HasValue)||rows.Select(r=>r.UnitId).Distinct().Count()!=1))return null;
        // Draft transfers have no posted cost; do not include them in posted cost totals.
        if(_applied.Mode=="transfers"&&c.Key=="cost")return SumKnown(rows.Where(r=>r.Status==2).Select(r=>(decimal?)Cell(r,c.Key)));
        // Uncounted lines are absent from variance totals, not counted as matching.
        if(_applied.Mode=="counts"&&c.Key=="variance"){var recorded=rows.Select(r=>Cell(r,c.Key)).OfType<decimal>().ToArray();return recorded.Length==0?null:recorded.Sum();}
        var vals=rows.Select(r=>Cell(r,c.Key)).ToArray();if(vals.Any(v=>v is null))return null;
        return c.Kind=="int"?(object)vals.Cast<int>().Sum():vals.Cast<decimal>().Sum();
    }
    private string Format(object? value,Col c)=>value switch{null=>"—",decimal d when c.Kind=="money"=>(d/_rate).ToString("N2",CultureInfo.InvariantCulture),decimal d=>d.ToString("#,##0.###",CultureInfo.InvariantCulture),int n=>n.ToString("N0",CultureInfo.InvariantCulture),DateTimeOffset dt=>dt.ToUniversalTime().ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),DateOnly date=>date.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),_=>value.ToString()??""};
    private string M(string key,string kind="money")=>_busy||_error is not null?"—":Format(Total(_rows,new(key,"",kind,"",true)),new(key,"",kind));
    private string N(int n)=>_busy||_error is not null?"—":n.ToString("N0",CultureInfo.InvariantCulture);
    private string Money(decimal? n)=>_busy||_error is not null?"—":Format(n,new("","","money"));
    private int WarehouseCount=>_rows.Select(r=>r.WarehouseId).Distinct().Count();
    private int ItemCount=>_rows.Select(r=>r.VariantId).OfType<Guid>().Distinct().Count();
    private int Documents=>_rows.Select(r=>r.DocumentId).OfType<Guid>().Distinct().Count();
    private int DocStatus(int status)=>_rows.Where(r=>r.Status==status).Select(r=>r.DocumentId).OfType<Guid>().Distinct().Count();
    private decimal? D(Row r,string key)=>Cell(r,key) is decimal d?d:null;
    private MetricDef[] Metrics=>_applied.Mode switch
    {
        "qty" or "cost"=>[new("عدد المخازن",N(WarehouseCount),"blue","fa-solid fa-warehouse"),new("عدد الأصناف",N(ItemCount),"violet","fa-solid fa-boxes-stacked"),new("الرصيد الافتتاحي",M("opening",FlowKind),"orange","fa-solid fa-box"),new("إجمالي الوارد",M("in",FlowKind),"green","fa-solid fa-arrow-down"),new("إجمالي الصادر",M("out",FlowKind),"red","fa-solid fa-arrow-up"),new("الرصيد الختامي",M("closing",FlowKind),"purple","fa-solid fa-coins")],
        "transfers"=>[new("عدد التحويلات",N(Documents),"blue","fa-solid fa-right-left"),new("التحويلات المرحلة",N(DocStatus(2)),"green","fa-solid fa-check"),new("المسودات",N(DocStatus(1)),"orange","fa-solid fa-file"),new("المخازن المرسلة",N(_rows.Select(r=>r.Source).OfType<Guid>().Distinct().Count()),"violet","fa-solid fa-arrow-up"),new("المخازن المستلمة",N(_rows.Select(r=>r.Destination).OfType<Guid>().Distinct().Count()),"red","fa-solid fa-arrow-down"),new("قيمة التحويلات المرحلة",M("cost"),"purple","fa-solid fa-coins")],
        "counts"=>[new("عمليات الجرد",N(Documents),"blue","fa-solid fa-clipboard-list"),new("الأصناف المجرودة",N(_rows.Where(r=>D(r,"counted").HasValue).Select(r=>r.VariantId).Distinct().Count()),"violet","fa-solid fa-boxes-stacked"),new("البنود المطابقة",N(_rows.Count(r=>D(r,"difference")==0)),"green","fa-solid fa-check"),new("قيمة الزيادة",Money(_rows.Select(r=>D(r,"variance")).OfType<decimal>().Where(v=>v>0).Sum()),"orange","fa-solid fa-arrow-up"),new("قيمة العجز",Money(-_rows.Select(r=>D(r,"variance")).OfType<decimal>().Where(v=>v<0).Sum()),"red","fa-solid fa-arrow-down"),new("صافي الفروقات المسجلة",M("variance"),"purple","fa-solid fa-coins")],
        "details"=>[new("عدد المخازن",N(WarehouseCount),"blue","fa-solid fa-warehouse"),new("عدد الأصناف",N(ItemCount),"violet","fa-solid fa-boxes-stacked"),new("أرصدة نافدة",N(_rows.Count(r=>D(r,"onHand")<=0)),"red","fa-solid fa-circle-xmark"),new(IsCurrent(_applied)?"أرصدة متوفرة":"أرصدة موجبة",N(_rows.Count(r=>(D(r,"available")??D(r,"onHand"))>0)),"green","fa-solid fa-circle-check"),new("قيمة المخزون المحجوز",Money(IsCurrent(_applied)?SumKnown(_rows.Select(r=>D(r,"reserved")*D(r,"avgCost"))):null),"orange","fa-solid fa-lock"),new("إجمالي قيمة المخزون",M("value"),"purple","fa-solid fa-coins")],
        _=>[new("عدد المخازن",N(WarehouseCount),"blue","fa-solid fa-warehouse"),new("أرصدة الأصناف",M("items","int"),"violet","fa-solid fa-boxes-stacked"),new("أرصدة نافدة",M("outCount","int"),"red","fa-solid fa-circle-xmark"),new(IsCurrent(_applied)?"أرصدة متوفرة":"أرصدة موجبة",M("availableCount","int"),"green","fa-solid fa-circle-check"),new("قيمة المخزون المحجوز",M("reservedValue"),"orange","fa-solid fa-lock"),new("إجمالي قيمة المخزون",M("value"),"purple","fa-solid fa-coins")]
    };
    private string StatusCss(Row r)=>"warehouse-state "+(_applied.Mode=="counts"?(r.Status==5?"good":r.Status==6?"bad":"pending"):r.Status==2?"good":_applied.Mode=="summary"?"bad":"pending");
    private static string ModeName(string m)=>m switch{"details"=>"أرصدة المخازن حسب الأصناف","qty"=>"حركة المخازن - كميات","cost"=>"حركة المخازن - تكاليف","transfers"=>"التحويلات بين المخازن","counts"=>"الجرد والفروقات",_=>"إجمالي أرصدة المخازن"};
    private static string StockName(string m)=>m switch{"available"=>"متوفر","out"=>"نافد","reserved"=>"محجوز بالكامل",_=>"الكل"};
    private static string TransferState(int s)=>s==2?"مرحل":"مسودة";
    private static string CountState(int s)=>s switch{1=>"مسودة",2=>"قيد الجرد",3=>"قيد المراجعة",4=>"معتمد",5=>"مرحل",6=>"ملغى",_=>"غير محدد"};
    private static string VarianceName(string s)=>s switch{"plus"=>"زيادة","minus"=>"عجز","equal"=>"مطابق","pending"=>"لم يُجرد",_=>"الكل"};
    private string Subtitle=>string.Join(" | ",Chips.Select(c=>$"{c.Label}: {c.Value}")) + $" | البحث: {_search}" + (!IsCurrent(_applied)&&IsBalance(_applied.Mode)?" | الحجوزات وقيد الطلب غير متاحة تاريخياً":"") + " | الشرطة تعني قيمة غير متاحة أو غير قابلة للجمع";
    private object? ExportValue(object? value,Col c)=>value switch{decimal d when c.Kind=="money"=>d/_rate,DateTimeOffset dt=>dt.ToUniversalTime().ToString("yyyy-MM-dd"),DateOnly date=>date.ToString("yyyy-MM-dd"),_=>value};
    private IEnumerable<object?[]> ExportRows()
    {
        foreach(var g in _rows.GroupBy(GroupKey))
        {
            foreach(var r in g)yield return [Grouped?WarehouseCaption(g.Key):"",..Columns.Select(c=>ExportValue(Cell(r,c.Key),c))];
            if(Grouped)yield return ["إجمالي "+WarehouseCaption(g.Key),..Columns.Select(c=>ExportValue(Total(g.ToArray(),c),c))];
        }
        yield return ["إجمالي النتائج",..Columns.Select(c=>ExportValue(Total(_rows,c),c))];
    }
    private async Task ExportAsync()
    {
        if(!CanExport)return;var bytes=CustomerDebtsWorkbook.Create(Subtitle,["المجموعة",..Columns.Select(c=>c.Label)],ExportRows(),sheetName:"المخازن");
        await using var m=await JS.InvokeAsync<IJSObjectReference>("import","./js/customer-debts-export.js");await m.InvokeVoidAsync("saveWorkbook",Convert.ToBase64String(bytes),"warehouses-report.xlsx");
    }
    private async Task PrintAsync()
    {
        if(!CanExport)return;
        var groups=_rows.GroupBy(GroupKey).Select(g=>new{title=Grouped?WarehouseCaption(g.Key):"",rows=g.Select(r=>Columns.Select(c=>Format(Cell(r,c.Key),c)).ToArray()).ToArray(),totals=Grouped?Columns.Skip(IdentityCount).Select(c=>Format(Total(g.ToArray(),c),c)).ToArray():Array.Empty<string>()}).ToArray();
        await using var m=await JS.InvokeAsync<IJSObjectReference>("import","./js/warehouse-report-print.js");
        await m.InvokeVoidAsync("printWarehouseReport","المخازن",Subtitle,Columns.Select(c=>c.Label).ToArray(),groups,Columns.Skip(IdentityCount).Select(c=>Format(Total(_rows,c),c)).ToArray(),IdentityCount,_applied.Mode is "qty" or "cost",_applied.Mode=="qty"?"الكميات":"القيم");
    }
}
