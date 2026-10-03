using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees.Compensation;
using OAS.Contracts.Features.Employees.Contracts;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Components;

public partial class EmployeeSalaryPanel
{
    private static readonly IReadOnlyList<string> SalaryGridHeaders = ["الرقم", "من", "إلى", "العملة", "الحالة"];
    private enum EditorMode { Empty, View, Create, Edit }
    [Parameter, EditorRequired] public Guid EmployeeId { get; set; }
    [Parameter] public EventCallback OnStateChanged { get; set; }
    [Inject] private IEmployeeCompensationClientService Compensation { get; set; } = default!;
    [Inject] private IEmployeeContractClientService Contracts { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private Guid _loadedEmployeeId;
    private IReadOnlyList<EmployeeSalaryStructureDto> _items = [];
    private EmployeeSalaryStructureDto? _selected;
    private IReadOnlyList<SalaryComponentDto> _components = [];
    private IReadOnlyList<EmployeeContractDto> _contracts = [];
    private IReadOnlyList<UiSelectOption> ComponentOptions { get; set; } = [];
    private IReadOnlyList<UiSelectOption> ContractOptions { get; set; } = [];
    private IReadOnlyList<UiSelectOption> CurrencyOptions { get; set; } = [];
    private readonly List<SalaryLineEditModel> _lines = [];
    private EditorMode _mode = EditorMode.Empty;
    private bool _loading, _saving;
    private string _contractValue = string.Empty, _currencyValue = string.Empty;
    private DateOnly? _effectiveFrom, _effectiveTo;
    private string? _notes;

    public bool CanNew => !_saving && !IsEditing;
    public bool CanEdit => _selected is { Status: 1 } && !IsEditing && !_saving;
    public bool CanSave => IsEditing && !_saving;
    public bool CanCancelEdit => IsEditing && !_saving;
    public bool CanRefresh => !IsEditing && !_saving;
    public bool CanActivate => _selected is { Status: 1 } && !IsEditing && !_saving;
    public bool CanCancelStructure => _selected is { Status: 1 } && !IsEditing && !_saving;
    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit;
    private string EditorTitle => _mode switch { EditorMode.Create=>"هيكل راتب جديد",EditorMode.Edit=>"تعديل هيكل الراتب",EditorMode.View=>"تفاصيل هيكل الراتب",_=>"تفاصيل هيكل الراتب"};

    protected override async Task OnParametersSetAsync()
    {
        if(EmployeeId==Guid.Empty||EmployeeId==_loadedEmployeeId)return;
        _loadedEmployeeId=EmployeeId;
        await LoadAsync();
    }

    public async Task NewAsync()
    {
        if(!CanNew)return;
        _selected=null;_mode=EditorMode.Create;_contractValue=string.Empty;_effectiveFrom=DateOnly.FromDateTime(DateTime.Today);_effectiveTo=null;_notes=null;_lines.Clear();
        _currencyValue=CurrencyOptions.FirstOrDefault()?.Value??string.Empty;
        if(_components.FirstOrDefault(x=>x.IsBasicSalary&&x.IsActive) is { } basic) _lines.Add(SalaryLineEditModel.FromComponent(basic));
        await ChangedAsync();
    }
    public async Task EditAsync(){if(!CanEdit||_selected is null)return;_mode=EditorMode.Edit;LoadForm(_selected);await ChangedAsync();}
    public async Task CancelEditAsync(){if(!CanCancelEdit)return;if(_selected is null){_mode=EditorMode.Empty;ClearForm();}else{_mode=EditorMode.View;LoadForm(_selected);}await ChangedAsync();}
    public Task RefreshAsync()=>LoadAsync();
    public async Task SaveAsync()
    {
        if(!CanSave||_effectiveFrom is null||!Guid.TryParse(_currencyValue,out var currencyId)){Snackbar.Error("تحقق من تاريخ السريان والعملة.");return;}
        Guid? contractId=Guid.TryParse(_contractValue,out var cid)?cid:null;
        var requests=new List<SalaryStructureLineRequest>();
        foreach(var line in _lines)
        {
            if(!Guid.TryParse(line.ComponentValue,out var componentId)){Snackbar.Error("اختر مكونًا صالحًا لكل سطر.");return;}
            if(!TryDecimal(line.AmountText,out var amount)||amount<0){Snackbar.Error("قيمة مكون الراتب غير صحيحة.");return;}
            decimal? percentage=null;
            if(line.IsPercentage)
            {
                if(!TryDecimal(line.PercentageText,out var pct)||pct<=0){Snackbar.Error("أدخل نسبة صحيحة للمكون النسبي.");return;}
                percentage=pct;
            }
            requests.Add(new SalaryStructureLineRequest(componentId,amount,percentage));
        }
        if(requests.Count==0){Snackbar.Error("هيكل الراتب يحتاج مكونًا واحدًا على الأقل.");return;}
        _saving=true;await ChangedAsync();
        try
        {
            ApiCallResult<EmployeeSalaryStructureDto> result;
            if(_mode==EditorMode.Create) result=await Compensation.CreateStructureAsync(EmployeeId,new CreateEmployeeSalaryStructureRequest(contractId,currencyId,_effectiveFrom.Value,_effectiveTo,NullIfEmpty(_notes),requests));
            else if(_selected is not null) result=await Compensation.UpdateStructureAsync(_selected.Id,new UpdateEmployeeSalaryStructureRequest(contractId,currencyId,_effectiveFrom.Value,_effectiveTo,NullIfEmpty(_notes),requests,_selected.RowVersion));
            else return;
            if(!result.Succeeded||result.Value is null){if(result.Error is not null)ApiFeedback.Show(result.Error);else ApiFeedback.ShowUnexpected();return;}
            _selected=result.Value;_mode=EditorMode.View;await LoadAsync();Snackbar.Success("تم حفظ هيكل الراتب بنجاح.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
        finally{_saving=false;await ChangedAsync();}
    }
    public async Task ActivateAsync(){if(!CanActivate||_selected is null)return;var result=await Compensation.ActivateStructureAsync(_selected.Id,new SalaryStructureLifecycleRequest(_selected.RowVersion));await HandleLifecycleAsync(result,"تم تفعيل هيكل الراتب.");}
    public async Task CancelStructureAsync(){if(!CanCancelStructure||_selected is null)return;var result=await Compensation.CancelStructureAsync(_selected.Id,new SalaryStructureLifecycleRequest(_selected.RowVersion));await HandleLifecycleAsync(result,"تم إلغاء هيكل الراتب.");}

    private async Task HandleLifecycleAsync(ApiCallResult<EmployeeSalaryStructureDto> result,string message){if(!result.Succeeded||result.Value is null){if(result.Error is not null)ApiFeedback.Show(result.Error);else ApiFeedback.ShowUnexpected();return;}_selected=result.Value;_mode=EditorMode.View;await LoadAsync();Snackbar.Success(message);await ChangedAsync();}
    private async Task LoadAsync()
    {
        _loading=true;await ChangedAsync();
        try
        {
            var currenciesTask=Accounting.GetCurrenciesPageAsync(new PageRequest{PageNumber=1,PageSize=200,SortBy="Code",SortDirection=SortDirection.Ascending});
            var componentsTask=Compensation.GetComponentsAsync();
            var contractsTask=Contracts.GetForEmployeeAsync(EmployeeId);
            var structuresTask=Compensation.GetStructuresAsync(EmployeeId);
            await Task.WhenAll(currenciesTask,componentsTask,contractsTask,structuresTask);
            var currencies=currenciesTask.Result;_components=componentsTask.Result;_contracts=contractsTask.Result;_items=structuresTask.Result;
            CurrencyOptions=currencies.Items.Where(x=>x.IsActive||x.Id==_selected?.CurrencyId).Select(x=>new UiSelectOption(x.Id.ToString("D"),$"{x.Code} - {x.NameAr}")).ToArray();
            ComponentOptions=_components.Where(x=>x.IsActive||_selected?.Lines.Any(l=>l.SalaryComponentId==x.Id)==true).OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.NameAr).Select(x=>new UiSelectOption(x.Id.ToString("D"),$"{x.ComponentCode} - {x.NameAr}")).ToArray();
            ContractOptions=_contracts.Where(x=>x.Status is 1 or 2).OrderByDescending(x=>x.StartDate).Select(x=>new UiSelectOption(x.Id.ToString("D"),$"{x.ContractCode} - {x.CurrencyCode}")).ToArray();
            var selectedId=_selected?.Id;_selected=selectedId.HasValue?_items.FirstOrDefault(x=>x.Id==selectedId):_items.FirstOrDefault();
            if(_selected is not null){_mode=EditorMode.View;LoadForm(_selected);}else{_mode=EditorMode.Empty;ClearForm();}
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
        finally{_loading=false;await ChangedAsync();}
    }
    private void SelectItem(EmployeeSalaryStructureDto item){if(IsEditing||_saving)return;_selected=item;_mode=EditorMode.View;LoadForm(item);_=ChangedAsync();}
    private void LoadForm(EmployeeSalaryStructureDto x){_contractValue=x.ContractId?.ToString("D")??string.Empty;_currencyValue=x.CurrencyId.ToString("D");_effectiveFrom=x.EffectiveFrom;_effectiveTo=x.EffectiveTo;_notes=x.Notes;_lines.Clear();foreach(var line in x.Lines)_lines.Add(SalaryLineEditModel.FromDto(line,_components));}
    private void ClearForm(){_contractValue=string.Empty;_currencyValue=string.Empty;_effectiveFrom=null;_effectiveTo=null;_notes=null;_lines.Clear();}
    private Task SetContractAsync(string? value){_contractValue=value??string.Empty;if(Guid.TryParse(_contractValue,out var id)&&_contracts.FirstOrDefault(x=>x.Id==id) is { } contract)_currencyValue=contract.CurrencyId.ToString("D");return Task.CompletedTask;}
    private Task SetCurrencyAsync(string? value){_currencyValue=value??string.Empty;return Task.CompletedTask;}
    private Task AddLineAsync(MouseEventArgs _){if(!IsEditing)return Task.CompletedTask;var component=_components.FirstOrDefault(x=>x.IsActive&&_lines.All(l=>l.ComponentValue!=x.Id.ToString("D")));if(component is not null)_lines.Add(SalaryLineEditModel.FromComponent(component));return ChangedAsync();}
    private Task RemoveLineAsync(SalaryLineEditModel line){if(IsEditing)_lines.Remove(line);return ChangedAsync();}
    private Task SetLineComponentAsync(SalaryLineEditModel line,string? value){line.ComponentValue=value??string.Empty;line.ApplyComponent(_components.FirstOrDefault(x=>x.Id.ToString("D")==line.ComponentValue));return ChangedAsync();}
    private Task ChangedAsync()=>OnStateChanged.InvokeAsync();
    private static bool TryDecimal(string? value,out decimal result)=>decimal.TryParse(value,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.CurrentCulture,out result)||decimal.TryParse(value,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out result);
    private static string? NullIfEmpty(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static string StatusName(byte value)=>value switch{1=>"مسودة",2=>"فعال",3=>"مستبدل",4=>"ملغي",_=>"—"};
    private static AlertTone StatusTone(byte value)=>value switch{2=>AlertTone.Success,3=>AlertTone.Warning,4=>AlertTone.Danger,_=>AlertTone.Info};

    private sealed class SalaryLineEditModel
    {
        public Guid Key { get; }=Guid.NewGuid(); public string ComponentValue{get;set;}=string.Empty;public string AmountText{get;set;}="0";public string PercentageText{get;set;}=string.Empty;public bool IsPercentage{get;private set;}public string ComponentHint{get;private set;}=string.Empty;
        public static SalaryLineEditModel FromComponent(SalaryComponentDto x){var m=new SalaryLineEditModel{ComponentValue=x.Id.ToString("D"),AmountText="0"};m.ApplyComponent(x);return m;}
        public static SalaryLineEditModel FromDto(EmployeeSalaryStructureLineDto x,IReadOnlyList<SalaryComponentDto> components){var m=new SalaryLineEditModel{ComponentValue=x.SalaryComponentId.ToString("D"),AmountText=x.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),PercentageText=x.Percentage?.ToString(System.Globalization.CultureInfo.InvariantCulture)??string.Empty};m.ApplyComponent(components.FirstOrDefault(c=>c.Id==x.SalaryComponentId),x.CalculationMethodSnapshot,x.IsBasicSalarySnapshot,x.ComponentNameSnapshot);return m;}
        public void ApplyComponent(SalaryComponentDto? component,byte fallbackMethod=0,bool fallbackBasic=false,string? fallbackName=null){var method=component?.CalculationMethod??fallbackMethod;IsPercentage=method==2;if(!IsPercentage)PercentageText=string.Empty;var basic=component?.IsBasicSalary??fallbackBasic;ComponentHint=$"{(basic?"راتب أساسي • ":string.Empty)}{MethodName(method)}"+(string.IsNullOrWhiteSpace(component?.NameAr??fallbackName)?string.Empty:$" • {component?.NameAr??fallbackName}");}
        private static string MethodName(byte value)=>value switch{1=>"ثابت",2=>"نسبة من الأساسي",3=>"بالساعة",4=>"باليوم",5=>"يدوي",6=>"مصدر خارجي",_=>""};
    }
}
