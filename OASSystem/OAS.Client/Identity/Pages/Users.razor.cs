using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Identity.Services;
using OAS.Client.Identity.State;
using OAS.Client.Identity.Users.Components;
using OAS.Client.Identity.Users.Workspace;
using OAS.Client.Services.Http;
using OAS.Client.Services.Browser;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Roles;
using OAS.Contracts.Identity.Users;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Identity.Pages;

public partial class Users
{
    [Inject] private IUserClientService UserService { get; set; } = default!;
    [Inject] private IAuthClientService AuthService { get; set; } = default!;
    [Inject] private OasAuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IUiDialogService Dialogs { get; set; } = default!;
    [Inject] private IUsersWorkspaceState Workspace { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private BrowserFileDownloadService BrowserFileDownload { get; set; } = default!;

    private PagedResult<UserSummaryDto> _page = new() { PageNumber = 1, PageSize = 20 };
    private IReadOnlyList<RoleDto> _roles = [];
    private string? _search;
    private string? _statusFilter = "all";
    private string? _roleFilter;
    private int _pageNumber = 1;
    private bool _loading = true;
    private bool _saving;
    private bool _exporting;

    private IReadOnlyList<UiSelectOption> StatusFilterOptions =>
    [
        new("all", L["Users_AllStatuses"]),
        new("active", L["Users_Active"]),
        new("inactive", L["Users_Inactive"])
    ];

    private IReadOnlyList<UiSelectOption> RoleFilterOptions =>
        _roles.Select(role => new UiSelectOption(role.Id.ToString("D"), TranslateRole(role.Name))).ToArray();

    private bool? SelectedStatusFilter => _statusFilter switch
    {
        "active" => true,
        "inactive" => false,
        _ => null
    };

    private Guid? SelectedRoleFilter => Guid.TryParse(_roleFilter, out var roleId) ? roleId : null;

    private IReadOnlyList<UiWorkspaceTabItem> WorkspaceTabs =>
    [
        new(Workspace.AllUsersTabId, L["Users_All"], Workspace.ActiveTabId == Workspace.AllUsersTabId, false, false),
        .. Workspace.UserTabs.Select(tab => new UiWorkspaceTabItem(
            tab.WorkspaceTabId,
            tab.Title,
            Workspace.ActiveTabId == tab.WorkspaceTabId,
            tab.IsDirty,
            true))
    ];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _roles = await UserService.GetRolesAsync();
            await LoadPageAsync();
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

    private async Task LoadPageAsync()
    {
        _loading = true;
        try
        {
            _page = await UserService.GetUsersAsync(
                new PageRequest
                {
                    PageNumber = _pageNumber,
                    PageSize = 20,
                    Search = _search,
                    SortBy = "UserName",
                    SortDirection = SortDirection.Ascending
                },
                SelectedStatusFilter,
                SelectedRoleFilter);
            _pageNumber = _page.PageNumber <= 0 ? 1 : _page.PageNumber;
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

    private async Task SearchAsync(MouseEventArgs _)
    {
        _pageNumber = 1;
        await LoadPageAsync();
    }

    private async Task ApplyFiltersAsync(string? _)
    {
        _pageNumber = 1;
        await LoadPageAsync();
    }

    private async Task RefreshAsync(MouseEventArgs _) => await LoadPageAsync();

    private async Task ResetFiltersAsync(MouseEventArgs _)
    {
        _search = null;
        _statusFilter = "all";
        _roleFilter = null;
        _pageNumber = 1;
        await LoadPageAsync();
    }

    private async Task ExportUsersAsync(MouseEventArgs _)
    {
        if (_exporting)
            return;

        _exporting = true;
        try
        {
            var allUsers = new List<UserSummaryDto>();
            var exportPage = 1;

            while (true)
            {
                var page = await UserService.GetUsersAsync(
                    new PageRequest
                    {
                        PageNumber = exportPage,
                        PageSize = PageRequest.MaximumPageSize,
                        Search = _search,
                        SortBy = "UserName",
                        SortDirection = SortDirection.Ascending
                    },
                    SelectedStatusFilter,
                    SelectedRoleFilter);

                allUsers.AddRange(page.Items);
                if (!page.HasNextPage)
                    break;

                exportPage++;
            }

            var csv = BuildUsersCsv(allUsers);
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            await BrowserFileDownload.DownloadAsync(
                bytes,
                $"OAS-Users-{DateTime.Now:yyyyMMdd-HHmm}.csv",
                "text/csv;charset=utf-8");

            Snackbar.Success(L["Users_ExportSuccess"]);
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
        }
        finally
        {
            _exporting = false;
        }
    }

    private static string BuildUsersCsv(IEnumerable<UserSummaryDto> users)
    {
        static string Csv(string? value)
        {
            var safe = value ?? string.Empty;
            return $"\"{safe.Replace("\"", "\"\"")}\"";
        }

        var builder = new StringBuilder();
        builder.AppendLine("DisplayName,UserName,Email,Roles,Status,Locked,MustChangePassword,LastLogin");

        foreach (var user in users)
        {
            builder.Append(Csv(user.DisplayName)).Append(',')
                   .Append(Csv(user.UserName)).Append(',')
                   .Append(Csv(user.Email)).Append(',')
                   .Append(Csv(string.Join(" | ", user.Roles))).Append(',')
                   .Append(Csv(user.IsActive ? "Active" : "Inactive")).Append(',')
                   .Append(Csv(user.IsLocked ? "Yes" : "No")).Append(',')
                   .Append(Csv(user.MustChangePassword ? "Yes" : "No")).Append(',')
                   .Append(Csv(user.LastLoginAtUtc?.ToString("O")))
                   .AppendLine();
        }

        return builder.ToString();
    }

    private async Task ShowRolesOverviewAsync(MouseEventArgs _)
    {
        await Dialogs.ShowAsync<RolesOverviewDialog>(
            L["Users_RolesPermissions"],
            new Dictionary<string, object>
            {
                [nameof(RolesOverviewDialog.Roles)] = _roles
            },
            new UiDialogOptions { Size = UiDialogSize.Small });
    }

    private Task OpenNewAsync(MouseEventArgs _)
    {
        var defaultRole = _roles.FirstOrDefault(x => string.Equals(x.Name, "User", StringComparison.OrdinalIgnoreCase));
        Workspace.OpenNew(L["Users_New"], defaultRole is null ? null : [defaultRole.Id]);
        return Task.CompletedTask;
    }

    private async Task OpenUserAsync(UserSummaryDto summary)
    {
        var existing = Workspace.FindByUserId(summary.Id);
        if (existing is not null)
        {
            Workspace.Activate(existing.WorkspaceTabId);
            return;
        }

        var tab = Workspace.OpenExisting(summary.Id, string.IsNullOrWhiteSpace(summary.DisplayName) ? summary.UserName : summary.DisplayName);
        try
        {
            var details = await UserService.GetUserByIdAsync(summary.Id);
            tab.Editor.Load(details);
            tab.Title = string.IsNullOrWhiteSpace(details.DisplayName) ? details.UserName : details.DisplayName;
        }
        catch (ApiClientException ex)
        {
            Workspace.Close(tab.WorkspaceTabId);
            ApiFeedback.Show(ex.Error);
        }
        finally
        {
            tab.IsLoading = false;
        }
    }

    private Task ActivateTabAsync(Guid tabId)
    {
        Workspace.Activate(tabId);
        return Task.CompletedTask;
    }

    private async Task CloseTabAsync(Guid tabId)
    {
        var tab = Workspace.Find(tabId);
        if (tab is null) return;
        if (tab.IsDirty)
        {
            var confirmed = await Dialogs.ConfirmAsync(
                L["Users_UnsavedTitle"],
                L["Users_CloseDirtyConfirm"],
                AlertTone.Warning,
                L["Users_Discard"],
                L["Common_Cancel"]);
            if (!confirmed) return;
        }
        Workspace.Close(tabId);
    }

    private async Task SaveAsync(MouseEventArgs _) => await SaveActiveAsync(false);
    private async Task SaveAndCloseAsync(MouseEventArgs _) => await SaveActiveAsync(true);

    private async Task SaveActiveAsync(bool closeAfterSave)
    {
        var tab = Workspace.ActiveUserTab;
        if (tab is null || _saving) return;
        if (!ValidateEditor(tab.Editor)) return;

        _saving = true;
        try
        {
            if (tab.IsNew)
            {
                var create = new CreateUserRequest(
                    tab.Editor.Form.UserName.Trim(),
                    tab.Editor.Form.FirstName.Trim(),
                    tab.Editor.Form.LastName.Trim(),
                    string.IsNullOrWhiteSpace(tab.Editor.Form.Email) ? null : tab.Editor.Form.Email.Trim(),
                    tab.Editor.RoleIds.ToArray(),
                    tab.Editor.Form.IsActive);
                var result = await UserService.CreateUserAsync(create);
                if (!TryGet(result, out var created)) return;

                tab.UserId = created.User.Id;
                tab.IsNew = false;
                tab.Title = string.IsNullOrWhiteSpace(created.User.DisplayName) ? created.User.UserName : created.User.DisplayName;
                tab.Editor.Load(created.User);
                Snackbar.Success(L["Users_CreateSuccess"]);
                await ShowTemporaryPasswordAsync(created.TemporaryPassword, L["Users_TemporaryPasswordCreatedMessage"]);
                await LoadPageAsync();
            }
            else
            {
                if (tab.UserId is not Guid userId || string.IsNullOrWhiteSpace(tab.Editor.RowVersion)) return;

                if (tab.Editor.BasicDirty)
                {
                    var update = new UpdateUserRequest(
                        tab.Editor.Form.UserName.Trim(),
                        tab.Editor.Form.FirstName.Trim(),
                        tab.Editor.Form.LastName.Trim(),
                        string.IsNullOrWhiteSpace(tab.Editor.Form.Email) ? null : tab.Editor.Form.Email.Trim(),
                        tab.Editor.RowVersion);
                    var result = await UserService.UpdateUserAsync(userId, update);
                    if (!TryGet(result, out var updated)) return;
                    tab.Editor.AcceptBasicSave(updated);
                    tab.Title = string.IsNullOrWhiteSpace(updated.DisplayName) ? updated.UserName : updated.DisplayName;
                }

                if (tab.Editor.RolesDirty)
                {
                    var result = await UserService.SetUserRolesAsync(
                        userId,
                        new SetUserRolesRequest(tab.Editor.RoleIds.ToArray(), tab.Editor.RowVersion!));
                    if (!TryGet(result, out var rolesUpdated)) return;
                    tab.Editor.AcceptRolesSave(rolesUpdated);
                }

                Snackbar.Success(L["Users_UpdateSuccess"]);
                await LoadPageAsync();
            }

            if (closeAfterSave && !tab.IsDirty)
                Workspace.Close(tab.WorkspaceTabId);
        }
        finally
        {
            _saving = false;
        }
    }

    private bool ValidateEditor(UserEditorState editor)
    {
        if (string.IsNullOrWhiteSpace(editor.Form.UserName) || string.IsNullOrWhiteSpace(editor.Form.FirstName) || string.IsNullOrWhiteSpace(editor.Form.LastName))
        {
            Snackbar.Error(L["Validation_RequiredFields"]);
            return false;
        }
        if (editor.RoleIds.Count == 0)
        {
            Snackbar.Error(L["Users_RoleRequired"]);
            return false;
        }
        return true;
    }

    private async Task RevertAsync(MouseEventArgs _)
    {
        var tab = Workspace.ActiveUserTab;
        if (tab is null || !tab.IsDirty) return;
        var confirmed = await Dialogs.ConfirmAsync(L["Users_UnsavedTitle"], L["Users_RevertConfirm"], AlertTone.Warning, L["Users_Revert"], L["Common_Cancel"]);
        if (confirmed) tab.Editor.Revert();
    }

    private async Task RefreshActiveAsync(MouseEventArgs _)
    {
        var tab = Workspace.ActiveUserTab;
        if (tab?.UserId is not Guid userId) return;
        if (tab.IsDirty)
        {
            var confirmed = await Dialogs.ConfirmAsync(L["Users_UnsavedTitle"], L["Users_RefreshDirtyConfirm"], AlertTone.Warning, L["Users_Refresh"], L["Common_Cancel"]);
            if (!confirmed) return;
        }
        try
        {
            tab.IsLoading = true;
            var details = await UserService.GetUserByIdAsync(userId);
            tab.Editor.Load(details);
            tab.Title = string.IsNullOrWhiteSpace(details.DisplayName) ? details.UserName : details.DisplayName;
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        finally { tab.IsLoading = false; }
    }

    private async Task ToggleStatusAsync(MouseEventArgs _)
    {
        var tab = Workspace.ActiveUserTab;
        if (tab?.UserId is not Guid userId || tab.Editor.Details is null || string.IsNullOrWhiteSpace(tab.Editor.RowVersion)) return;
        var desired = !tab.Editor.Details.IsActive;
        var confirmed = await Dialogs.ConfirmAsync(
            desired ? L["Users_Activate"] : L["Users_Deactivate"],
            desired ? L["Users_ActivateConfirm"] : L["Users_DeactivateConfirm"],
            desired ? AlertTone.Info : AlertTone.Warning,
            desired ? L["Users_Activate"] : L["Users_Deactivate"],
            L["Common_Cancel"]);
        if (!confirmed) return;

        var result = await UserService.SetUserStatusAsync(userId, new SetUserStatusRequest(desired, tab.Editor.RowVersion));
        if (!TryGet(result, out var updated)) return;
        tab.Editor.ApplyServerState(updated);
        Snackbar.Success(L["Users_StatusUpdated"]);
        await LoadPageAsync();
    }

    private async Task UnlockAsync(MouseEventArgs _)
    {
        var tab = Workspace.ActiveUserTab;
        if (tab?.UserId is not Guid userId || string.IsNullOrWhiteSpace(tab.Editor.RowVersion)) return;
        var result = await UserService.UnlockUserAsync(userId, new UnlockUserRequest(tab.Editor.RowVersion));
        if (!TryGet(result, out var updated)) return;
        tab.Editor.ApplyServerState(updated);
        Snackbar.Success(L["Users_UnlockedSuccess"]);
        await LoadPageAsync();
    }

    private async Task ResetPasswordAsync(MouseEventArgs _)
    {
        var tab = Workspace.ActiveUserTab;
        if (tab?.UserId is not Guid userId || string.IsNullOrWhiteSpace(tab.Editor.RowVersion)) return;
        var confirmed = await Dialogs.ConfirmAsync(
            L["Users_ResetPassword"],
            L["Users_ResetPasswordConfirm"],
            AlertTone.Warning,
            L["Users_ResetPassword"],
            L["Common_Cancel"]);
        if (!confirmed) return;

        var result = await UserService.ResetUserPasswordAsync(userId, new ResetUserPasswordRequest(tab.Editor.RowVersion));
        if (!TryGet(result, out var reset)) return;

        tab.Editor.ApplyPasswordReset(reset.RowVersion);
        Snackbar.Success(L["Users_PasswordResetSuccess"]);
        await ShowTemporaryPasswordAsync(reset.TemporaryPassword, L["Users_TemporaryPasswordResetMessage"]);

        var currentUser = AuthState.CurrentUser;
        if (currentUser?.Id == userId)
        {
            // A self-reset invalidates the current credential version. Require the
            // administrator to authenticate again with the generated temporary password
            // before the mandatory password-setup flow can continue.
            try
            {
                await AuthService.LogoutAsync();
            }
            catch
            {
                // The local session is still cleared below. The login endpoint accepts
                // a stale cookie and replaces it after successful temporary-password login.
            }

            AuthState.SetAnonymous();
            Navigation.NavigateTo("/login", replace: true);
            return;
        }

        await LoadPageAsync();
    }

    private async Task ShowTemporaryPasswordAsync(string temporaryPassword, string message)
    {
        await Dialogs.ShowAsync<TemporaryPasswordDialog>(
            L["Users_TemporaryPasswordTitle"],
            new Dictionary<string, object>
            {
                [nameof(TemporaryPasswordDialog.TemporaryPassword)] = temporaryPassword,
                [nameof(TemporaryPasswordDialog.Message)] = message
            },
            new UiDialogOptions { Size = UiDialogSize.Small, CloseOnBackdrop = false, CloseOnEscape = false });
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

    private async Task PreviousPageAsync()
    {
        if (_pageNumber <= 1) return;
        _pageNumber--;
        await LoadPageAsync();
    }

    private async Task NextPageAsync()
    {
        if (!_page.HasNextPage) return;
        _pageNumber++;
        await LoadPageAsync();
    }

    private string FormatRoles(IReadOnlyList<string> roles) => roles.Count == 0 ? "—" : string.Join("، ", roles.Select(TranslateRole));
    private string TranslateRole(string role) => role switch { "Administrator" => L["Role_Administrator"], "User" => L["Role_User"], _ => role };
    private static string FormatDate(DateTimeOffset? value) => value?.ToLocalTime().ToString("g") ?? "—";

    private string? GetActiveUserSubtitle()
    {
        var tab = Workspace.ActiveUserTab;
        if (tab is null) return null;
        if (tab.IsNew) return L["Users_NewAccountSubtitle"].Value;
        return tab.Editor.Details?.Email ?? tab.Editor.Details?.UserName;
    }

    private string? GetActiveUserStatusText()
    {
        var details = Workspace.ActiveUserTab?.Editor.Details;
        if (details is null) return null;
        return details.IsActive ? L["Users_Active"].Value : L["Users_Inactive"].Value;
    }

    private AlertTone GetActiveUserStatusTone() =>
        Workspace.ActiveUserTab?.Editor.Details?.IsActive == true ? AlertTone.Success : AlertTone.Danger;

    private static string GetInitials(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(parts.Take(2).Select(x => x[0])).ToUpperInvariant();
    }
}
