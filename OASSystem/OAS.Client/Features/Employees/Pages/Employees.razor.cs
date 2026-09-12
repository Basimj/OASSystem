using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Features.Employees.Workspace;
using OAS.Client.Identity.Services;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Contracts.Features.Employees.JobTitles;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Pages;

public partial class Employees
{
    internal const int PageSize = 12;
    private const long MaximumEmployeeImageBytes = 2_500_000;

    [Inject] private IEmployeeClientService EmployeeService { get; set; } = default!;
    [Inject] private IUserClientService UserService { get; set; } = default!;
    [Inject] private IEmployeesWorkspaceState Workspace { get; set; } = default!;
    [Inject] private IUiDialogService Dialog { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private BrowserFileDownloadService BrowserFileDownload { get; set; } = default!;

    private bool _isLoading;
    private bool _importMenuOpen;
    private bool _importDialogOpen;
    private bool _employeeEntryOpen;
    private bool _exportDialogOpen;
    private IReadOnlyList<JobTitleDto> _jobTitles = [];

    private PagedResult<EmployeeDto> _page
    {
        get => Workspace.Page;
        set => Workspace.Page = value;
    }

    private EmployeeWorkspaceTabState? ActiveEditorTab => Workspace.ActiveEditorTab;
    private IReadOnlyList<EmployeeDto> VisibleEmployees => GetVisibleEmployees().ToArray();

    private IReadOnlyList<UiApplicationTabItem> WorkspaceTabs
    {
        get
        {
            var tabs = new List<UiApplicationTabItem>
            {
                new(Workspace.AllEmployeesTabId, "جميع الموظفين", "/employees", "fa-solid fa-list",
                    Workspace.ActiveTabId == Workspace.AllEmployeesTabId, CanClose: false)
            };

            tabs.AddRange(Workspace.EditorTabs.Select(tab => new UiApplicationTabItem(
                tab.TabId, tab.Title, "/employees", BuildEditorTabIconCss(tab),
                Workspace.ActiveTabId == tab.TabId, CanClose: !tab.IsSaving)));
            return tabs;
        }
    }

    private static string BuildEditorTabIconCss(EmployeeWorkspaceTabState tab)
    {
        var baseCss = tab.IsNew ? "fa-solid fa-user-plus" : "fa-regular fa-user";
        return tab.IsDirty ? $"{baseCss} ui-employee-tab-icon--dirty" : baseCss;
    }

    private bool CanEditActive => ActiveEditorTab is { IsNew: false, IsEditMode: false, IsLoading: false, IsSaving: false };
    private bool CanSaveActive => ActiveEditorTab is { IsInitialized: true, IsSaving: false } tab && (tab.IsNew || tab.IsEditMode);
    private bool CanCloseActive => ActiveEditorTab is { IsSaving: false };
    private bool CanCancelActive => ActiveEditorTab is { IsSaving: false } tab && (tab.IsNew || tab.IsEditMode);
    private bool CanRefreshActive => ActiveEditorTab is null ? !_isLoading : ActiveEditorTab is { IsNew: false, IsSaving: false, IsLoading: false, IsDirty: false };

    private static readonly IReadOnlyList<UiSelectOption> FilterOptions =
    [
        new(nameof(EmployeeListFilter.All), "جميع الموظفين"),
        new(nameof(EmployeeListFilter.Active), "الموظفون النشطون"),
        new(nameof(EmployeeListFilter.Inactive), "الموظفون غير النشطين"),
        new(nameof(EmployeeListFilter.Commission), "المستحقون للعمولة")
    ];

    private IReadOnlyList<UiSelectOption> ActiveJobTitleOptions
    {
        get
        {
            var currentId = ActiveEditorTab?.Form.JobTitleId;
            return _jobTitles
                .Where(x => x.IsActive || x.Id == currentId)
                .OrderBy(x => x.Name)
                .Select(x => new UiSelectOption(x.Id.ToString("D"), x.Name))
                .ToArray();
        }
    }

    private UiLookupItem? LinkedUserItem
    {
        get
        {
            var tab = ActiveEditorTab;
            if (tab?.Form.UserAccountId is not Guid id || string.IsNullOrWhiteSpace(tab.UserAccountDisplayName))
                return null;

            var secondary = string.Join(" • ", new[] { tab.UserAccountUserName, tab.UserAccountEmail }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return new UiLookupItem(id.ToString("D"), tab.UserAccountDisplayName!, secondary, "fa-regular fa-user");
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadJobTitlesAsync();

        if (!Workspace.HasLoadedPage)
            await LoadEmployeesAsync();

        if (Workspace.ActiveEditorTab is { } tab)
            await EnsureTabInitializedAsync(tab);
    }

    private async Task LoadJobTitlesAsync()
    {
        try
        {
            _jobTitles = await EmployeeService.GetJobTitlesAsync();
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
            _jobTitles = [];
        }
    }

    private async Task LoadEmployeesAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        try
        {
            var request = new PageRequest
            {
                PageNumber = Workspace.PageNumber,
                PageSize = PageSize,
                Search = Workspace.AppliedSearch,
                SortBy = "EmployeeCode",
                SortDirection = SortDirection.Ascending
            };

            _page = await EmployeeService.GetPageAsync(request);
            Workspace.PageNumber = _page.PageNumber;
            Workspace.HasLoadedPage = true;
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _isLoading = false; }
    }

    private async Task RefreshAsync(MouseEventArgs _)
    {
        await LoadJobTitlesAsync();
        await LoadEmployeesAsync();
    }


    private async Task SearchChangedAsync(string? value)
    {
        Workspace.AppliedSearch = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        Workspace.PageNumber = 1;
        await LoadEmployeesAsync();
    }

    private Task FilterChangedAsync(string? value)
    {
        Workspace.Filter = Enum.TryParse<EmployeeListFilter>(value, out var filter) ? filter : EmployeeListFilter.All;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task PageChangedAsync(int page)
    {
        if (page < 1 || page == Workspace.PageNumber) return;
        Workspace.PageNumber = page;
        await LoadEmployeesAsync();
    }

    private IEnumerable<EmployeeDto> GetVisibleEmployees() => Workspace.Filter switch
    {
        EmployeeListFilter.Active => _page.Items.Where(x => x.IsActive),
        EmployeeListFilter.Inactive => _page.Items.Where(x => !x.IsActive),
        EmployeeListFilter.Commission => _page.Items.Where(x => x.IsCommissionEligible),
        _ => _page.Items
    };

    private static string? GetEmployeeImageUrl(EmployeeDto employee) =>
        string.IsNullOrWhiteSpace(employee.Photo)
            ? null
            : $"api/employees/{employee.Id:D}/image?v={Uri.EscapeDataString(employee.RowVersion)}";

    private async Task OpenEmployeeAsync(Guid employeeId)
    {
        var tab = Workspace.GetOrCreateEmployeeTab(employeeId);
        Workspace.ActiveTabId = tab.TabId;
        await EnsureTabInitializedAsync(tab);
    }

    private async Task OpenNewAsync(MouseEventArgs _)
    {
        try
        {
            var reservation =
                await EmployeeService.ReserveEmployeeNumberAsync();

            var tab = Workspace.CreateNewTab(reservation.EmployeeCode);
            Workspace.ActiveTabId = tab.TabId;
            await InvokeAsync(StateHasChanged);
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
    }

    private async Task EnsureTabInitializedAsync(EmployeeWorkspaceTabState tab)
    {
        if (tab.IsInitialized || tab.IsLoading) return;
        tab.IsLoading = true;
        try
        {
            if (tab.IsNew)
            {
                var reservation = await EmployeeService.ReserveEmployeeNumberAsync();
                tab.InitializeNew(reservation.EmployeeCode);
            }
            else if (tab.EmployeeId is Guid employeeId)
                tab.Load(await EmployeeService.GetByIdAsync(employeeId));
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
            Workspace.RemoveEditorTab(tab.TabId);
            Workspace.ActiveTabId = Workspace.AllEmployeesTabId;
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
            Workspace.RemoveEditorTab(tab.TabId);
            Workspace.ActiveTabId = Workspace.AllEmployeesTabId;
        }
        finally
        {
            tab.IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchUsersAsync(string search, CancellationToken cancellationToken)
    {
        var request = new PageRequest
        {
            PageNumber = 1,
            PageSize = 8,
            Search = string.IsNullOrWhiteSpace(search) ? null : search,
            SortBy = "FirstName",
            SortDirection = SortDirection.Ascending
        };

        var page = await UserService.GetUsersAsync(request, isActive: true, cancellationToken: cancellationToken);
        return page.Items
            .Where(x => !x.IsSuperAdmin)
            .Select(x => new UiLookupItem(
                x.Id.ToString("D"),
                x.DisplayName,
                string.Join(" • ", new[] { x.UserName, x.Email }.Where(v => !string.IsNullOrWhiteSpace(v))),
                "fa-regular fa-user"))
            .ToArray();
    }

    private async Task SelectWorkspaceTabAsync(Guid tabId)
    {
        Workspace.ActiveTabId = tabId;
        if (Workspace.FindEditorTab(tabId) is { } tab)
            await EnsureTabInitializedAsync(tab);
    }

    private async Task CloseWorkspaceTabAsync(Guid tabId)
    {
        if (tabId == Workspace.AllEmployeesTabId) return;
        var tab = Workspace.FindEditorTab(tabId);
        if (tab is null || !await ConfirmDiscardAsync([tab])) return;
        Workspace.RemoveEditorTab(tabId);
        await InvokeAsync(StateHasChanged);
    }

    private async Task CloseOtherWorkspaceTabsAsync(Guid tabId)
    {
        var keepEditor = Workspace.FindEditorTab(tabId);
        var toClose = Workspace.EditorTabs.Where(x => keepEditor is null || x.TabId != keepEditor.TabId).ToArray();
        if (!await ConfirmDiscardAsync(toClose)) return;
        foreach (var tab in toClose) Workspace.RemoveEditorTab(tab.TabId);
        Workspace.ActiveTabId = keepEditor?.TabId ?? Workspace.AllEmployeesTabId;
        await InvokeAsync(StateHasChanged);
    }

    private async Task CloseAllWorkspaceTabsAsync()
    {
        var toClose = Workspace.EditorTabs.ToArray();
        if (!await ConfirmDiscardAsync(toClose)) return;
        foreach (var tab in toClose) Workspace.RemoveEditorTab(tab.TabId);
        Workspace.ActiveTabId = Workspace.AllEmployeesTabId;
        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> ConfirmDiscardAsync(IEnumerable<EmployeeWorkspaceTabState> tabs)
    {
        var candidates = tabs.ToArray();
        if (candidates.Any(x => x.IsSaving))
        {
            Snackbar.Info("انتظر حتى تنتهي عملية الحفظ قبل إغلاق التبويبة.");
            return false;
        }

        var dirty = candidates.Where(x => x.IsDirty).ToArray();
        if (dirty.Length == 0) return true;

        var description = dirty.Length == 1
            ? $"توجد تغييرات غير محفوظة في تبويبة «{dirty[0].Title}». هل تريد تجاهلها وإغلاق التبويبة؟"
            : $"توجد تغييرات غير محفوظة في {dirty.Length} تبويبات. هل تريد تجاهلها وإغلاق التبويبات؟";

        return await Dialog.ConfirmAsync("تغييرات غير محفوظة", description, AlertTone.Warning, "تجاهل وإغلاق", "إلغاء");
    }

    private Task EditorChangedAsync()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task EmployeeImageSelectedAsync(IBrowserFile file)
    {
        var tab = ActiveEditorTab;
        if (tab is null || !(tab.IsNew || tab.IsEditMode)) return;

        if (file.Size <= 0 || file.Size > MaximumEmployeeImageBytes)
        {
            Snackbar.Error("حجم صورة الموظف يجب ألا يتجاوز 2.5 MB.");
            return;
        }

        var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            Snackbar.Error("الصيغ المسموح بها لصورة الموظف هي JPEG وPNG وWebP فقط.");
            return;
        }

        try
        {
            await using var source = file.OpenReadStream(MaximumEmployeeImageBytes);
            await using var memory = new MemoryStream((int)file.Size);
            await source.CopyToAsync(memory);
            var content = memory.ToArray();
            tab.StageImage(content, contentType, file.Name, $"data:{contentType};base64,{Convert.ToBase64String(content)}");
            await InvokeAsync(StateHasChanged);
        }
        catch { Snackbar.Error("تعذر قراءة ملف صورة الموظف."); }
    }

    private Task EmployeeImageRemovedAsync()
    {
        var tab = ActiveEditorTab;
        if (tab is null || !(tab.IsNew || tab.IsEditMode)) return Task.CompletedTask;

        if (tab.IsNew) tab.ClearImageDraft(); else tab.StageImageRemoval();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task BeginEditActiveAsync(MouseEventArgs _)
    {
        ActiveEditorTab?.BeginEdit();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task CloseActiveAsync(MouseEventArgs _)
    {
        if (ActiveEditorTab is not { } tab) return;
        await CloseWorkspaceTabAsync(tab.TabId);
    }

    private async Task RefreshActionAsync(MouseEventArgs args)
    {
        if (ActiveEditorTab is { } tab)
            await ReloadTabAsync(tab);
        else
            await RefreshAsync(args);
    }

    private async Task CancelActiveEditAsync(MouseEventArgs _)
    {
        var tab = ActiveEditorTab;
        if (tab is null) return;

        if (tab.IsNew)
        {
            if (!await ConfirmDiscardAsync([tab])) return;
            Workspace.RemoveEditorTab(tab.TabId);
            Workspace.ActiveTabId = Workspace.AllEmployeesTabId;
        }
        else
        {
            if (tab.IsDirty)
            {
                var confirmed = await Dialog.ConfirmAsync(
                    "إلغاء التعديلات",
                    "سيتم تجاهل جميع التعديلات غير المحفوظة وإعادة البيانات إلى آخر حالة محفوظة.",
                    AlertTone.Warning,
                    "تجاهل التعديلات",
                    "رجوع");
                if (!confirmed) return;
            }
            tab.Revert();
        }

        await InvokeAsync(StateHasChanged);
    }

    private Task SaveActiveAsync(MouseEventArgs _) => SaveActiveCoreAsync(false);
    private Task SaveAndCloseActiveAsync(MouseEventArgs _) => SaveActiveCoreAsync(true);

    private async Task SaveActiveCoreAsync(bool closeAfterSave)
    {
        var tab = ActiveEditorTab;
        if (tab is null || tab.IsSaving || !tab.IsInitialized) return;
        if (!ValidateTab(tab)) return;

        tab.IsSaving = true;
        try
        {
            var wasNew = tab.IsNew;
            var previousUserAccountId = tab.IsUserAccountLinkLocked ? tab.Form.UserAccountId : null;
            var hadImageChanges = tab.HasImageChanges;

            EmployeeDto saved;
            if (wasNew)
            {
                var result = await EmployeeService.CreateAsync(BuildCreateRequest(tab));
                if (!result.Succeeded || result.Value is null)
                {
                    if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected();
                    return;
                }
                saved = result.Value;
            }
            else
            {
                if (tab.EmployeeId is not Guid employeeId || string.IsNullOrWhiteSpace(tab.RowVersion))
                {
                    Snackbar.Error("تعذر الحصول على بيانات التزامن الحالية للموظف.");
                    return;
                }

                try
                {
                    saved = await EmployeeService.UpdateAsync(employeeId, BuildUpdateRequest(tab, tab.RowVersion));
                }
                catch (ApiClientException ex) when (ex.StatusCode == 409)
                {
                    ApiFeedback.Show(ex.Error);
                    await ReloadTabAsync(tab, force: true);
                    return;
                }
            }

            var newlyLinked = previousUserAccountId is null && saved.UserAccountId is not null;
            tab.Load(saved, preserveImageChanges: hadImageChanges);

            if (hadImageChanges)
            {
                if (!await SavePendingImageAsync(tab))
                {
                    Snackbar.Info("تم حفظ بيانات الموظف، لكن تعذر حفظ تغيير الصورة. أعد المحاولة.");
                    return;
                }

                if (tab.EmployeeId is Guid mediaEmployeeId)
                    tab.CompleteSave(await EmployeeService.GetByIdAsync(mediaEmployeeId));
            }
            else
            {
                tab.CompleteSave(saved);
            }

            Workspace.HasLoadedPage = false;
            await LoadEmployeesAsync();
            Snackbar.Success(wasNew ? "تم إنشاء الموظف بنجاح." : "تم حفظ بيانات الموظف بنجاح.");
            if (newlyLinked)
                Snackbar.Info("تم ربط الموظف بحساب المستخدم. لا يمكن تغيير الحساب المرتبط لاحقًا، ويمكن تعديل بقية بيانات الموظف بشكل طبيعي.");

            if (closeAfterSave)
            {
                Workspace.RemoveEditorTab(tab.TabId);
                Workspace.ActiveTabId = Workspace.AllEmployeesTabId;
            }
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally
        {
            tab.IsSaving = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private static CreateEmployeeRequest BuildCreateRequest(EmployeeWorkspaceTabState tab) => new(
        tab.Form.FirstName.Trim(),
        tab.Form.LastName.Trim(),
        NullIfEmpty(tab.Form.Phone),
        NullIfEmpty(tab.Form.Email),
        NullIfEmpty(tab.Form.Country),
        NullIfEmpty(tab.Form.Governorate),
        NullIfEmpty(tab.Form.City),
        NullIfEmpty(tab.Form.PostalCode),
        NullIfEmpty(tab.Form.ResidentialAddress),
        tab.Form.JobTitleId!.Value,
        tab.Form.HireDate,
        tab.Form.IsCommissionEligible,
        tab.Form.IsActive,
        tab.Form.UserAccountId,
        tab.Form.EmployeeCode);

    private static UpdateEmployeeRequest BuildUpdateRequest(EmployeeWorkspaceTabState tab, string rowVersion) => new(
        tab.Form.FirstName.Trim(),
        tab.Form.LastName.Trim(),
        NullIfEmpty(tab.Form.Phone),
        NullIfEmpty(tab.Form.Email),
        NullIfEmpty(tab.Form.Country),
        NullIfEmpty(tab.Form.Governorate),
        NullIfEmpty(tab.Form.City),
        NullIfEmpty(tab.Form.PostalCode),
        NullIfEmpty(tab.Form.ResidentialAddress),
        tab.Form.JobTitleId!.Value,
        tab.Form.HireDate,
        tab.Form.IsCommissionEligible,
        tab.Form.IsActive,
        tab.Form.UserAccountId,
        rowVersion);

    private async Task<bool> SavePendingImageAsync(EmployeeWorkspaceTabState tab)
    {
        if (!tab.HasImageChanges || tab.EmployeeId is not Guid employeeId) return !tab.HasImageChanges;

        ApiCallResult<bool> result;
        var hasImageAfterSave = false;

        if (tab.PendingImageContent is { Length: > 0 } content &&
            !string.IsNullOrWhiteSpace(tab.PendingImageContentType) &&
            !string.IsNullOrWhiteSpace(tab.PendingImageFileName))
        {
            result = await EmployeeService.UploadImageAsync(
                employeeId, content, tab.PendingImageFileName, tab.PendingImageContentType);
            hasImageAfterSave = true;
        }
        else if (tab.IsImageRemovalPending)
        {
            result = await EmployeeService.RemoveImageAsync(employeeId);
        }
        else return true;

        if (!result.Succeeded)
        {
            if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected();
            return false;
        }

        tab.AcceptImageSave(hasImageAfterSave);
        return true;
    }

    private bool ValidateTab(EmployeeWorkspaceTabState tab)
    {
        if (string.IsNullOrWhiteSpace(tab.Form.FirstName))
        {
            Snackbar.Error("الاسم الأول مطلوب.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(tab.Form.LastName))
        {
            Snackbar.Error("اسم العائلة مطلوب.");
            return false;
        }
        if (tab.Form.FirstName.Trim().Length > 100 || tab.Form.LastName.Trim().Length > 100)
        {
            Snackbar.Error("الاسم يجب ألا يتجاوز 100 حرف.");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(tab.Form.Phone) && tab.Form.Phone.Trim().Length > 32)
        {
            Snackbar.Error("رقم الهاتف يجب ألا يتجاوز 32 حرفًا.");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(tab.Form.Email))
        {
            var email = tab.Form.Email.Trim();
            if (email.Length > 256 || !email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
            {
                Snackbar.Error("البريد الإلكتروني غير صالح.");
                return false;
            }
        }
        if ((tab.Form.Country?.Trim().Length ?? 0) > 100 ||
            (tab.Form.Governorate?.Trim().Length ?? 0) > 100 ||
            (tab.Form.City?.Trim().Length ?? 0) > 100 ||
            (tab.Form.PostalCode?.Trim().Length ?? 0) > 24 ||
            (tab.Form.ResidentialAddress?.Trim().Length ?? 0) > 300)
        {
            Snackbar.Error("أحد حقول العنوان يتجاوز الطول المسموح به.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(tab.Form.EmployeeCode))
        {
            Snackbar.Error("كود الموظف مطلوب.");
            return false;
        }

        if (tab.Form.EmployeeCode.Trim().Length > 32)
        {
            Snackbar.Error("كود الموظف يجب ألا يتجاوز 32 حرفًا.");
            return false;
        }
        if (tab.Form.JobTitleId is null || tab.Form.JobTitleId == Guid.Empty)
        {
            Snackbar.Error("المسمى الوظيفي مطلوب.");
            return false;
        }
        return true;
    }

    private async Task ReloadActiveEmployeeAsync(MouseEventArgs _)
    {
        if (ActiveEditorTab is { IsNew: false } tab)
            await ReloadTabAsync(tab);
    }

    private async Task ReloadTabAsync(EmployeeWorkspaceTabState tab, bool force = false)
    {
        if (tab.EmployeeId is not Guid employeeId || tab.IsLoading) return;
        if (!force && tab.IsDirty)
        {
            var confirmed = await Dialog.ConfirmAsync(
                "تحديث بيانات الموظف",
                "سيتم تجاهل التعديلات غير المحفوظة وتحميل آخر نسخة من الخادم.",
                AlertTone.Warning,
                "تجاهل وتحديث",
                "إلغاء");
            if (!confirmed) return;
        }

        tab.IsLoading = true;
        try { tab.Load(await EmployeeService.GetByIdAsync(employeeId)); }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally
        {
            tab.IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ToggleEmployeeStatusAsync(EmployeeDto employee)
    {
        await SetEmployeeStatusAsync(employee.Id, employee.DisplayName, employee.IsActive, employee.RowVersion);
    }

    private async Task ToggleActiveEditorStatusAsync(MouseEventArgs _)
    {
        var tab = ActiveEditorTab;
        if (tab?.EmployeeId is not Guid employeeId || string.IsNullOrWhiteSpace(tab.RowVersion)) return;
        if (tab.IsDirty)
        {
            Snackbar.Info("احفظ أو ألغِ التعديلات الحالية قبل تغيير حالة الموظف.");
            return;
        }
        await SetEmployeeStatusAsync(employeeId, tab.Title, tab.Form.IsActive, tab.RowVersion);
    }

    private async Task SetEmployeeStatusAsync(Guid employeeId, string displayName, bool currentStatus, string rowVersion)
    {
        var openTab = Workspace.EditorTabs.FirstOrDefault(x => x.EmployeeId == employeeId);
        if (openTab?.IsDirty == true)
        {
            Snackbar.Info("احفظ أو ألغِ التعديلات المفتوحة لهذا الموظف قبل تغيير حالته.");
            return;
        }

        var targetStatus = !currentStatus;
        var confirmed = await Dialog.ConfirmAsync(
            targetStatus ? "تفعيل الموظف" : "تعطيل الموظف",
            targetStatus ? $"هل تريد تفعيل الموظف «{displayName}»؟" : $"هل تريد تعطيل الموظف «{displayName}»؟",
            AlertTone.Warning,
            targetStatus ? "تفعيل" : "تعطيل",
            "إلغاء");
        if (!confirmed) return;

        try
        {
            var result = await EmployeeService.SetStatusAsync(employeeId, new SetEmployeeStatusRequest(targetStatus, rowVersion));
            if (!result.Succeeded || result.Value is null)
            {
                if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected();
                return;
            }

            if (openTab is not null) openTab.Load(result.Value);
            Snackbar.Success(targetStatus ? "تم تفعيل الموظف بنجاح." : "تم تعطيل الموظف بنجاح.");
            Workspace.HasLoadedPage = false;
            await LoadEmployeesAsync();
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
    }

    private void OpenImportDialog(MouseEventArgs _)
    {
        _exportDialogOpen = false;
        _employeeEntryOpen = false;
        _importDialogOpen = false;
        _importMenuOpen = true;
    }

    private void CloseImportDialog() => _importMenuOpen = false;

    private void OpenExcelImportDialog()
    {
        _importMenuOpen = false;
        _employeeEntryOpen = false;
        _importDialogOpen = true;
    }

    private void CloseExcelImportDialog() => _importDialogOpen = false;

    private void OpenEmployeeEntry()
    {
        _importMenuOpen = false;
        _importDialogOpen = false;
        _exportDialogOpen = false;
        _employeeEntryOpen = true;
    }

    private void CloseEmployeeEntry() => _employeeEntryOpen = false;

    private async Task RefreshAfterImport()
    {
        _importDialogOpen = false;
        Workspace.PageNumber = 1;
        Workspace.HasLoadedPage = false;
        await LoadEmployeesAsync();
    }

    private void OpenExportDialog(MouseEventArgs _)
    {
        _importMenuOpen = false;
        _importDialogOpen = false;
        _employeeEntryOpen = false;
        _exportDialogOpen = true;
    }

    private void CloseExportDialog() => _exportDialogOpen = false;

    private async Task ExportEmployeesAsync()
    {
        _exportDialogOpen = false;
        try
        {
            Snackbar.Info("جاري تجهيز ملف Excel للموظفين...");
            var file = await EmployeeService.ExportAsync();
            var saved = await BrowserFileDownload.SaveAsync(
                file, "Employees.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            if (saved) Snackbar.Success("تم حفظ ملف الموظفين بنجاح.");
            else Snackbar.Info("تم إلغاء عملية حفظ ملف الموظفين.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch (HttpRequestException) { Snackbar.Error("تعذر الاتصال بالخادم لتصدير الموظفين."); }
        catch { Snackbar.Error("حدث خطأ أثناء تصدير الموظفين."); }
    }

    private async Task DownloadEmployeeTemplateAsync()
    {
        _exportDialogOpen = false;
        try
        {
            var file = await EmployeeService.DownloadTemplateAsync();
            var saved = await BrowserFileDownload.SaveAsync(
                file, "EmployeesTemplate.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            if (saved) Snackbar.Success("تم حفظ قالب الموظفين بنجاح.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { Snackbar.Error("تعذر تنزيل قالب الموظفين."); }
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
