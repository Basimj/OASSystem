using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees.Departments;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Pages;

public partial class Departments
{
    private static readonly IReadOnlyList<string> DepartmentGridHeaders = ["الكود", "القسم", "القسم الأب", "المدير", "الحالة"];
    private enum EditorMode { Empty, View, Create, Edit }

    [Inject] private IDepartmentClientService DepartmentService { get; set; } = default!;
    [Inject] private IEmployeeClientService EmployeeService { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private IReadOnlyList<DepartmentDto> _items = [];
    private DepartmentDto? _selected;
    private EditorMode _mode = EditorMode.Empty;
    private string? _search;
    private string? _formNameAr;
    private string? _formNameEn;
    private Guid? _formParentDepartmentId;
    private Guid? _formManagerEmployeeId;
    private string? _formManagerName;
    private string? _formManagerCode;
    private bool _formIsActive = true;
    private string? _formNotes;
    private bool _loading = true;
    private bool _saving;

    private IReadOnlyList<DepartmentDto> FilteredItems
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_search)) return _items;
            var search = _search.Trim();
            return _items.Where(x =>
                    x.DepartmentCode.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    x.NameAr.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(x.NameEn) && x.NameEn.Contains(search, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(x.ManagerEmployeeName) && x.ManagerEmployeeName.Contains(search, StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();
        }
    }

    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit;
    private bool CanEdit => _mode == EditorMode.View && _selected is not null;
    private bool CanSave => IsEditing && !_saving;
    private string EditorTitle => _mode switch { EditorMode.Create => "قسم جديد", EditorMode.Edit => "تعديل القسم", EditorMode.View => "تفاصيل القسم", _ => "تفاصيل القسم" };
    private string? EditorSubtitle => _selected?.NameAr;
    private string DepartmentCodeDisplay => _selected?.DepartmentCode ?? "سيولد تلقائيًا";
    private string? ParentDepartmentValue => _formParentDepartmentId?.ToString("D");
    private string? ManagerEmployeeValue => _formManagerEmployeeId?.ToString("D");

    private IReadOnlyList<UiSelectOption> ParentDepartmentOptions => _items
        .Where(x => x.IsActive && x.Id != _selected?.Id)
        .OrderBy(x => x.NameAr)
        .Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.DepartmentCode} - {x.NameAr}"))
        .ToArray();

    private UiLookupItem? ManagerEmployeeItem => _formManagerEmployeeId is Guid id && !string.IsNullOrWhiteSpace(_formManagerName)
        ? new UiLookupItem(id.ToString("D"), _formManagerName!, _formManagerCode, "fa-solid fa-user-tie")
        : null;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync(bool preserveSelection = true)
    {
        _loading = true;
        try
        {
            var selectedId = preserveSelection ? _selected?.Id : null;
            _items = await DepartmentService.GetAsync();
            _selected = selectedId.HasValue ? _items.FirstOrDefault(x => x.Id == selectedId.Value) : null;
            if (_selected is not null)
            {
                _mode = EditorMode.View;
                LoadForm(_selected);
            }
            else if (_mode != EditorMode.Create)
            {
                _mode = EditorMode.Empty;
                ClearForm();
            }
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _loading = false; }
    }

    private void SelectItem(DepartmentDto item)
    {
        if (_saving || IsEditing) return;
        _selected = item;
        _mode = EditorMode.View;
        LoadForm(item);
    }

    private Task BeginCreateAsync(MouseEventArgs _)
    {
        if (IsEditing) return Task.CompletedTask;
        _selected = null;
        _mode = EditorMode.Create;
        ClearForm();
        _formIsActive = true;
        return Task.CompletedTask;
    }

    private Task BeginEditAsync(MouseEventArgs _)
    {
        if (_selected is null || _mode != EditorMode.View) return Task.CompletedTask;
        _mode = EditorMode.Edit;
        LoadForm(_selected);
        return Task.CompletedTask;
    }

    private Task CancelEditAsync(MouseEventArgs _)
    {
        if (_selected is not null)
        {
            _mode = EditorMode.View;
            LoadForm(_selected);
        }
        else
        {
            _mode = EditorMode.Empty;
            ClearForm();
        }
        return Task.CompletedTask;
    }

    private async Task SaveAsync(MouseEventArgs _)
    {
        if (!IsEditing || _saving) return;
        if (string.IsNullOrWhiteSpace(_formNameAr))
        {
            Snackbar.Error("اسم القسم مطلوب.");
            return;
        }

        _saving = true;
        try
        {
            ApiCallResult<DepartmentDto> result;
            if (_mode == EditorMode.Create)
            {
                result = await DepartmentService.CreateAsync(new CreateDepartmentRequest(
                    _formNameAr.Trim(), NullIfEmpty(_formNameEn), _formParentDepartmentId, _formManagerEmployeeId, _formIsActive, NullIfEmpty(_formNotes)));
            }
            else if (_selected is not null)
            {
                result = await DepartmentService.UpdateAsync(_selected.Id, new UpdateDepartmentRequest(
                    _formNameAr.Trim(), NullIfEmpty(_formNameEn), _formParentDepartmentId, _formManagerEmployeeId, _formIsActive, NullIfEmpty(_formNotes), _selected.RowVersion));
            }
            else return;

            if (!result.Succeeded || result.Value is null)
            {
                if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected();
                return;
            }

            _selected = result.Value;
            _mode = EditorMode.View;
            await LoadAsync();
            Snackbar.Success("تم حفظ القسم بنجاح.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _saving = false; }
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchManagerEmployeesAsync(string search, CancellationToken cancellationToken)
    {
        var page = await EmployeeService.GetPageAsync(new PageRequest
        {
            PageNumber = 1,
            PageSize = 12,
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            SortBy = "EmployeeCode",
            SortDirection = SortDirection.Ascending
        }, cancellationToken);

        return page.Items.Where(x => x.IsActive).Select(x => new UiLookupItem(
            x.Id.ToString("D"), x.DisplayName, $"{x.EmployeeCode} • {x.JobTitleName}", "fa-solid fa-user-tie")).ToArray();
    }

    private Task SetParentDepartmentAsync(string? value)
    {
        _formParentDepartmentId = Guid.TryParse(value, out var id) ? id : null;
        return Task.CompletedTask;
    }

    private async Task SetManagerEmployeeAsync(string? value)
    {
        _formManagerEmployeeId = Guid.TryParse(value, out var id) ? id : null;
        if (_formManagerEmployeeId is null)
        {
            _formManagerName = null;
            _formManagerCode = null;
        }
        else
        {
            var manager = await EmployeeService.GetByIdAsync(_formManagerEmployeeId.Value);
            _formManagerName = manager.DisplayName;
            _formManagerCode = manager.EmployeeCode;
        }
    }

    private async Task RefreshAsync(MouseEventArgs _) => await LoadAsync();
    private Task SearchChangedAsync(string? value) { _search = value; return Task.CompletedTask; }

    private void LoadForm(DepartmentDto item)
    {
        _formNameAr = item.NameAr;
        _formNameEn = item.NameEn;
        _formParentDepartmentId = item.ParentDepartmentId;
        _formManagerEmployeeId = item.ManagerEmployeeId;
        _formManagerName = item.ManagerEmployeeName;
        _formManagerCode = null;
        _formIsActive = item.IsActive;
        _formNotes = item.Notes;
    }

    private void ClearForm()
    {
        _formNameAr = string.Empty;
        _formNameEn = string.Empty;
        _formParentDepartmentId = null;
        _formManagerEmployeeId = null;
        _formManagerName = null;
        _formManagerCode = null;
        _formIsActive = true;
        _formNotes = string.Empty;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
