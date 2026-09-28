using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Accounting.Common;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Customers;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Spreadsheets;
using OAS.UiLib.Components.Spreadsheets;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Accounting;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Accounting.Pages;

public partial class CustomersPage
{
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private AccountingSpreadsheetClient Spreadsheets { get; set; } = default!;
    [Inject] private BrowserFileDownloadService Download { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IUiDialogService Dialog { get; set; } = default!;

    private const int PageSize = 25;

    private readonly Guid _listTabId = Guid.NewGuid();
    private readonly List<PartyEditorTab> _editorTabs = [];
    private readonly UiAccountingPartyFormModel _emptyForm = new();
    private readonly Dictionary<Guid, AccountDto> _accountLookups = [];

    private Guid _activeTabId;
    private bool _busy;
    private bool _importOpen;
    private bool _spreadsheetBusy;
    private int _pageNumber = 1;
    private int _totalPages = 1;
    private long _totalCount;
    private long _importKey;
    private string _search = string.Empty;
    private string _filter = "all";
    private string _fileName = string.Empty;
    private byte[]? _fileBytes;
    private SpreadsheetPreview? _preview;
    private IReadOnlyList<UiAccountingPartyListItem> _items = [];

    private PartyEditorTab? ActiveEditorTab =>
        _editorTabs.FirstOrDefault(x => x.TabId == _activeTabId);

    private UiAccountingPartyFormModel ActiveForm =>
        ActiveEditorTab?.Form ?? _emptyForm;

    private bool EditorOpen => ActiveEditorTab is not null;
    private bool IsNew => ActiveEditorTab?.IsNew ?? false;

    private IReadOnlyList<UiApplicationTabItem> WorkspaceTabs
    {
        get
        {
            var tabs = new List<UiApplicationTabItem>
            {
                new(
                    _listTabId,
                    "العملاء",
                    "/accounting/customers",
                    "fa-solid fa-users",
                    _activeTabId == _listTabId,
                    CanClose: false)
            };

            tabs.AddRange(_editorTabs.Select(tab => new UiApplicationTabItem(
                tab.TabId,
                tab.Title,
                "/accounting/customers",
                tab.IsNew ? "fa-solid fa-user-plus" : "fa-solid fa-user-tie",
                _activeTabId == tab.TabId,
                CanClose: true)));

            return tabs;
        }
    }

    private UiSpreadsheetPreview? UiPreview => _preview is null
        ? null
        : new UiSpreadsheetPreview(
            _preview.Rows
                .Select(x => new UiSpreadsheetRow(
                    x.Sheet,
                    x.RowNumber,
                    x.Values,
                    x.Errors,
                    x.Warnings))
                .ToArray(),
            _preview.ValidRows,
            _preview.InvalidRows,
            _preview.WarningRows,
            _preview.CanImport,
            _preview.ImportedRecords);

    protected override async Task OnInitializedAsync()
    {
        _activeTabId = _listTabId;
        await LoadAsync();
    }

    private Task SetSearch(string? value)
    {
        _search = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task SearchAsync(string? value)
    {
        _search = value ?? string.Empty;
        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task FilterChangedAsync(string? value)
    {
        _filter = string.IsNullOrWhiteSpace(value) ? "all" : value;
        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task LoadAsync() => await RunAsync(async () =>
    {
        var page = await Accounting.GetCustomersPageAsync(
            new PageRequest
            {
                PageNumber = _pageNumber,
                PageSize = PageSize,
                Search = _search,
                SortBy = "CustomerCode"
            },
            _filter);

        _pageNumber = page.PageNumber <= 0 ? 1 : page.PageNumber;
        _totalPages = Math.Max(1, page.TotalPages);
        _totalCount = page.TotalCount;
        _items = page.Items.Select(ToListItem).ToArray();
    });

    private UiLookupItem? ParentAccountLookupItem =>
        Guid.TryParse(ActiveForm.ParentAccountId, out var id) &&
        _accountLookups.TryGetValue(id, out var account)
            ? AccountingLookupEligibility.ToAccountLookup(
                account,
                AccountingAccountEligibilityContext.AssetControlParent)
            : null;

    private Task<IReadOnlyList<UiLookupItem>> SearchParentAccountsAsync(
        string text,
        CancellationToken cancellationToken) =>
        SearchParentAccountsAsync(
            text,
            AccountingAccountEligibilityContext.AssetControlParent,
            cancellationToken);

    private async Task<IReadOnlyList<UiLookupItem>> SearchParentAccountsAsync(
        string text,
        AccountingAccountEligibilityContext context,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        var accounts = new List<AccountDto>();
        var pageNumber = 1;

        while (true)
        {
            var page = await Accounting.GetAccountsPageAsync(
                new PageRequest
                {
                    PageNumber = pageNumber,
                    PageSize = PageRequest.MaximumPageSize,
                    Search = normalizedSearch,
                    SortBy = "Code",
                    SortDirection = SortDirection.Ascending
                },
                cancellationToken);

            accounts.AddRange(page.Items);

            foreach (var account in page.Items)
                _accountLookups[account.Id] = account;

            if (normalizedSearch is null || !page.HasNextPage)
                break;

            pageNumber++;
        }

        return accounts
            .Select(x => AccountingLookupEligibility.ToAccountLookup(x, context))
            .ToArray();
    }

    private async Task EnsureParentLookupAsync(Guid? accountId)
    {
        if (!accountId.HasValue || _accountLookups.ContainsKey(accountId.Value))
            return;

        var account = await Accounting.GetAccountByIdAsync(accountId.Value);
        if (account is not null)
            _accountLookups[account.Id] = account;
    }

    private async Task<bool> ValidateParentAccountAsync(Guid parentId)
    {
        var account = await Accounting.GetAccountByIdAsync(parentId);
        if (account is null)
        {
            Snackbar.Warning("الحساب الرئيسي المحدد غير موجود.");
            return false;
        }

        _accountLookups[account.Id] = account;

        var eligibility = AccountingAccountEligibility.Evaluate(
            account,
            AccountingAccountEligibilityContext.AssetControlParent);

        if (eligibility.IsEligible)
            return true;

        Snackbar.Warning(
            eligibility.Reason ??
            "الحساب الرئيسي المحدد غير صالح لحسابات العملاء.");

        return false;
    }

    private async Task NewAsync() => await RunAsync(async () =>
    {
        var reserved = await Accounting.ReserveCustomerCodeAsync();

        var tab = new PartyEditorTab(
            entityId: null,
            title: "عميل جديد",
            form: new UiAccountingPartyFormModel
            {
                Code = reserved?.CustomerCode ?? string.Empty,
                EntityType = "1",
                PreferredContactMethod = "2",
                IsActive = true
            });

        _editorTabs.Add(tab);
        _activeTabId = tab.TabId;
    });

    private async Task OpenAsync(Guid id)
    {
        var existing = _editorTabs.FirstOrDefault(x => x.EntityId == id);
        if (existing is not null)
        {
            _activeTabId = existing.TabId;
            return;
        }

        await RunAsync(async () =>
        {
            var dto = await Accounting.GetCustomerByIdAsync(id);
            if (dto is null)
            {
                Snackbar.Warning("تعذر العثور على العميل.");
                return;
            }

            await EnsureParentLookupAsync(dto.ParentAccountId);

            var tab = new PartyEditorTab(
                dto.Id,
                GetTabTitle(dto.NameAr, dto.CustomerCode),
                FromDto(dto));

            _editorTabs.Add(tab);
            _activeTabId = tab.TabId;
        });
    }

    private async Task SaveAsync()
    {
        var tab = ActiveEditorTab;
        if (tab is null)
            return;

        await RunAsync(async () =>
        {
            var form = tab.Form;

            if (string.IsNullOrWhiteSpace(form.NameAr))
            {
                Snackbar.Warning("الاسم العربي مطلوب.");
                return;
            }

            if (!Enum.TryParse<PartyEntityType>(form.EntityType, out var entityType) ||
                entityType == PartyEntityType.Unknown)
            {
                Snackbar.Warning("اختر نوع الكيان.");
                return;
            }

            if (!Enum.TryParse<ContactMethod>(form.PreferredContactMethod, out var contact) ||
                contact == ContactMethod.Unspecified)
            {
                Snackbar.Warning("اختر وسيلة التواصل المفضلة.");
                return;
            }

            CustomerDto? result;

            if (tab.IsNew)
            {
                if (!Guid.TryParse(form.ParentAccountId, out var parent))
                {
                    Snackbar.Warning("اختر حساب العملاء الرئيسي.");
                    return;
                }

                if (!await ValidateParentAccountAsync(parent))
                    return;

                result = await Accounting.CreateCustomerAsync(
                    new CreateCustomerRequest(
                        form.Code,
                        parent,
                        entityType,
                        form.NameAr.Trim(),
                        N(form.NameEn),
                        N(form.TradeName),
                        N(form.NationalId),
                        N(form.CommercialRegistrationNo),
                        N(form.TaxNumber),
                        form.DateOfBirth,
                        Enum.TryParse<Gender>(form.Gender, out var gender)
                            ? gender
                            : Gender.Unspecified,
                        N(form.ContactPersonName),
                        N(form.ContactPersonTitle),
                        N(form.Phone),
                        N(form.Mobile),
                        N(form.AlternatePhone),
                        N(form.WhatsAppNumber),
                        N(form.Email),
                        N(form.Website),
                        contact,
                        N(form.Country),
                        N(form.Governorate),
                        N(form.City),
                        N(form.District),
                        N(form.Street),
                        N(form.Building),
                        N(form.PostalCode),
                        N(form.AddressDetails),
                        form.IsCreditAllowed,
                        form.IsCreditAllowed ? form.CreditLimit : 0,
                        form.IsCreditAllowed ? form.PaymentTermDays : 0,
                        form.Since,
                        form.IsActive,
                        N(form.Notes)));
            }
            else
            {
                if (!form.Id.HasValue || string.IsNullOrWhiteSpace(form.RowVersion))
                    return;

                result = await Accounting.UpdateCustomerAsync(
                    form.Id.Value,
                    new UpdateCustomerRequest(
                        entityType,
                        form.NameAr.Trim(),
                        N(form.NameEn),
                        N(form.TradeName),
                        N(form.NationalId),
                        N(form.CommercialRegistrationNo),
                        N(form.TaxNumber),
                        form.DateOfBirth,
                        Enum.TryParse<Gender>(form.Gender, out var gender)
                            ? gender
                            : Gender.Unspecified,
                        N(form.ContactPersonName),
                        N(form.ContactPersonTitle),
                        N(form.Phone),
                        N(form.Mobile),
                        N(form.AlternatePhone),
                        N(form.WhatsAppNumber),
                        N(form.Email),
                        N(form.Website),
                        contact,
                        N(form.Country),
                        N(form.Governorate),
                        N(form.City),
                        N(form.District),
                        N(form.Street),
                        N(form.Building),
                        N(form.PostalCode),
                        N(form.AddressDetails),
                        form.IsCreditAllowed,
                        form.IsCreditAllowed ? form.CreditLimit : 0,
                        form.IsCreditAllowed ? form.PaymentTermDays : 0,
                        form.Since,
                        N(form.Notes),
                        form.RowVersion));
            }

            if (result is null)
                return;

            tab.EntityId = result.Id;
            tab.Form = FromDto(result);
            tab.Title = GetTabTitle(result.NameAr, result.CustomerCode);
            tab.IsDirty = false;

            await EnsureParentLookupAsync(result.ParentAccountId);
            Snackbar.Success("تم حفظ العميل بنجاح.");
            await LoadAsync();
        });
    }

    private async Task CancelAsync()
    {
        if (ActiveEditorTab is { } tab)
            await CloseTabAsync(tab.TabId);
    }

    private async Task ToggleStatusAsync()
    {
        var tab = ActiveEditorTab;
        if (tab is null)
            return;

        await RunAsync(async () =>
        {
            var form = tab.Form;
            if (!form.Id.HasValue || string.IsNullOrWhiteSpace(form.RowVersion))
                return;

            if (tab.IsDirty)
            {
                Snackbar.Warning("احفظ التعديلات قبل تغيير حالة العميل.");
                return;
            }

            var result = await Accounting.SetCustomerStatusAsync(
                form.Id.Value,
                new SetCustomerStatusRequest(!form.IsActive, form.RowVersion));

            if (result is null)
                return;

            tab.Form = FromDto(result);
            tab.Title = GetTabTitle(result.NameAr, result.CustomerCode);
            tab.IsDirty = false;

            await LoadAsync();
            Snackbar.Success(result.IsActive ? "تم تفعيل العميل." : "تم تعطيل العميل.");
        });
    }

    private Task SelectTabAsync(Guid tabId)
    {
        if (tabId == _listTabId || _editorTabs.Any(x => x.TabId == tabId))
            _activeTabId = tabId;

        return Task.CompletedTask;
    }

    private async Task CloseTabAsync(Guid tabId)
    {
        var tab = _editorTabs.FirstOrDefault(x => x.TabId == tabId);
        if (tab is null)
            return;

        if (!await CanDiscardTabAsync(tab))
            return;

        var index = _editorTabs.IndexOf(tab);
        var wasActive = _activeTabId == tab.TabId;
        _editorTabs.Remove(tab);

        if (!wasActive)
            return;

        if (_editorTabs.Count == 0)
        {
            _activeTabId = _listTabId;
            return;
        }

        var nextIndex = Math.Clamp(index, 0, _editorTabs.Count - 1);
        _activeTabId = _editorTabs[nextIndex].TabId;
    }

    private async Task CloseOtherTabsAsync(Guid tabId)
    {
        var tabsToClose = _editorTabs
            .Where(x => x.TabId != tabId)
            .ToArray();

        foreach (var tab in tabsToClose)
        {
            if (!await CanDiscardTabAsync(tab))
                continue;

            _editorTabs.Remove(tab);
        }

        _activeTabId = tabId == _listTabId || _editorTabs.Any(x => x.TabId == tabId)
            ? tabId
            : _listTabId;
    }

    private async Task CloseAllTabsAsync()
    {
        foreach (var tab in _editorTabs.ToArray())
        {
            if (!await CanDiscardTabAsync(tab))
                continue;

            _editorTabs.Remove(tab);
        }

        _activeTabId = _listTabId;
    }

    private async Task<bool> CanDiscardTabAsync(PartyEditorTab tab)
    {
        if (!tab.IsDirty)
            return true;

        return await Dialog.ConfirmAsync(
            "تغييرات غير محفوظة",
            $"توجد تغييرات لم يتم حفظها في «{tab.Title}». هل تريد تجاهلها؟",
            AlertTone.Warning,
            confirmText: "تجاهل وإغلاق",
            cancelText: "إلغاء");
    }

    private Task MarkActiveTabDirty()
    {
        if (ActiveEditorTab is { } tab)
            tab.IsDirty = true;

        return Task.CompletedTask;
    }

    private async Task PreviousPageAsync()
    {
        if (_pageNumber > 1)
        {
            _pageNumber--;
            await LoadAsync();
        }
    }

    private async Task NextPageAsync()
    {
        if (_pageNumber < _totalPages)
        {
            _pageNumber++;
            await LoadAsync();
        }
    }

    private Task OpenImport()
    {
        _preview = null;
        _fileBytes = null;
        _fileName = string.Empty;
        _importKey++;
        _importOpen = true;
        return Task.CompletedTask;
    }

    private Task CloseImport()
    {
        if (!_spreadsheetBusy)
            _importOpen = false;

        return Task.CompletedTask;
    }

    private Task ClearImport()
    {
        if (!_spreadsheetBusy)
        {
            _preview = null;
            _fileBytes = null;
            _fileName = string.Empty;
        }

        return Task.CompletedTask;
    }

    private async Task PreviewImportAsync(IBrowserFile file) =>
        await RunSpreadsheetAsync(async () =>
        {
            _preview = null;
            _fileBytes = null;
            _fileName = string.Empty;

            if (!file.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                file.Size > 10 * 1024 * 1024)
            {
                _preview = InvalidFile();
                return;
            }

            using var ms = new MemoryStream();
            await using var stream = file.OpenReadStream(10 * 1024 * 1024);
            await stream.CopyToAsync(ms);
            _fileBytes = ms.ToArray();
            _fileName = file.Name;
            _preview = await Spreadsheets.UploadAsync("customers", _fileBytes, _fileName, false);
        });

    private async Task ConfirmImportAsync() =>
        await RunSpreadsheetAsync(async () =>
        {
            if (_fileBytes is null || _preview is null || !_preview.CanImport)
                return;

            _preview = await Spreadsheets.UploadAsync("customers", _fileBytes, _fileName, true);
            if (_preview.ImportedRecords > 0)
            {
                Snackbar.Success($"تم استيراد {_preview.ImportedRecords} عميل.");
                await LoadAsync();
            }
        });

    private async Task DownloadTemplateAsync() =>
        await RunSpreadsheetAsync(async () =>
            await Download.SaveAsync(
                await Spreadsheets.TemplateAsync("customers"),
                "customers-template.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

    private async Task ExportAsync() =>
        await RunSpreadsheetAsync(async () =>
            await Download.SaveAsync(
                await Spreadsheets.ExportAsync("customers", _search, null, _filter),
                "customers.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

    private async Task RunAsync(Func<Task> action)
    {
        if (_busy)
            return;

        _busy = true;
        try
        {
            await action();
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RunSpreadsheetAsync(Func<Task> action)
    {
        if (_spreadsheetBusy)
            return;

        _spreadsheetBusy = true;
        try
        {
            await action();
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _spreadsheetBusy = false;
        }
    }

    private void ShowAccountingError(OAS.Contracts.Common.Errors.ApiError error)
    {
        if (AccountingApiErrorPresenter.TryGetMessage(error, out var message))
        {
            Snackbar.Error(message);
            return;
        }

        ApiFeedback.Show(error);
    }

    private static string GetTabTitle(string? name, string code) =>
        string.IsNullOrWhiteSpace(name) ? code : name.Trim();

    private static string? N(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SpreadsheetPreview InvalidFile() =>
        new([
            new SpreadsheetRowResult(
                "الملف",
                1,
                new Dictionary<string, string>(),
                ["اختر ملف xlsx بحجم لا يتجاوز 10 MB."],
                [])
        ]);

    private static UiAccountingPartyListItem ToListItem(CustomerDto x) =>
        new(
            x.Id,
            x.CustomerCode,
            x.AccountCode,
            x.NameAr,
            x.TradeName ?? x.NameEn,
            x.Mobile ?? x.Phone,
            string.Join(" / ", new[] { x.City, x.Governorate, x.Country }
                .Where(v => !string.IsNullOrWhiteSpace(v))),
            x.TaxNumber ?? x.NationalId,
            x.EntityType switch
            {
                PartyEntityType.Individual => "فرد",
                PartyEntityType.Organization => "منشأة",
                _ => "غير محدد"
            },
            $"{x.CreditLimit:N2} · {x.PaymentTermDays} يوم",
            x.IsActive);

    private static UiAccountingPartyFormModel FromDto(CustomerDto x) => new()
    {
        Id = x.Id,
        Code = x.CustomerCode,
        AccountCode = x.AccountCode,
        ParentAccountId = x.ParentAccountId?.ToString("D") ?? string.Empty,
        EntityType = ((byte)x.EntityType).ToString(),
        NameAr = x.NameAr,
        NameEn = x.NameEn,
        TradeName = x.TradeName,
        NationalId = x.NationalId,
        CommercialRegistrationNo = x.CommercialRegistrationNo,
        TaxNumber = x.TaxNumber,
        DateOfBirth = x.DateOfBirth,
        Gender = ((byte)x.Gender).ToString(),
        ContactPersonName = x.ContactPersonName,
        ContactPersonTitle = x.ContactPersonTitle,
        Phone = x.Phone,
        Mobile = x.Mobile,
        AlternatePhone = x.AlternatePhone,
        WhatsAppNumber = x.WhatsAppNumber,
        Email = x.Email,
        Website = x.Website,
        PreferredContactMethod = ((byte)x.PreferredContactMethod).ToString(),
        Country = x.Country,
        Governorate = x.Governorate,
        City = x.City,
        District = x.District,
        Street = x.Street,
        Building = x.Building,
        PostalCode = x.PostalCode,
        AddressDetails = x.AddressDetails,
        IsCreditAllowed = x.IsCreditAllowed,
        CreditLimit = x.CreditLimit,
        PaymentTermDays = x.PaymentTermDays,
        Since = x.CustomerSince,
        IsActive = x.IsActive,
        Notes = x.Notes,
        RowVersion = x.RowVersion
    };

    private sealed class PartyEditorTab(
        Guid? entityId,
        string title,
        UiAccountingPartyFormModel form)
    {
        public Guid TabId { get; } = Guid.NewGuid();
        public Guid? EntityId { get; set; } = entityId;
        public string Title { get; set; } = title;
        public UiAccountingPartyFormModel Form { get; set; } = form;
        public bool IsDirty { get; set; }
        public bool IsNew => !EntityId.HasValue;
    }
}
