using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.JobTitles;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Pages;

public partial class JobTitles
{
    private enum EditorMode { Empty, View, Create, Edit }

    [Inject] private IEmployeeClientService EmployeeService { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private BrowserFileDownloadService BrowserFileDownload { get; set; } = default!;

    private IReadOnlyList<JobTitleDto> _items = [];
    private JobTitleDto? _selected;
    private EditorMode _mode = EditorMode.Empty;
    private string? _search;
    private string? _formName;
    private bool _formIsActive = true;
    private bool _loading = true;
    private bool _saving;
    private bool _importing;
    private bool _exporting;

    private IReadOnlyList<JobTitleDto> FilteredItems =>
        string.IsNullOrWhiteSpace(_search)
            ? _items
            : _items.Where(x => x.Name.Contains(_search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();

    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit;
    private bool CanEdit => _mode == EditorMode.View && _selected is not null;
    private bool CanSave => IsEditing && !_saving;
    private string EditorTitle => _mode switch
    {
        EditorMode.Create => "مسمى وظيفي جديد",
        EditorMode.Edit => "تعديل المسمى الوظيفي",
        EditorMode.View => "تفاصيل المسمى الوظيفي",
        _ => "تفاصيل المسمى الوظيفي"
    };
    private string? EditorSubtitle => _selected?.Name;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync(bool preserveSelection = true)
    {
        _loading = true;
        try
        {
            var selectedId = preserveSelection ? _selected?.Id : null;
            _items = await EmployeeService.GetJobTitlesAsync();
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
        finally { _loading = false; }
    }

    private void SelectItem(JobTitleDto item)
    {
        if (_saving || _importing || IsEditing) return;
        _selected = item;
        _mode = EditorMode.View;
        LoadForm(item);
    }

    private Task BeginCreateAsync(MouseEventArgs _)
    {
        if (IsEditing) return Task.CompletedTask;
        _selected = null;
        _mode = EditorMode.Create;
        _formName = string.Empty;
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
        var name = _formName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Snackbar.Error("اسم المسمى الوظيفي مطلوب.");
            return;
        }

        _saving = true;
        try
        {
            ApiCallResult<JobTitleDto> result;
            if (_mode == EditorMode.Create)
            {
                result = await EmployeeService.CreateJobTitleAsync(new CreateJobTitleRequest(name, _formIsActive));
            }
            else if (_selected is not null)
            {
                result = await EmployeeService.UpdateJobTitleAsync(
                    _selected.Id,
                    new UpdateJobTitleRequest(name, _formIsActive, _selected.RowVersion));
            }
            else return;

            if (!result.Succeeded || result.Value is null)
            {
                if (result.Error is not null) ApiFeedback.Show(result.Error);
                else Snackbar.Error("تعذر حفظ المسمى الوظيفي.");
                return;
            }

            _selected = result.Value;
            _mode = EditorMode.View;
            await LoadAsync();
            Snackbar.Success("تم حفظ المسمى الوظيفي بنجاح.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        finally { _saving = false; }
    }

    private async Task RefreshAsync(MouseEventArgs _) => await LoadAsync();

    private Task SearchChangedAsync(string? value)
    {
        _search = value;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task ExportAsync(MouseEventArgs _)
    {
        if (_exporting) return;
        _exporting = true;
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Name,IsActive");
            foreach (var item in _items)
                csv.Append(EscapeCsv(item.Name)).Append(',').AppendLine(item.IsActive ? "true" : "false");

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            var saved = await BrowserFileDownload.SaveAsync(bytes, "JobTitles.csv", "text/csv;charset=utf-8");
            if (saved) Snackbar.Success("تم تصدير المسميات الوظيفية.");
        }
        finally
        {
            _exporting = false;
        }
    }

    private async Task ImportAsync(InputFileChangeEventArgs args)
    {
        if (_importing || IsEditing) return;
        _importing = true;
        try
        {
            var file = args.File;
            if (file.Size <= 0 || file.Size > 1_000_000)
            {
                Snackbar.Error("ملف الاستيراد غير صالح أو أكبر من 1 MB.");
                return;
            }

            await using var stream = file.OpenReadStream(1_000_000);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var text = await reader.ReadToEndAsync();
            var rows = ParseCsv(text);
            if (rows.Count == 0)
            {
                Snackbar.Info("لا توجد بيانات للاستيراد.");
                return;
            }

            var current = (await EmployeeService.GetJobTitlesAsync()).ToDictionary(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
            var created = 0;
            var updated = 0;

            foreach (var row in rows)
            {
                if (current.TryGetValue(row.Name, out var existing))
                {
                    if (existing.IsActive == row.IsActive) continue;
                    var result = await EmployeeService.UpdateJobTitleAsync(
                        existing.Id,
                        new UpdateJobTitleRequest(existing.Name, row.IsActive, existing.RowVersion));
                    if (result.Succeeded && result.Value is not null)
                    {
                        current[existing.Name] = result.Value;
                        updated++;
                    }
                }
                else
                {
                    var result = await EmployeeService.CreateJobTitleAsync(new CreateJobTitleRequest(row.Name, row.IsActive));
                    if (result.Succeeded && result.Value is not null)
                    {
                        current[row.Name] = result.Value;
                        created++;
                    }
                }
            }

            await LoadAsync(false);
            Snackbar.Success($"اكتمل الاستيراد: {created} جديد، {updated} محدث.");
        }
        catch (Exception ex)
        {
            Snackbar.Error($"تعذر استيراد المسميات الوظيفية: {ex.Message}");
        }
        finally
        {
            _importing = false;
        }
    }

    private void LoadForm(JobTitleDto item)
    {
        _formName = item.Name;
        _formIsActive = item.IsActive;
    }

    private void ClearForm()
    {
        _formName = null;
        _formIsActive = true;
    }

    private static string FormatDate(DateTimeOffset value) =>
        value.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

    private static string EscapeCsv(string value)
    {
        var safe = value;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static IReadOnlyList<JobTitleImportRow> ParseCsv(string text)
    {
        var result = new List<JobTitleImportRow>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var first = true;
        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var columns = ParseCsvLine(raw);
            if (first && columns.Count > 0 && string.Equals(columns[0].Trim().TrimStart('\uFEFF'), "Name", StringComparison.OrdinalIgnoreCase))
            {
                first = false;
                continue;
            }
            first = false;
            if (columns.Count < 1) continue;
            var name = columns[0].Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(name)) continue;
            var isActive = columns.Count < 2 || ParseBool(columns[1]);
            result.Add(new JobTitleImportRow(name, isActive));
        }
        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (ch == ',' && !quoted)
            {
                result.Add(value.ToString());
                value.Clear();
            }
            else value.Append(ch);
        }
        result.Add(value.ToString());
        return result;
    }

    private static bool ParseBool(string value) =>
        value.Trim().ToLowerInvariant() is "true" or "1" or "yes" or "نعم" or "active" or "نشط";

    private sealed record JobTitleImportRow(string Name, bool IsActive);
}
