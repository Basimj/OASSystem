using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees.Contracts;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Components;

public partial class EmployeeContractsPanel
{
    private static readonly IReadOnlyList<string> ContractGridHeaders = ["الرقم", "النوع", "البداية", "العملة", "الحالة"];
    private enum EditorMode { Empty, View, Create, Edit }
    [Parameter, EditorRequired] public Guid EmployeeId { get; set; }
    [Parameter] public EventCallback OnStateChanged { get; set; }
    [Inject] private IEmployeeContractClientService ContractService { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private Guid _loadedEmployeeId;
    private IReadOnlyList<EmployeeContractDto> _items = [];
    private EmployeeContractDto? _selected;
    private IReadOnlyList<UiSelectOption> CurrencyOptions { get; set; } = [];
    private EditorMode _mode = EditorMode.Empty;
    private bool _loading, _saving;
    private string _contractTypeValue = "1", _currencyValue = string.Empty, _workingHoursText = string.Empty, _workingDaysText = string.Empty;
    private DateOnly? _startDate, _endDate, _probationEndDate, _terminationEffectiveDate;
    private string? _notes, _terminationReason;

    private static readonly IReadOnlyList<UiSelectOption> ContractTypeOptions = [new("1","دائم"),new("2","محدد المدة"),new("3","دوام جزئي"),new("4","مؤقت")];
    public bool CanNew => !_saving && !IsEditing;
    public bool CanEdit => _selected is { Status: 1 } && !IsEditing && !_saving;
    public bool CanSave => IsEditing && !_saving;
    public bool CanCancelEdit => IsEditing && !_saving;
    public bool CanRefresh => !IsEditing && !_saving;
    public bool CanActivate => _selected is { Status: 1 } && !IsEditing && !_saving;
    public bool CanTerminate => _selected is { Status: 2 } && !IsEditing && !_saving;
    public bool CanCancelContract => _selected is { Status: 1 } && !IsEditing && !_saving;
    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit;
    private string EditorTitle => _mode switch { EditorMode.Create=>"عقد جديد", EditorMode.Edit=>"تعديل العقد", EditorMode.View=>"تفاصيل العقد", _=>"تفاصيل العقد" };

    protected override async Task OnParametersSetAsync()
    {
        if (EmployeeId == Guid.Empty || EmployeeId == _loadedEmployeeId) return;
        _loadedEmployeeId = EmployeeId;
        await LoadAsync();
    }

    public async Task NewAsync()
    {
        if (!CanNew) return;
        _selected = null; _mode = EditorMode.Create; _contractTypeValue="1"; _startDate=DateOnly.FromDateTime(DateTime.Today); _endDate=null; _probationEndDate=null; _workingHoursText="8"; _workingDaysText="6"; _notes=null;
        _currencyValue = CurrencyOptions.FirstOrDefault()?.Value ?? string.Empty;
        await ChangedAsync();
    }
    public async Task EditAsync() { if (!CanEdit || _selected is null) return; _mode=EditorMode.Edit; LoadForm(_selected); await ChangedAsync(); }
    public async Task CancelEditAsync() { if (!CanCancelEdit) return; if (_selected is null) { _mode=EditorMode.Empty; ClearForm(); } else { _mode=EditorMode.View; LoadForm(_selected); } await ChangedAsync(); }
    public Task RefreshAsync() => LoadAsync();
    public async Task SaveAsync()
    {
        if (!CanSave || _startDate is null || !byte.TryParse(_contractTypeValue, out var type) || !Guid.TryParse(_currencyValue, out var currencyId)) { Snackbar.Error("تحقق من نوع العقد وتاريخ البداية والعملة."); return; }
        if (!TryNullableDecimal(_workingHoursText, out var hours) || !TryNullableDecimal(_workingDaysText, out var days)) { Snackbar.Error("ساعات وأيام العمل يجب أن تكون أرقامًا صحيحة."); return; }
        _saving=true; await ChangedAsync();
        try
        {
            ApiCallResult<EmployeeContractDto> result;
            if (_mode==EditorMode.Create)
                result=await ContractService.CreateAsync(EmployeeId,new CreateEmployeeContractRequest(type,_startDate.Value,_endDate,_probationEndDate,hours,days,currencyId,NullIfEmpty(_notes)));
            else if (_selected is not null)
                result=await ContractService.UpdateAsync(_selected.Id,new UpdateEmployeeContractRequest(type,_startDate.Value,_endDate,_probationEndDate,hours,days,currencyId,NullIfEmpty(_notes),_selected.RowVersion));
            else return;
            if (!result.Succeeded || result.Value is null) { if(result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected(); return; }
            _selected=result.Value; _mode=EditorMode.View; await LoadAsync(); Snackbar.Success("تم حفظ العقد بنجاح.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);} catch{ApiFeedback.ShowUnexpected();}
        finally{_saving=false;await ChangedAsync();}
    }
    public async Task ActivateAsync() { if(!CanActivate||_selected is null)return; var result=await ContractService.ActivateAsync(_selected.Id,new ContractLifecycleRequest(_selected.RowVersion)); await HandleLifecycleAsync(result,"تم تفعيل العقد."); }
    public async Task CancelContractAsync() { if(!CanCancelContract||_selected is null)return; var result=await ContractService.CancelAsync(_selected.Id,new ContractLifecycleRequest(_selected.RowVersion)); await HandleLifecycleAsync(result,"تم إلغاء العقد."); }
    public async Task TerminateAsync()
    {
        if(!CanTerminate||_selected is null)return;
        if(string.IsNullOrWhiteSpace(_terminationReason)){Snackbar.Error("اكتب سبب إنهاء العقد أولًا.");return;}
        if(_terminationEffectiveDate is null){Snackbar.Error("حدد تاريخ آخر يوم عمل.");return;}
        var result=await ContractService.TerminateAsync(_selected.Id,new TerminateEmployeeContractRequest(_terminationReason.Trim(),_terminationEffectiveDate.Value,_selected.RowVersion)); await HandleLifecycleAsync(result,"تم إنهاء العقد.");
    }

    private async Task HandleLifecycleAsync(ApiCallResult<EmployeeContractDto> result,string message)
    {
        if(!result.Succeeded||result.Value is null){if(result.Error is not null)ApiFeedback.Show(result.Error);else ApiFeedback.ShowUnexpected();return;}
        _selected=result.Value; _mode=EditorMode.View; await LoadAsync(); Snackbar.Success(message); await ChangedAsync();
    }
    private async Task LoadAsync()
    {
        _loading=true; await ChangedAsync();
        try
        {
            var currencies=await Accounting.GetCurrenciesPageAsync(new PageRequest{PageNumber=1,PageSize=200,SortBy="Code",SortDirection=SortDirection.Ascending});
            CurrencyOptions=currencies.Items.Where(x=>x.IsActive||x.Id==_selected?.CurrencyId).Select(x=>new UiSelectOption(x.Id.ToString("D"),$"{x.Code} - {x.NameAr}")).ToArray();
            var selectedId=_selected?.Id; _items=await ContractService.GetForEmployeeAsync(EmployeeId); _selected=selectedId.HasValue?_items.FirstOrDefault(x=>x.Id==selectedId):_items.FirstOrDefault();
            if(_selected is not null){_mode=EditorMode.View;LoadForm(_selected);}else{_mode=EditorMode.Empty;ClearForm();}
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);} catch{ApiFeedback.ShowUnexpected();}
        finally{_loading=false;await ChangedAsync();}
    }
    private void SelectItem(EmployeeContractDto item){if(IsEditing||_saving)return;_selected=item;_mode=EditorMode.View;LoadForm(item);_=ChangedAsync();}
    private void LoadForm(EmployeeContractDto x){_contractTypeValue=x.ContractType.ToString();_startDate=x.StartDate;_endDate=x.EndDate;_probationEndDate=x.ProbationEndDate;_workingHoursText=x.WorkingHoursPerDay?.ToString(System.Globalization.CultureInfo.InvariantCulture)??string.Empty;_workingDaysText=x.WorkingDaysPerWeek?.ToString(System.Globalization.CultureInfo.InvariantCulture)??string.Empty;_currencyValue=x.CurrencyId.ToString("D");_notes=x.Notes;_terminationReason=null;_terminationEffectiveDate=x.TerminationEffectiveDate??DateOnly.FromDateTime(DateTime.Today);}
    private void ClearForm(){_contractTypeValue="1";_startDate=null;_endDate=null;_probationEndDate=null;_terminationEffectiveDate=null;_workingHoursText=string.Empty;_workingDaysText=string.Empty;_currencyValue=string.Empty;_notes=null;_terminationReason=null;}
    private Task SetContractTypeAsync(string? value){_contractTypeValue=value??"1";return Task.CompletedTask;}
    private Task SetCurrencyAsync(string? value){_currencyValue=value??string.Empty;return Task.CompletedTask;}
    private Task ChangedAsync()=>OnStateChanged.InvokeAsync();
    private static bool TryNullableDecimal(string? value,out decimal? result){result=null;if(string.IsNullOrWhiteSpace(value))return true;if(decimal.TryParse(value,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.CurrentCulture,out var x)||decimal.TryParse(value,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out x)){result=x;return true;}return false;}
    private static string? NullIfEmpty(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static string TypeName(byte value)=>value switch{1=>"دائم",2=>"محدد المدة",3=>"دوام جزئي",4=>"مؤقت",_=>"—"};
    private static string StatusName(byte value)=>value switch{1=>"مسودة",2=>"فعال",3=>"منتهي",4=>"منهى",5=>"ملغي",_=>"—"};
    private static AlertTone StatusTone(byte value)=>value switch{2=>AlertTone.Success,4 or 5=>AlertTone.Danger,3=>AlertTone.Warning,_=>AlertTone.Info};
}
