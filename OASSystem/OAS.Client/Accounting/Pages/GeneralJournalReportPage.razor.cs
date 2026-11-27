using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Accounting.Common;
using OAS.Client.Accounting.Services;
using OAS.Client.Identity.Services;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Users;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Accounting.Pages;

public partial class GeneralJournalReportPage
{
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IUserClientService Users { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private sealed record Filters
    {
        public string Year { get; set; } = "";
        public string Period { get; set; } = "";
        public DateTime From { get; set; } = new(DateTime.Today.Year, 1, 1);
        public DateTime To { get; set; } = DateTime.Today;
        public string Account { get; set; } = "";
        public string Scope { get; set; } = "complete";
        public string Mode { get; set; } = "grouped";
        public string Type { get; set; } = "";
        public string Source { get; set; } = "";
        public string Reversal { get; set; } = "";
        public string State { get; set; } = ((byte)JournalEntryStatus.Posted).ToString(CultureInfo.InvariantCulture);
        public string User { get; set; } = "";
        public string Reference { get; set; } = "";
        public string Currency { get; set; } = "";
    }
    private sealed record Col(string Key, string Label, string Css = "");
    private sealed record Unit(IReadOnlyList<JournalReportLineDto> Lines);
    private static readonly Col Debit = new("debit", "مدين", "oas-report__debit");
    private static readonly Col Credit = new("credit", "دائن", "oas-report__credit");
    private static readonly Col[] DetailColumns = [new("date","التاريخ"),new("number","رقم القيد"),new("type","نوع القيد"),new("document","نوع المستند"),new("reference","المرجع"),new("code","رقم الحساب"),new("account","اسم الحساب"),new("description","البيان"),new("reversal","حالة العكس"),new("related","القيد المرتبط"),Debit,Credit,new("status","الحالة")];
    private static readonly Col[] SummaryColumns = [new("date","التاريخ"),new("number","رقم القيد"),new("type","نوع القيد"),new("document","نوع المستند"),new("reference","المرجع"),new("count","عدد البنود"),new("reversal","حالة العكس"),new("related","القيد المرتبط"),Debit,Credit,new("status","الحالة")];
    private static readonly Col[] SourceColumns = [new("source","مصدر القيد"),new("journals","عدد القيود"),new("count","عدد البنود"),Debit,Credit,new("difference","الفرق")];
    private Col[] Columns => _applied.Mode switch { "summary" => SummaryColumns, "source" => SourceColumns, _ => DetailColumns };
    private Filters _draft = new(), _applied = new();
    private IReadOnlyList<AccountDto> _accounts = [];
    private IReadOnlyList<FiscalYearDto> _years = [];
    private IReadOnlyList<FiscalPeriodDto> _periods = [];
    private IReadOnlyList<CurrencyDto> _currencies = [];
    private IReadOnlyList<UserSummaryDto> _users = [];
    private IReadOnlyList<JournalReportLineDto> _loaded = [];
    private IReadOnlyList<Unit> _units = [];
    private readonly HashSet<string> _sources = new(StringComparer.OrdinalIgnoreCase) { "Accounting", "Sales", "Purchasing", "Inventory", "__manual" };
    private bool _busy, _referencesLoaded, _filtersOpen = true, _descending;
    private string? _error, _validation;
    private string _search = "", _sort = "date", _baseName = "العملة الأساسية", _currencyName = "العملة الأساسية";
    private Guid? _baseId;
    private decimal _rate = 1m, _debit, _credit;
    private int _page = 1, _pageSize = 25, _journalCount, _lineCount, _unpostedCount;
    private bool CanExport => !_busy && _error is null && _units.Count > 0;
    private int TotalPages => Math.Max(1, (_units.Count + _pageSize - 1) / _pageSize);
    private IEnumerable<Unit> PageUnits => _units.Skip((_page - 1) * _pageSize).Take(_pageSize);
    private string UnitLabel => _applied.Mode switch { "lines" => "بند", "source" => "مصدر", _ => "قيد" };
    private string Status => _busy ? "جارٍ التحميل" : _error is not null ? "غير متاح" : _applied.State == "" ? "جميع الحالات" : StateName(ParseEnum<JournalEntryStatus>(_applied.State) ?? JournalEntryStatus.Posted);
    private IEnumerable<FiscalPeriodDto> PeriodOptions => _periods.Where(p => _draft.Year == "" || p.FiscalYearId.ToString() == _draft.Year).OrderBy(p => p.StartDate);
    private IEnumerable<string> SourceOptions => _sources.OrderBy(SourceName);
    private IReadOnlyList<UiSelectOption> AccountOptions => new[] { new UiSelectOption("", "جميع الحسابات") }.Concat(_accounts.OrderBy(a=>a.Code).Select(a=>new UiSelectOption(a.Id.ToString(),$"{a.Code} - {a.NameAr}"))).ToArray();
    private IReadOnlyList<UiSelectOption> UserOptions => new[] { new UiSelectOption("", "جميع المستخدمين") }.Concat(_users.OrderBy(u=>u.DisplayName).Select(u=>new UiSelectOption(u.Id.ToString(),$"{u.DisplayName} - {u.UserName}"))).ToArray();
    private IReadOnlyList<(string Label,string Value)> Chips => [
        ("الفترة",$"{_applied.From:dd/MM/yyyy} – {_applied.To:dd/MM/yyyy}"),
        ("الحساب",_accounts.FirstOrDefault(a=>a.Id.ToString()==_applied.Account) is { } a ? a.Code+" - "+a.NameAr : "الكل"),
        ("العرض",ModeName(_applied.Mode)),("النوع",ParseEnum<JournalType>(_applied.Type) is { } t ? TypeName(t) : "الكل"),
        ("المصدر",_applied.Source=="" ? "الكل" : SourceName(_applied.Source)),("الحالة",_applied.State=="" ? "الكل" : StatusName()),("العملة",_currencyName),
        ("حالة العكس",_applied.Reversal=="" ? "الكل" : ReversalFilterName(_applied.Reversal)),
        ("نطاق الحساب",_applied.Account=="" ? "الكل" : _applied.Scope=="complete" ? "القيود الكاملة" : "بنود الحساب فقط"),
        ("المستخدم",_users.FirstOrDefault(u=>u.Id.ToString()==_applied.User)?.DisplayName ?? "الكل"), ("المرجع",_applied.Reference=="" ? "الكل" : _applied.Reference)];
    private string StatusName() => ParseEnum<JournalEntryStatus>(_applied.State) is { } s ? StateName(s) : "الكل";
    protected override async Task OnInitializedAsync() => await ApplyAsync();
    private static async Task<IReadOnlyList<T>> LoadAll<T>(Func<PageRequest,Task<PagedResult<T>>> loader)
    {
        var result=new List<T>();
        for(var n=1;;n++) { var page=await loader(new PageRequest{PageNumber=n,PageSize=100}); result.AddRange(page.Items); if(!page.HasNextPage) return result; }
    }
    private async Task LoadReferencesAsync()
    {
        _accounts=await LoadAll(p=>Accounting.GetAccountsPageAsync(p));
        _years=(await LoadAll(p=>Accounting.GetFiscalYearsPageAsync(p))).OrderByDescending(y=>y.StartDate).ToArray();
        _periods=await LoadAll(p=>Accounting.GetFiscalPeriodsPageAsync(p));
        _currencies=await LoadAll(p=>Accounting.GetCurrenciesPageAsync(p));
        var settings=await Accounting.GetAccountingSettingsAsync();
        _baseId=settings?.BaseCurrencyId;
        _baseName=_currencies.FirstOrDefault(c=>c.Id==_baseId)?.NameAr ?? settings?.BaseCurrencyCode ?? "العملة الأساسية";
        // The user directory may require separate permission. Do not fail the report if inaccessible.
        try { _users=await LoadAll(p=>Users.GetUsersAsync(p)); } catch { _users=[]; }
        _referencesLoaded=true;
    }
    private void OnAccount(string? value)=>_draft.Account=value??"";
    private void OnUser(string? value)=>_draft.User=value??"";
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
    private static Guid? Id(string value)=>Guid.TryParse(value,out var id)?id:null;
    private static T? ParseEnum<T>(string value) where T:struct,Enum => Enum.TryParse<T>(value,out var parsed)&&Enum.IsDefined(parsed)?parsed:null;
    private async Task ResetAsync(){if(_busy)return;_draft=new();_search="";_sort="date";_descending=false;await ApplyAsync();}
    private async Task ApplyAsync()
    {
        if(_busy)return;
        _validation=null;
        var next=_draft with { Reference=_draft.Reference.Trim() };
        if(next.From.Date>next.To.Date){_validation="تاريخ البداية يجب ألا يتجاوز تاريخ النهاية.";return;}
        _busy=true;_error=null;
        try
        {
            if(!_referencesLoaded)await LoadReferencesAsync();
            var filter=new JournalReportFilterDto(DateOnly.FromDateTime(next.From),DateOnly.FromDateTime(next.To),FiscalYearId:Id(next.Year),FiscalPeriodId:Id(next.Period),JournalType:ParseEnum<JournalType>(next.Type),SourceModule:next.Source is "" or "__manual" ? null : next.Source,Status:ParseEnum<JournalEntryStatus>(next.State),AccountId:Id(next.Account),UserId:Id(next.User),PageSize:500,IncludeCompleteEntries:next.Scope=="complete");
            var all=new List<JournalReportLineDto>();
            long? expected=null; decimal expectedDebit=0m,expectedCredit=0m;
            for(var n=1;;n++)
            {
                var result=await Accounting.GetJournalReportAsync(filter with {PageNumber=n}) ?? throw new InvalidOperationException("لم يُرجع الخادم بيانات اليومية العامة.");
                if(expected is null){expected=result.TotalCount;expectedDebit=result.TotalDebit;expectedCredit=result.TotalCredit;}
                if(result.TotalCount!=expected || result.TotalDebit!=expectedDebit || result.TotalCredit!=expectedCredit)throw new InvalidOperationException("تغيرت القيود أثناء التحميل. أعد تحديث التقرير.");
                if(expected>50_000)throw new InvalidOperationException("الفترة تحتوي أكثر من 50,000 بند. حدد فترة أقصر أو حساباً أو نوع قيد ثم أعد التطبيق.");
                all.AddRange(result.Lines);
                next.From=result.FromDate.ToDateTime(TimeOnly.MinValue);next.To=result.ToDate.ToDateTime(TimeOnly.MinValue);
                if(all.Count>=expected)break;
                if(result.Lines.Count==0)throw new InvalidOperationException("تعذر تحميل جميع البنود. أعد المحاولة.");
            }
            if(all.Count!=expected || all.Select(l=>(l.JournalEntryId,l.LineNumber)).Distinct().Count()!=all.Count || all.Sum(l=>l.Debit)!=expectedDebit || all.Sum(l=>l.Credit)!=expectedCredit)
                throw new InvalidOperationException("تعذر مطابقة جميع بنود التقرير مع إجماليات الخادم. أعد التحديث.");
            decimal rate=1m;var currencyName=_baseName;
            if(Id(next.Currency) is Guid currencyId && currencyId!=_baseId)
            {
                var c=_currencies.FirstOrDefault(x=>x.Id==currencyId && x.IsActive)??throw new InvalidOperationException("العملة غير متاحة.");
                var effective=await Accounting.GetEffectiveExchangeRateAsync(currencyId,DateOnly.FromDateTime(next.To),ExchangeRateType.Accounting);
                if(effective is null||effective.Rate<=0m)throw new InvalidOperationException("لا يوجد سعر صرف محاسبي صالح حتى نهاية الفترة.");
                rate=effective.Rate;currencyName=$"{c.NameAr} ({c.Code})";
            }
            foreach(var l in all)_sources.Add(SourceKey(l));
            _loaded=all;_applied=next;_rate=rate;_currencyName=currencyName;
            if(!Columns.Any(c=>c.Key==_sort)){_sort=Columns[0].Key;_descending=false;}
            BuildUnits();
        }
        catch(Exception ex){_error="تعذر تحميل اليومية العامة: "+ex.Message;_loaded=[];_units=[];_debit=_credit=0;_journalCount=_lineCount=_unpostedCount=0;_page=1;}
        finally{_busy=false;}
    }
    private static string SourceKey(JournalReportLineDto l)=>string.IsNullOrWhiteSpace(l.SourceModule)?"__manual":l.SourceModule!;
    private static string Normalize(string value)=>new(value.Select(c=>c is >= '\u0660' and <= '\u0669'?(char)('0'+c-'\u0660'):c is >= '\u06f0' and <= '\u06f9'?(char)('0'+c-'\u06f0'):c).ToArray());
    private static bool Contains(string? value,string term)=>Normalize(value??"").Contains(Normalize(term.Trim()),StringComparison.OrdinalIgnoreCase);
    private bool MatchText(JournalReportLineDto l)=>Contains($"{l.JournalNumber} {l.Reference} {l.AccountCode} {l.AccountNameAr} {l.DisplayDescription ?? l.Description} {TypeName(l.JournalType)} {DocumentName(l)} {SourceName(SourceKey(l))} {StateName(l.Status)} {AccountingArabicPresenter.GetJournalReversalText(l.Reversal)} {AccountingArabicPresenter.GetJournalRelatedNumbers(l.Reversal)}",_search);
    private bool MatchReference(JournalReportLineDto l)=>Contains(l.JournalNumber,_applied.Reference)||Contains(l.Reference,_applied.Reference)||Contains(AccountingArabicPresenter.GetJournalRelatedNumbers(l.Reversal),_applied.Reference);
    private bool MatchReversal(JournalReportLineDto l) => _applied.Reversal switch
    {
        "normal" => l.Reversal is { IsReversal: false, ReversalJournalId: null },
        "pending" => l.Reversal?.ReversalJournalStatus is JournalEntryStatus.Draft or JournalEntryStatus.PendingApproval or JournalEntryStatus.Approved,
        "done" => l.Reversal?.ReversalJournalStatus == JournalEntryStatus.Posted,
        "reversal" => l.Reversal?.IsReversal == true,
        "review" => l.Reversal?.ReversalJournalId is not null && l.Reversal.ReversalJournalStatus is not (JournalEntryStatus.Draft or JournalEntryStatus.PendingApproval or JournalEntryStatus.Approved or JournalEntryStatus.Posted),
        _ => true
    };
    private static string ReversalFilterName(string value) => value switch { "normal"=>"غير معكوس", "pending"=>"عكس معلق", "done"=>"معكوس", "reversal"=>"قيد عكسي", "review"=>"عكس يحتاج مراجعة", _=>"الكل" };
    private void BuildUnits()
    {
        var scoped=_loaded.Where(l=>(_applied.Source==""||string.Equals(SourceKey(l),_applied.Source,StringComparison.OrdinalIgnoreCase)) && MatchReversal(l));
        var lines=_applied.Mode=="lines" ? scoped.Where(l=>MatchText(l)&&MatchReference(l)).ToArray() : scoped.GroupBy(l=>l.JournalEntryId).Where(g=>g.Any(MatchText)&&g.Any(MatchReference)).SelectMany(g=>g).ToArray();
        _debit=lines.Sum(l=>l.Debit);_credit=lines.Sum(l=>l.Credit);_lineCount=lines.Length;
        _journalCount=lines.Select(l=>l.JournalEntryId).Distinct().Count();_unpostedCount=lines.Where(l=>l.Status is JournalEntryStatus.Draft or JournalEntryStatus.PendingApproval or JournalEntryStatus.Approved).Select(l=>l.JournalEntryId).Distinct().Count();
        _units=_applied.Mode switch {"lines"=>lines.Select(l=>new Unit([l])).ToArray(),"source"=>lines.GroupBy(SourceKey,StringComparer.OrdinalIgnoreCase).Select(g=>new Unit(g.ToArray())).ToArray(),_=>lines.GroupBy(l=>l.JournalEntryId).Select(g=>new Unit(g.OrderBy(l=>l.LineNumber).ToArray())).ToArray()};
        SortUnits();_page=1;
    }
    private IEnumerable<JournalReportLineDto?> DisplayLines(Unit u)=>_applied.Mode switch {"source"=>new JournalReportLineDto?[]{null},"summary"=>new JournalReportLineDto?[]{u.Lines[0]},_=>u.Lines};
    private object? Cell(Unit u,JournalReportLineDto? l,string key)
    {
        var first=l??u.Lines[0];var aggregate=_applied.Mode is "summary" or "source" || l is null;
        return key switch {"date"=>first.PostingDate,"number"=>first.JournalNumber,"type"=>TypeName(first.JournalType),"document"=>DocumentName(first),"reference"=>first.Reference,"code"=>first.AccountCode,"account"=>first.AccountNameAr,"description"=>first.DisplayDescription??first.Description,"status"=>StateName(first.Status),"reversal"=>AccountingArabicPresenter.GetJournalReversalText(first.Reversal),"related"=>AccountingArabicPresenter.GetJournalRelatedNumbers(first.Reversal),"source"=>SourceName(SourceKey(first)),"count"=>u.Lines.Count,"journals"=>u.Lines.Select(x=>x.JournalEntryId).Distinct().Count(),"debit"=>aggregate?u.Lines.Sum(x=>x.Debit):first.Debit,"credit"=>aggregate?u.Lines.Sum(x=>x.Credit):first.Credit,"difference"=>u.Lines.Sum(x=>x.Debit-x.Credit),_=>null};
    }
    private object? SortValue(Unit u)=>_sort is "debit" or "credit" or "difference" ? Cell(u,null,_sort) : Cell(u,u.Lines[0],_sort);
    private void SortUnits()=>_units=(_descending?_units.OrderByDescending(SortValue):_units.OrderBy(SortValue)).ThenBy(u=>u.Lines[0].PostingDate).ThenBy(u=>u.Lines[0].JournalNumber).ThenBy(u=>u.Lines[0].LineNumber).ToArray();
    private void SortBy(string key){if(_busy)return;_descending=_sort==key&&!_descending;_sort=key;SortUnits();_page=1;}
    private void OnSearch(string value){_search=value;if(!_busy&&_error is null)BuildUnits();}
    private void GoPage(int p)=>_page=Math.Clamp(p,1,TotalPages);
    private void ChangePageSize(ChangeEventArgs e){if(int.TryParse(e.Value?.ToString(),out var n)&&n is 10 or 25 or 50 or 100)_pageSize=n;_page=1;}
    private string Money(decimal amount)=> (amount/_rate).ToString("N2",CultureInfo.InvariantCulture);
    private string Metric(decimal amount)=>_busy||_error is not null?"—":Money(amount);
    private string Count(int count)=>_busy||_error is not null?"—":count.ToString("N0",CultureInfo.InvariantCulture);
    private string Format(object? value,string key)=>value switch {null=>"—",decimal d=>Money(d),DateOnly date=>date.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),int n=>n.ToString("N0",CultureInfo.InvariantCulture),_=>value.ToString()??"—"};
    private void OpenJournal(Guid id)=>Nav.NavigateTo($"/accounting/journals?journalId={id:D}");
    private void OpenStatement(Guid id)=>Nav.NavigateTo($"/accounting/reports/account-statement?accountId={id:D}&fromDate={_applied.From:yyyy-MM-dd}&toDate={_applied.To:yyyy-MM-dd}{(Id(_applied.Currency) is Guid c?$"&currencyId={c:D}":"")}");
    private static string TypeName(JournalType t)=>AccountingArabicPresenter.GetJournalTypeText(t);
    private static string StateName(JournalEntryStatus t)=>AccountingArabicPresenter.GetJournalStatusText(t);
    private static string ModeName(string key)=>key switch{"summary"=>"إجمالي حسب القيد","lines"=>"تفصيلي حسب السطور","source"=>"إجمالي حسب المصدر",_=>"تفصيلي حسب القيد"};
    private static string SourceName(string key)=>key.ToLowerInvariant() switch{"accounting"=>"المحاسبة","sales"=>"المبيعات","purchasing"=>"المشتريات","inventory"=>"المخزون","__manual"=>"دون مصدر آلي",_=>key};
    private static string DocumentName(JournalReportLineDto l)=>l.SourceDocumentType?.ToLowerInvariant() switch {"salesinvoice"=>"فاتورة مبيعات","salesreturn"=>"مرتجع مبيعات","purchaseinvoice"=>"فاتورة مشتريات","purchasereceipt"=>"استلام مشتريات","purchasereturn"=>"مرتجع مشتريات","receiptvoucher"=>"سند قبض","paymentvoucher"=>"سند صرف","inventorytransaction"=>"حركة مخزون","customersettlementfx" or "suppliersettlementfx"=>"تسوية فرق صرف","customersettlementfxreversal" or "suppliersettlementfxreversal"=>"عكس تسوية فرق صرف","journalentryreversal"=>"عكس قيد يومية",_=>string.IsNullOrWhiteSpace(l.SourceDocumentType)?TypeName(l.JournalType):l.SourceDocumentType!};
    private string Subtitle=>string.Join(" | ",Chips.Select(c=>$"{c.Label}: {c.Value}"))+" | البحث: "+(_search==""?"الكل":_search);
    private string[] ExportHeaders=>Columns.Select(c=>c.Label).ToArray();
    private object?[] TotalRow(string title,decimal debit,decimal credit)=>Columns.Select((c,i)=>i==0?(object?)title:c.Key=="debit"?debit:c.Key=="credit"?credit:c.Key=="difference"?debit-credit:null).ToArray();
    private IEnumerable<object?[]> ExportRows()
    {
        foreach(var u in _units){foreach(var l in DisplayLines(u))yield return Columns.Select(c=>Cell(u,l,c.Key)).ToArray();if(_applied.Mode=="grouped")yield return TotalRow("إجمالي القيد "+u.Lines[0].JournalNumber,u.Lines.Sum(l=>l.Debit),u.Lines.Sum(l=>l.Credit));}
        yield return TotalRow("الإجمالي العام",_debit,_credit);
    }
    private async Task ExportAsync()
    {
        if(!CanExport)return;
        try{var bytes=CustomerDebtsWorkbook.Create(Subtitle,ExportHeaders,ExportRows().Select(r=>r.Select(v=>v is decimal d?(object)(d/_rate):v is DateOnly date?date.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture):v).ToArray()),sheetName:"اليومية العامة");await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/customer-debts-export.js");await module.InvokeVoidAsync("saveWorkbook",Convert.ToBase64String(bytes),"general-journal.xlsx");}catch(Exception ex){_validation="تعذر التصدير: "+ex.Message;}
    }
    private async Task PrintAsync()
    {
        if(!CanExport)return;
        try{await using var module=await JS.InvokeAsync<IJSObjectReference>("import","./js/general-journal-print.js");await module.InvokeVoidAsync("printReport",Subtitle,ExportHeaders,ExportRows().Select(r=>r.Select((v,i)=>Format(v,Columns[i].Key)).ToArray()).ToArray());}catch(Exception ex){_validation="تعذرت الطباعة: "+ex.Message;}
    }
}
