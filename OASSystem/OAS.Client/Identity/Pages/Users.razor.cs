using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Identity.Services;
using OAS.Client.Identity.State;
using OAS.Client.Identity.Users.Components;
using OAS.Client.Identity.Users.Workspace;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Roles;
using OAS.Contracts.Identity.Users;
using OAS.UiLib.Components.Dialogs;
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

    private UserEditor? _editorComponent;
    private PagedResult<UserSummaryDto> _page = new() { PageNumber = 1, PageSize = 20 };
    private IReadOnlyList<RoleDto> _roles = [];
    private bool _loading = true;
    private bool _saving;
    private bool _exporting;
    private long _photoRevision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private IReadOnlyList<UiSelectOption> StatusFilterOptions =>
    [
        new("all", L["Users_AllStatuses"]),
        new("active", L["Users_Active"]),
        new("inactive", L["Users_Inactive"])
    ];

    private IReadOnlyList<UiSelectOption> RoleFilterOptions =>
        _roles.Select(role => new UiSelectOption(role.Id.ToString("D"), TranslateRole(role.Name))).ToArray();

    private bool? SelectedStatusFilter => Workspace.StatusFilter switch
    {
        "active" => true,
        "inactive" => false,
        _ => null
    };

    private Guid? SelectedRoleFilter => Guid.TryParse(Workspace.RoleFilter, out var roleId) ? roleId : null;
    private string EditorTitle => Workspace.Mode == UserEditorMode.Create
        ? L["Users_New"]
        : Workspace.SelectedTitle ?? Workspace.Editor.Details?.DisplayName ?? L["Users_Details"];
    private string? SelectedImageUrl => Workspace.Editor.PendingPhoto?.PreviewUrl
        ?? (Workspace.Editor.PhotoRemoved || !Workspace.SelectedUserId.HasValue ? null : GetUserImageUrl(Workspace.SelectedUserId.Value));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _roles = await UserService.GetRolesAsync();
            await LoadPageAsync();
            if (Workspace.SelectedUserId.HasValue && Workspace.Editor.Details is null)
                await ReloadSelectedAsync(false);
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
                    PageNumber = Workspace.PageNumber,
                    PageSize = 20,
                    Search = Workspace.Search,
                    SortBy = "UserName",
                    SortDirection = SortDirection.Ascending
                },
                SelectedStatusFilter,
                SelectedRoleFilter);
            Workspace.PageNumber = _page.PageNumber <= 0 ? 1 : _page.PageNumber;
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

    private async Task SearchChangedAsync(string? _)
    {
        Workspace.PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task StatusFilterChangedAsync(string? value)
    {
        Workspace.StatusFilter = string.IsNullOrWhiteSpace(value) ? "all" : value;
        Workspace.PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task RoleFilterChangedAsync(string? value)
    {
        Workspace.RoleFilter = value;
        Workspace.PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task RefreshAsync(MouseEventArgs _)
    {
        await LoadPageAsync();
        if (Workspace.SelectedUserId.HasValue && Workspace.Mode == UserEditorMode.View)
            await ReloadSelectedAsync(false);
    }

    private async Task ResetFiltersAsync(MouseEventArgs _)
    {
        Workspace.Search = null;
        Workspace.StatusFilter = "all";
        Workspace.RoleFilter = null;
        Workspace.PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task ExportUsersAsync(MouseEventArgs _)
    {
        if (_exporting) return;
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
                        Search = Workspace.Search,
                        SortBy = "UserName",
                        SortDirection = SortDirection.Ascending
                    },
                    SelectedStatusFilter,
                    SelectedRoleFilter);
                allUsers.AddRange(page.Items);
                if (!page.HasNextPage) break;
                exportPage++;
            }

            var csv = BuildUsersCsv(allUsers);
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            await BrowserFileDownload.DownloadAsync(bytes, $"OAS-Users-{DateTime.Now:yyyyMMdd-HHmm}.csv", "text/csv;charset=utf-8");
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
        static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
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
                   .Append(Csv(user.LastLoginAtUtc?.ToString("O"))).AppendLine();
        }
        return builder.ToString();
    }

    private async Task ShowRolesOverviewAsync(MouseEventArgs _)
    {
        await Dialogs.ShowAsync<RolesOverviewDialog>(
            L["Users_RolesPermissions"],
            new Dictionary<string, object> { [nameof(RolesOverviewDialog.Roles)] = _roles },
            new UiDialogOptions { Size = UiDialogSize.Small });
    }

    private async Task OpenNewAsync(MouseEventArgs _)
    {
        if (!await ConfirmDiscardIfDirtyAsync()) return;
        var defaultRole = _roles.FirstOrDefault(x => string.Equals(x.Name, "User", StringComparison.OrdinalIgnoreCase));
        Workspace.BeginCreate(defaultRole is null ? null : [defaultRole.Id]);
        await FocusUserNameAsync();
    }

    private async Task SelectUserAsync(UserSummaryDto summary)
    {
        if (Workspace.SelectedUserId == summary.Id && Workspace.Mode != UserEditorMode.Create) return;
        if (!await ConfirmDiscardIfDirtyAsync()) return;

        Workspace.SelectExisting(summary.Id, string.IsNullOrWhiteSpace(summary.DisplayName) ? summary.UserName : summary.DisplayName);
        try
        {
            var details = await UserService.GetUserByIdAsync(summary.Id);
            Workspace.LoadDetails(details);
        }
        catch (ApiClientException ex)
        {
            Workspace.ClearSelection();
            ApiFeedback.Show(ex.Error);
        }
        finally
        {
            Workspace.IsLoading = false;
        }
    }

    private async Task BeginEditAsync(MouseEventArgs _)
    {
        if (!Workspace.SelectedUserId.HasValue || Workspace.Editor.Details is null) return;
        Workspace.BeginEdit();
        await FocusUserNameAsync();
    }

    private async Task FocusUserNameAsync()
    {
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        if (_editorComponent is not null) await _editorComponent.FocusUserNameAsync();
    }

    private async Task<bool> ConfirmDiscardIfDirtyAsync()
    {
        if (!Workspace.IsDirty) return true;
        return await Dialogs.ConfirmAsync(
            L["Users_UnsavedTitle"],
            L["Users_CloseDirtyConfirm"],
            AlertTone.Warning,
            L["Users_Discard"],
            L["Common_Cancel"]);
    }

    private async Task SaveAsync(MouseEventArgs _) => await SaveCurrentAsync(false);
    private async Task SaveAndCloseAsync(MouseEventArgs _) => await SaveCurrentAsync(true);

    private async Task SaveCurrentAsync(bool clearAfterSave)
    {
        if (Workspace.Mode is not (UserEditorMode.Create or UserEditorMode.Edit) || _saving) return;
        if (!ValidateEditor(Workspace.Editor)) return;

        _saving = true;
        try
        {
            if (Workspace.Mode == UserEditorMode.Create)
            {
                await CreateUserAsync(clearAfterSave);
                return;
            }

            await UpdateUserAsync(clearAfterSave);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task CreateUserAsync(bool clearAfterSave)
    {
        var editor = Workspace.Editor;
        var pendingPhoto = editor.PendingPhoto;
        var create = new CreateUserRequest(
            editor.Form.UserName.Trim(), editor.Form.FirstName.Trim(), editor.Form.LastName.Trim(),
            string.IsNullOrWhiteSpace(editor.Form.Email) ? null : editor.Form.Email.Trim(),
            editor.RoleIds.ToArray(), editor.Form.IsActive);

        var result = await UserService.CreateUserAsync(create);
        if (!TryGet(result, out var created)) return;

        Workspace.LoadDetails(created.User);
        string? photoError = null;
        if (pendingPhoto is not null)
        {
            var upload = await UploadUserPhotoAsync(created.User.Id, pendingPhoto);
            if (!upload.Succeeded) photoError = GetErrorMessage(upload.Error);
            else _photoRevision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        await LoadPageAsync();
        await ShowTemporaryPasswordAsync(
            created.TemporaryPassword,
            L["Users_TemporaryPasswordCreatedMessage"],
            photoError is null ? UiOperationStatus.Success : UiOperationStatus.Warning,
            photoError);

        if (photoError is not null && pendingPhoto is not null)
        {
            Workspace.BeginEdit();
            Workspace.Editor.SetPhoto(pendingPhoto);
        }
        else if (clearAfterSave)
        {
            Workspace.ClearSelection();
        }
    }

    private async Task UpdateUserAsync(bool clearAfterSave)
    {
        var editor = Workspace.Editor;
        if (Workspace.SelectedUserId is not Guid userId || string.IsNullOrWhiteSpace(editor.RowVersion)) return;

        if (editor.BasicDirty)
        {
            var update = new UpdateUserRequest(
                editor.Form.UserName.Trim(), editor.Form.FirstName.Trim(), editor.Form.LastName.Trim(),
                string.IsNullOrWhiteSpace(editor.Form.Email) ? null : editor.Form.Email.Trim(), editor.RowVersion);
            var result = await UserService.UpdateUserAsync(userId, update);
            if (!TryGet(result, out var updated)) return;
            editor.AcceptBasicSave(updated);
        }

        if (editor.RolesDirty)
        {
            var result = await UserService.SetUserRolesAsync(userId, new SetUserRolesRequest(editor.RoleIds.ToArray(), editor.RowVersion!));
            if (!TryGet(result, out var rolesUpdated)) return;
            editor.AcceptRolesSave(rolesUpdated);
        }

        if (editor.PhotoDirty)
        {
            ApiCallResult<bool> photoResult;
            if (editor.PhotoRemoved)
                photoResult = await UserService.RemoveProfileImageAsync(userId);
            else if (editor.PendingPhoto is { } pending)
                photoResult = await UploadUserPhotoAsync(userId, pending);
            else
                photoResult = ApiCallResult<bool>.Success(true);

            if (!photoResult.Succeeded)
            {
                await ShowOperationAsync(UiOperationStatus.Failure, L["Users_UpdateSuccess"], null, GetErrorMessage(photoResult.Error));
                return;
            }
            editor.MarkPhotoSaved();
            _photoRevision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        Workspace.ReturnToView();
        Snackbar.Success(L["Users_UpdateSuccess"]);
        await LoadPageAsync();
        if (clearAfterSave) Workspace.ClearSelection();
    }

    private async Task<ApiCallResult<bool>> UploadUserPhotoAsync(Guid userId, PendingUserPhoto photo)
    {
        await using var stream = new MemoryStream(photo.Content, writable: false);
        return await UserService.UploadProfileImageAsync(userId, stream, photo.FileName, photo.ContentType);
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
        if (!Workspace.IsDirty) return;
        var confirmed = await Dialogs.ConfirmAsync(L["Users_UnsavedTitle"], L["Users_RevertConfirm"], AlertTone.Warning, L["Users_Revert"], L["Common_Cancel"]);
        if (!confirmed) return;
        Workspace.Revert();
        if (Workspace.Mode == UserEditorMode.Create) await FocusUserNameAsync();
    }

    private async Task ReloadSelectedAsync(bool confirmDirty = true)
    {
        if (Workspace.SelectedUserId is not Guid userId) return;
        if (confirmDirty && !await ConfirmDiscardIfDirtyAsync()) return;
        try
        {
            Workspace.IsLoading = true;
            var details = await UserService.GetUserByIdAsync(userId);
            Workspace.LoadDetails(details);
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
        }
        finally
        {
            Workspace.IsLoading = false;
        }
    }

    private async Task ToggleStatusAsync(MouseEventArgs _)
    {
        if (Workspace.SelectedUserId is not Guid userId || Workspace.Editor.Details is not { } details || string.IsNullOrWhiteSpace(Workspace.Editor.RowVersion)) return;
        var desired = !details.IsActive;
        var confirmed = await Dialogs.ConfirmAsync(
            desired ? L["Users_Activate"] : L["Users_Deactivate"],
            desired ? L["Users_ActivateConfirm"] : L["Users_DeactivateConfirm"],
            desired ? AlertTone.Info : AlertTone.Warning,
            desired ? L["Users_Activate"] : L["Users_Deactivate"], L["Common_Cancel"]);
        if (!confirmed) return;

        var result = await UserService.SetUserStatusAsync(userId, new SetUserStatusRequest(desired, Workspace.Editor.RowVersion));
        if (!TryGet(result, out var updated)) return;
        Workspace.Editor.ApplyServerState(updated);
        Workspace.LoadDetails(updated);
        Snackbar.Success(L["Users_StatusUpdated"]);
        await LoadPageAsync();
    }

    private async Task UnlockAsync(MouseEventArgs _)
    {
        if (Workspace.SelectedUserId is not Guid userId || string.IsNullOrWhiteSpace(Workspace.Editor.RowVersion)) return;
        var result = await UserService.UnlockUserAsync(userId, new UnlockUserRequest(Workspace.Editor.RowVersion));
        if (!TryGet(result, out var updated)) return;
        Workspace.LoadDetails(updated);
        Snackbar.Success(L["Users_UnlockedSuccess"]);
        await LoadPageAsync();
    }

    private async Task ResetPasswordAsync(MouseEventArgs _)
    {
        if (Workspace.SelectedUserId is not Guid userId || string.IsNullOrWhiteSpace(Workspace.Editor.RowVersion)) return;
        var confirmed = await Dialogs.ConfirmAsync(
            L["Users_ResetPassword"], L["Users_ResetPasswordConfirm"], AlertTone.Warning,
            L["Users_ResetPassword"], L["Common_Cancel"]);
        if (!confirmed) return;

        var result = await UserService.ResetUserPasswordAsync(userId, new ResetUserPasswordRequest(Workspace.Editor.RowVersion));
        if (!result.Succeeded || result.Value is null)
        {
            await ShowOperationAsync(UiOperationStatus.Failure, L["Users_ResetPassword"], null, GetErrorMessage(result.Error));
            return;
        }

        Workspace.Editor.ApplyPasswordReset(result.Value.RowVersion);
        await ShowTemporaryPasswordAsync(result.Value.TemporaryPassword, L["Users_TemporaryPasswordResetMessage"], UiOperationStatus.Success, null);

        if (AuthState.CurrentUser?.Id == userId)
        {
            try { await AuthService.LogoutAsync(); } catch { }
            AuthState.SetAnonymous();
            Navigation.NavigateTo("/login", replace: true);
            return;
        }

        await LoadPageAsync();
    }

    private async Task ShowTemporaryPasswordAsync(string temporaryPassword, string message, UiOperationStatus status, string? reason)
    {
        await Dialogs.ShowAsync<UiOperationResultDialog>(
            null,
            new Dictionary<string, object>
            {
                [nameof(UiOperationResultDialog.Status)] = status,
                [nameof(UiOperationResultDialog.Title)] = L["Users_TemporaryPasswordTitle"].Value,
                [nameof(UiOperationResultDialog.Message)] = message,
                [nameof(UiOperationResultDialog.Reason)] = reason ?? string.Empty,
                [nameof(UiOperationResultDialog.SecretValue)] = temporaryPassword,
                [nameof(UiOperationResultDialog.SecretHint)] = L["Users_TemporaryPasswordOneTime"].Value,
                [nameof(UiOperationResultDialog.CopyText)] = L["Users_CopyPassword"].Value,
                [nameof(UiOperationResultDialog.CloseText)] = L["Common_Close"].Value
            },
            new UiDialogOptions { Size = UiDialogSize.Small, ShowCloseButton = false, CloseOnBackdrop = false, CloseOnEscape = false });
    }

    private async Task ShowOperationAsync(UiOperationStatus status, string title, string? message, string? reason)
    {
        await Dialogs.ShowAsync<UiOperationResultDialog>(
            null,
            new Dictionary<string, object>
            {
                [nameof(UiOperationResultDialog.Status)] = status,
                [nameof(UiOperationResultDialog.Title)] = title,
                [nameof(UiOperationResultDialog.Message)] = message ?? string.Empty,
                [nameof(UiOperationResultDialog.Reason)] = reason ?? string.Empty,
                [nameof(UiOperationResultDialog.CloseText)] = L["Common_Close"].Value
            },
            new UiDialogOptions { Size = UiDialogSize.Small, ShowCloseButton = false });
    }

    private async Task UserImageSelectedAsync(IBrowserFile file)
    {
        if (Workspace.Mode is not (UserEditorMode.Create or UserEditorMode.Edit)) return;
        if (file.Size <= 0 || file.Size > 2_500_000)
        {
            await ShowOperationAsync(UiOperationStatus.Warning, "الصورة الشخصية", null, "حجم الصورة يجب ألا يتجاوز 2.5 MB.");
            return;
        }

        var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            await ShowOperationAsync(UiOperationStatus.Warning, "الصورة الشخصية", null, "الصيغ المسموح بها هي JPEG وPNG وWebP فقط.");
            return;
        }
        try
        {
            await using var stream = file.OpenReadStream(2_500_000);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            Workspace.Editor.SetPhoto(new PendingUserPhoto(file.Name, file.ContentType, memory.ToArray()));
        }
        catch
        {
            await ShowOperationAsync(UiOperationStatus.Failure, "الصورة الشخصية", null, "تعذر قراءة ملف الصورة.");
        }
    }

    private Task UserImageRemovedAsync()
    {
        if (Workspace.Mode == UserEditorMode.Create)
            Workspace.Editor.ClearPendingPhoto();
        else if (Workspace.Mode == UserEditorMode.Edit)
            Workspace.Editor.RemovePhoto();
        return Task.CompletedTask;
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
        if (Workspace.PageNumber <= 1) return;
        Workspace.PageNumber--;
        await LoadPageAsync();
    }

    private async Task NextPageAsync()
    {
        if (!_page.HasNextPage) return;
        Workspace.PageNumber++;
        await LoadPageAsync();
    }

    private string GetUserImageUrl(Guid userId) => $"api/identity/profile-images/{userId:D}?v={_photoRevision}";
    private string FormatRoles(IReadOnlyList<string> roles) => roles.Count == 0 ? "—" : string.Join("، ", roles.Select(TranslateRole));
    private string TranslateRole(string role) => role switch { "Administrator" => L["Role_Administrator"], "User" => L["Role_User"], _ => role };
    private static string FormatDate(DateTimeOffset? value) => value?.ToLocalTime().ToString("g") ?? "—";
    private string? GetEditorSubtitle() => Workspace.Mode == UserEditorMode.Create ? L["Users_NewAccountSubtitle"] : Workspace.Editor.Details?.Email ?? Workspace.Editor.Details?.UserName;
    private string? GetEditorStatusText() => Workspace.Editor.Details is { } details ? (details.IsActive ? L["Users_Active"] : L["Users_Inactive"]) : null;
    private AlertTone GetEditorStatusTone() => Workspace.Editor.Details?.IsActive == true ? AlertTone.Success : AlertTone.Danger;
    private static string GetInitials(string? displayName)
    {
        var parts = (displayName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(parts.Take(2).Select(x => x[..1])).ToUpperInvariant();
    }
    private static string GetErrorMessage(OAS.Contracts.Common.Errors.ApiError? error) =>
        !string.IsNullOrWhiteSpace(error?.Message) ? error.Message : error?.Code ?? "حدث خطأ غير متوقع.";
}
