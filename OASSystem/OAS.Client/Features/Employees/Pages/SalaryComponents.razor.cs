using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Compensation;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Pages;

public partial class SalaryComponents
{
    private static readonly IReadOnlyList<string> SalaryComponentGridHeaders = ["الكود", "المكون", "النوع", "طريقة الاحتساب", "الحالة"];
    private enum EditorMode { Empty, View, Create, Edit }
    [Inject] private IEmployeeCompensationClientService Compensation { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private IReadOnlyList<SalaryComponentDto> _items = [];
    private SalaryComponentDto? _selected;
    private EditorMode _mode = EditorMode.Empty;
    private string? _search, _nameAr, _nameEn, _debitRole, _creditRole, _notes;
    private string _typeValue = "1", _methodValue = "1", _displayOrderText = "0";
    private bool _isBasic, _isRecurring = true, _isTaxable, _isActive = true, _loading = true, _saving;

    private static readonly IReadOnlyList<UiSelectOption> TypeOptions = [new("1","استحقاق"),new("2","استقطاع"),new("3","مساهمة جهة العمل")];
    private static readonly IReadOnlyList<UiSelectOption> MethodOptions = [new("1","مبلغ ثابت"),new("2","نسبة من الأساسي"),new("3","بالساعة"),new("4","باليوم"),new("5","يدوي"),new("6","مصدر خارجي")];

    private IReadOnlyList<SalaryComponentDto> FilteredItems => string.IsNullOrWhiteSpace(_search) ? _items : _items.Where(x => x.ComponentCode.Contains(_search.Trim(), StringComparison.OrdinalIgnoreCase) || x.NameAr.Contains(_search.Trim(), StringComparison.CurrentCultureIgnoreCase) || (!string.IsNullOrWhiteSpace(x.NameEn) && x.NameEn.Contains(_search.Trim(), StringComparison.CurrentCultureIgnoreCase))).ToArray();
    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit;
    private bool CanEdit => _mode == EditorMode.View && _selected is not null;
    private bool CanSave => IsEditing && !_saving;
    private string EditorTitle => _mode switch { EditorMode.Create => "مكون راتب جديد", EditorMode.Edit => "تعديل مكون الراتب", EditorMode.View => "تفاصيل مكون الراتب", _ => "تفاصيل مكون الراتب" };
    private string? EditorSubtitle => _selected?.NameAr;

    protected override async Task OnInitializedAsync() => await LoadAsync();
    private async Task LoadAsync(bool preserveSelection = true)
    {
        _loading = true;
        try
        {
            var id = preserveSelection ? _selected?.Id : null;
            _items = await Compensation.GetComponentsAsync();
            _selected = id.HasValue ? _items.FirstOrDefault(x => x.Id == id.Value) : null;
            if (_selected is not null) { _mode = EditorMode.View; LoadForm(_selected); }
            else if (_mode != EditorMode.Create) { _mode = EditorMode.Empty; ClearForm(); }
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _loading = false; }
    }
    private void SelectItem(SalaryComponentDto item) { if (IsEditing || _saving) return; _selected = item; _mode = EditorMode.View; LoadForm(item); }
    private Task BeginCreateAsync(MouseEventArgs _) { if (IsEditing) return Task.CompletedTask; _selected = null; _mode = EditorMode.Create; ClearForm(); return Task.CompletedTask; }
    private Task BeginEditAsync(MouseEventArgs _) { if (_selected is null || _mode != EditorMode.View) return Task.CompletedTask; _mode = EditorMode.Edit; LoadForm(_selected); return Task.CompletedTask; }
    private Task CancelEditAsync(MouseEventArgs _) { if (_selected is not null) { _mode = EditorMode.View; LoadForm(_selected); } else { _mode = EditorMode.Empty; ClearForm(); } return Task.CompletedTask; }
    private async Task SaveAsync(MouseEventArgs _)
    {
        if (!IsEditing || _saving) return;
        if (string.IsNullOrWhiteSpace(_nameAr)) { Snackbar.Error("اسم مكون الراتب مطلوب."); return; }
        if (!byte.TryParse(_typeValue, out var type) || !byte.TryParse(_methodValue, out var method) || !int.TryParse(_displayOrderText, out var order)) { Snackbar.Error("تحقق من نوع المكون وطريقة الاحتساب وترتيب العرض."); return; }
        _saving = true;
        try
        {
            ApiCallResult<SalaryComponentDto> result;
            if (_mode == EditorMode.Create)
                result = await Compensation.CreateComponentAsync(new CreateSalaryComponentRequest(_nameAr.Trim(), NullIfEmpty(_nameEn), type, method, _isBasic, _isRecurring, _isTaxable, _isActive, NullIfEmpty(_debitRole), NullIfEmpty(_creditRole), order, NullIfEmpty(_notes)));
            else if (_selected is not null)
                result = await Compensation.UpdateComponentAsync(_selected.Id, new UpdateSalaryComponentRequest(_nameAr.Trim(), NullIfEmpty(_nameEn), type, method, _isBasic, _isRecurring, _isTaxable, _isActive, NullIfEmpty(_debitRole), NullIfEmpty(_creditRole), order, NullIfEmpty(_notes), _selected.RowVersion));
            else return;
            if (!result.Succeeded || result.Value is null) { if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected(); return; }
            _selected = result.Value; _mode = EditorMode.View; await LoadAsync(); Snackbar.Success("تم حفظ مكون الراتب بنجاح.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _saving = false; }
    }
    private async Task RefreshAsync(MouseEventArgs _) => await LoadAsync();
    private Task SearchChangedAsync(string? value) { _search = value; return Task.CompletedTask; }
    private Task SetTypeAsync(string? value) { _typeValue = value ?? "1"; return Task.CompletedTask; }
    private Task SetMethodAsync(string? value) { _methodValue = value ?? "1"; return Task.CompletedTask; }
    private void LoadForm(SalaryComponentDto x) { _nameAr=x.NameAr; _nameEn=x.NameEn; _typeValue=x.ComponentType.ToString(); _methodValue=x.CalculationMethod.ToString(); _isBasic=x.IsBasicSalary; _isRecurring=x.IsRecurring; _isTaxable=x.IsTaxable; _isActive=x.IsActive; _debitRole=x.DebitPostingRole; _creditRole=x.CreditPostingRole; _displayOrderText=x.DisplayOrder.ToString(); _notes=x.Notes; }
    private void ClearForm() { _nameAr=string.Empty; _nameEn=string.Empty; _typeValue="1"; _methodValue="1"; _isBasic=false; _isRecurring=true; _isTaxable=false; _isActive=true; _debitRole=null; _creditRole=null; _displayOrderText="0"; _notes=null; }
    private static string TypeName(byte value) => value switch { 1=>"استحقاق",2=>"استقطاع",3=>"مساهمة جهة العمل",_=>"—" };
    private static string MethodName(byte value) => value switch { 1=>"مبلغ ثابت",2=>"نسبة من الأساسي",3=>"بالساعة",4=>"باليوم",5=>"يدوي",6=>"مصدر خارجي",_=>"—" };
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
