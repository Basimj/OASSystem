using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Identity.Services;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Identity.Roles;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Identity.Pages;

public partial class Roles
{
    private enum RoleEditorMode { Empty, View, Create, Edit }

    [Inject] private IUserClientService UserService { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private BrowserFileDownloadService BrowserFileDownload { get; set; } = default!;

    private IReadOnlyList<RoleDto> _roles = [];
    private RoleDto? _selectedRole;
    private RoleEditorMode _mode = RoleEditorMode.Empty;
    private string? _search;
    private string? _formName;
    private string? _formDisplayName;
    private bool _loading = true;
    private bool _saving;
    private bool _importing;
    private bool _exporting;

    private IReadOnlyList<RoleDto> FilteredRoles
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_search)) return _roles;
            var term = _search.Trim();
            return _roles.Where(x =>
                    x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.DisplayName.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    GetRoleDisplayName(x).Contains(term, StringComparison.CurrentCultureIgnoreCase))
                .ToArray();
        }
    }

    private bool IsEditing => _mode is RoleEditorMode.Create or RoleEditorMode.Edit;
    private bool CanEdit => _mode == RoleEditorMode.View && _selectedRole is not null;
    private bool CanSave => IsEditing && !_saving;
    private string EditorTitle => _mode switch
    {
        RoleEditorMode.Create => L["Roles_New"],
        RoleEditorMode.Edit => L["Roles_Edit"],
        RoleEditorMode.View => L["Roles_Details"],
        _ => L["Roles_Details"]
    };
    private string? EditorSubtitle => _selectedRole?.Name;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync(bool preserveSelection = true)
    {
        _loading = true;
        try
        {
            var selectedId = preserveSelection ? _selectedRole?.Id : null;
            _roles = await UserService.GetRolesAsync();
            _selectedRole = selectedId.HasValue ? _roles.FirstOrDefault(x => x.Id == selectedId.Value) : null;
            if (_selectedRole is not null)
            {
                _mode = RoleEditorMode.View;
                LoadForm(_selectedRole);
            }
            else if (_mode != RoleEditorMode.Create)
            {
                _mode = RoleEditorMode.Empty;
                ClearForm();
            }
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void SelectRole(RoleDto role)
    {
        if (_saving || _importing || IsEditing) return;
        _selectedRole = role;
        _mode = RoleEditorMode.View;
        LoadForm(role);
    }

    private Task BeginCreateAsync(MouseEventArgs _)
    {
        _selectedRole = null;
        _mode = RoleEditorMode.Create;
        ClearForm();
        return Task.CompletedTask;
    }

    private Task BeginEditAsync(MouseEventArgs _)
    {
        if (_selectedRole is null) return Task.CompletedTask;
        _mode = RoleEditorMode.Edit;
        LoadForm(_selectedRole);
        return Task.CompletedTask;
    }

    private Task CancelEditAsync(MouseEventArgs _)
    {
        if (_selectedRole is null)
        {
            _mode = RoleEditorMode.Empty;
            ClearForm();
        }
        else
        {
            _mode = RoleEditorMode.View;
            LoadForm(_selectedRole);
        }
        return Task.CompletedTask;
    }

    private async Task SaveAsync(MouseEventArgs _)
    {
        if (!CanSave || !ValidateForm()) return;
        _saving = true;
        try
        {
            if (_mode == RoleEditorMode.Create)
            {
                var result = await UserService.CreateRoleAsync(new CreateRoleRequest(_formName!.Trim(), _formDisplayName!.Trim()));
                if (!TryGet(result, out var created)) return;
                _selectedRole = created;
                Snackbar.Success(L["Roles_Created"]);
            }
            else if (_selectedRole is not null)
            {
                var result = await UserService.UpdateRoleDisplayNameAsync(
                    _selectedRole.Id,
                    new UpdateRoleDisplayNameRequest(_formDisplayName!.Trim()));
                if (!TryGet(result, out var updated)) return;
                _selectedRole = updated;
                Snackbar.Success(L["Roles_Updated"]);
            }

            await LoadAsync();
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task RefreshAsync(MouseEventArgs _) => await LoadAsync();

    private Task SearchChangedAsync(string? _) => Task.CompletedTask;

    private async Task ExportAsync(MouseEventArgs _)
    {
        if (_exporting) return;
        _exporting = true;
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("Name,DisplayName");
            foreach (var role in FilteredRoles)
            {
                builder.Append(Csv(role.Name)).Append(',').Append(Csv(role.DisplayName)).AppendLine();
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            await BrowserFileDownload.DownloadAsync(bytes, $"OAS-Roles-{DateTime.Now:yyyyMMdd-HHmm}.csv", "text/csv;charset=utf-8");
            Snackbar.Success(L["Roles_Exported"]);
        }
        finally
        {
            _exporting = false;
        }
    }

    private async Task ImportAsync(InputFileChangeEventArgs args)
    {
        if (_importing || IsEditing) return;
        var file = args.File;
        if (file.Size <= 0 || file.Size > 1_000_000)
        {
            Snackbar.Error(L["Roles_ImportSize"]);
            return;
        }

        _importing = true;
        try
        {
            await using var stream = file.OpenReadStream(1_000_000);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync();
            var items = ParseCsv(text);
            if (items.Count == 0)
            {
                Snackbar.Warning(L["Roles_ImportEmpty"]);
                return;
            }

            var result = await UserService.ImportRolesAsync(new ImportRolesRequest(items));
            if (!TryGet(result, out var imported)) return;
            Snackbar.Success(string.Format(L["Roles_ImportSuccess"], imported.CreatedCount, imported.UpdatedCount, imported.UnchangedCount));
            await LoadAsync();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException)
        {
            Snackbar.Error(L["Roles_ImportInvalid"]);
        }
        finally
        {
            _importing = false;
        }
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(_formName) || string.IsNullOrWhiteSpace(_formDisplayName))
        {
            Snackbar.Error(L["Validation_RequiredFields"]);
            return false;
        }

        if (_mode == RoleEditorMode.Create && !System.Text.RegularExpressions.Regex.IsMatch(_formName.Trim(), "^[A-Za-z][A-Za-z0-9_.-]*$"))
        {
            Snackbar.Error(L["Roles_KeyInvalid"]);
            return false;
        }

        return true;
    }

    private void LoadForm(RoleDto role)
    {
        _formName = role.Name;
        _formDisplayName = role.DisplayName;
    }

    private void ClearForm()
    {
        _formName = string.Empty;
        _formDisplayName = string.Empty;
    }

    private string GetRoleDisplayName(RoleDto role)
    {
        if (!string.Equals(role.DisplayName, role.Name, StringComparison.OrdinalIgnoreCase)) return role.DisplayName;
        return role.Name switch
        {
            "Administrator" => L["Role_Administrator"],
            "User" => L["Role_User"],
            _ => role.DisplayName
        };
    }

    private bool TryGet<T>(ApiCallResult<T> result, out T value)
    {
        if (result.Succeeded && result.Value is not null)
        {
            value = result.Value;
            return true;
        }

        if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected();
        value = default!;
        return false;
    }

    private static string Csv(string? value)
    {
        var normalized = value ?? string.Empty;
        if (normalized.Length > 0 && normalized[0] is '=' or '+' or '-' or '@' or '\t')
            normalized = "'" + normalized;
        return "\"" + normalized.Replace("\"", "\"\"") + "\"";
    }

    private static IReadOnlyList<RoleImportItem> ParseCsv(string text)
    {
        var result = new List<RoleImportItem>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var firstDataLine = true;
        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var columns = ParseCsvLine(raw);
            if (columns.Count < 2) throw new InvalidDataException("CSV must contain Name and DisplayName columns.");

            if (firstDataLine && string.Equals(columns[0].Trim().TrimStart('\uFEFF'), "Name", StringComparison.OrdinalIgnoreCase))
            {
                firstDataLine = false;
                continue;
            }

            firstDataLine = false;
            var name = columns[0].Trim().TrimStart('\uFEFF');
            var displayName = columns[1].Trim();
            if (name.Length == 0 && displayName.Length == 0) continue;
            result.Add(new RoleImportItem(name, displayName));
        }
        return result;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (ch == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (ch == ',' && !quoted)
            {
                result.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(ch);
            }
        }
        if (quoted) throw new InvalidDataException("Invalid CSV quoting.");
        result.Add(value.ToString());
        return result;
    }
}
