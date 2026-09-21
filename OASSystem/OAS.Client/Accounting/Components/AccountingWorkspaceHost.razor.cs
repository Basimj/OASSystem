using System.Text.Json;
using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Common;
using OAS.Client.Accounting.Services;
using OAS.Client.Accounting.Workspace;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.UiLib.Components.Accounting.Accounts;
using OAS.UiLib.Components.Accounting.Journals;
using OAS.UiLib.Components.Accounting.PostingProfiles;
using OAS.UiLib.Components.Accounting.Vouchers;
using OAS.UiLib.Components.Accounting.Workspace;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;
using AccountingRecordItem = OAS.UiLib.Components.Accounting.Workspace.UiAccountingRecordList.RecordItem;
using AccountingDetailItem = OAS.UiLib.Components.Accounting.Workspace.UiAccountingRecordList.DetailItem;

namespace OAS.Client.Accounting.Components;

public partial class AccountingWorkspaceHost : IDisposable
{
    [Inject] private IAccountingWorkspaceState Workspace { get; set; } = default!;
    [Inject] private IAccountingClientService AccountingService { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IUiDialogService Dialog { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;

    [Parameter] public AccountingEntityType? Section { get; set; }

    private const int PageSize = 25;
    private const int AccountTreePageSize = 2000;
    private bool _isLoading;
    private bool _initialized;
    private AccountingEntityType? _loadedSection;

    private readonly Dictionary<AccountingEntityType, string> _searchByEntity = [];
    private readonly Dictionary<AccountingEntityType, int> _pageByEntity = [];

    private readonly Dictionary<Guid, UiLookupItem> _accountLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _costCenterLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _fiscalYearLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _fiscalPeriodLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _cashAccountLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _bankAccountLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _expenseTypeLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _paymentSourceLookups = [];

    private PagedResult<AccountDto> _accountsPage = new();
    private PagedResult<JournalEntryDto> _journalsPage = new();
    private PagedResult<ReceiptVoucherDto> _receiptsPage = new();
    private PagedResult<PaymentVoucherDto> _paymentsPage = new();
    private PagedResult<CashAccountDto> _cashAccountsPage = new();
    private PagedResult<BankAccountDto> _bankAccountsPage = new();
    private PagedResult<CashShiftDto> _cashShiftsPage = new();
    private PagedResult<ExpenseDto> _expensesPage = new();
    private PagedResult<ExpenseTypeDto> _expenseTypesPage = new();
    private PagedResult<CustomerAccountDto> _customerAccountsPage = new();
    private PagedResult<SupplierAccountDto> _supplierAccountsPage = new();
    private PagedResult<FiscalYearDto> _fiscalYearsPage = new();
    private PagedResult<FiscalPeriodDto> _fiscalPeriodsPage = new();
    private PagedResult<CostCenterDto> _costCentersPage = new();
    private PagedResult<PostingProfileDto> _postingProfilesPage = new();
    private PagedResult<PaymentAllocationDto> _allocationsPage = new();

    // Each accounting page owns its own visible tab strip: one permanent list tab
    // plus the new/open records that belong to that page. Tabs from other accounting
    // pages remain in state but are never mixed into the current page strip.
    private IReadOnlyList<UiApplicationTabItem> WorkspaceTabs
    {
        get
        {
            if (!Section.HasValue) return [];

            var section = Section.Value;
            var allowedEditors = GetEditorEntityTypesForSection(section);

            return Workspace.Tabs
                .Where(t =>
                    (t.IsListTab && t.EntityType == section) ||
                    (!t.IsListTab && allowedEditors.Contains(t.EntityType)))
                .Select(t => new UiApplicationTabItem(
                    t.TabId,
                    t.Title,
                    GetSectionRoute(section),
                    t.GetIconCss(),
                    Workspace.ActiveTabId == t.TabId,
                    CanClose: !t.IsListTab && t.CanClose))
                .ToArray();
        }
    }

    private AccountingEntityType CurrentListEntity => Section ?? AccountingEntityType.Accounts;

    private string CurrentTabsAriaLabel => Section.HasValue
        ? $"تبويبات {Workspace.Tabs.FirstOrDefault(t => t.IsListTab && t.EntityType == Section.Value)?.Title ?? "المحاسبة"}"
        : "تبويبات المحاسبة";

    private string CurrentSearch => GetSearch(CurrentListEntity);

    private string CurrentSearchPlaceholder => CurrentListEntity switch
    {
        AccountingEntityType.Accounts => "بحث بالكود أو اسم الحساب...",
        AccountingEntityType.Journals => "بحث برقم القيد أو البيان...",
        AccountingEntityType.ReceiptVouchers => "بحث برقم سند القبض أو البيان...",
        AccountingEntityType.PaymentVouchers => "بحث برقم سند الصرف أو البيان...",
        AccountingEntityType.CashAccounts => "بحث بالكود أو اسم الصندوق...",
        AccountingEntityType.BankAccounts => "بحث بالكود أو اسم البنك أو رقم الحساب...",
        AccountingEntityType.CashShifts => "بحث برقم الوردية...",
        AccountingEntityType.Expenses => "بحث في المصروفات وأنواعها...",
        AccountingEntityType.FiscalYears => "بحث في السنوات والفترات المالية...",
        AccountingEntityType.PostingProfiles => "بحث بكود أو اسم ملف الترحيل...",
        AccountingEntityType.CostCenters => "بحث بكود أو اسم مركز التكلفة...",
        AccountingEntityType.CustomerAccounts => "بحث بمعرف العميل أو الحساب...",
        AccountingEntityType.SupplierAccounts => "بحث بمعرف المورد أو الحساب...",
        _ => "بحث..."
    };


    private Guid? SelectedAccountId =>
        Section == AccountingEntityType.Accounts &&
        Workspace.ActiveTab is { IsListTab: false, EntityType: AccountingEntityType.Accounts, EntityId: Guid id }
            ? id
            : null;

    private IReadOnlyList<AccountTreeNode> AccountTreeNodes => BuildAccountTreeNodes();

    private bool CanEditActive =>
        Workspace.ActiveTab is { IsListTab: false, IsNew: false, IsEditMode: false, IsLoading: false, IsSaving: false } tab &&
        tab.EntityType != AccountingEntityType.CashShifts &&
        IsRecordEditable(tab);

    private bool CanSaveActive =>
        Workspace.ActiveTab is { IsListTab: false, IsSaving: false } tab &&
        (tab.IsNew || tab.IsEditMode) &&
        IsRecordEditable(tab);

    private bool CanCancelActive =>
        Workspace.ActiveTab is { IsListTab: false, IsSaving: false } tab &&
        (tab.IsNew || tab.IsEditMode);

    private bool CanCloseActive => Workspace.ActiveTab is { IsListTab: false, CanClose: true, IsSaving: false };

    protected override Task OnInitializedAsync()
    {
        Workspace.OnChange += HandleWorkspaceChanged;
        _initialized = true;
        return Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!_initialized)
            return;

        if (!Section.HasValue)
        {
            _loadedSection = null;
            return;
        }

        if (_loadedSection == Section.Value)
            return;

        _loadedSection = Section.Value;
        Workspace.OpenOrActivateListTab(Section.Value);

        if (Section.Value is AccountingEntityType.ReceiptVouchers or AccountingEntityType.PaymentVouchers)
            _pageByEntity[AccountingEntityType.PaymentAllocations] = 1;

        await LoadSectionDataAsync(Section.Value);
    }

    public void Dispose()
    {
        Workspace.OnChange -= HandleWorkspaceChanged;
        GC.SuppressFinalize(this);
    }

    private void HandleWorkspaceChanged() => _ = InvokeAsync(StateHasChanged);

    private bool IsRecordEditable(AccountingTabState tab) => tab.Model switch
    {
        JournalEntryEditor.FormModel m => m.Status is not JournalEntryStatus.Posted and not JournalEntryStatus.Reversed,
        ReceiptVoucherEditor.FormModel m => m.Status == ReceiptVoucherStatus.Draft,
        PaymentVoucherEditor.FormModel m => m.Status == PaymentVoucherStatus.Draft,
        ExpenseEditor.FormModel m => m.Status == ExpenseStatus.Draft,
        FiscalYearEditor.FormModel m => m.OriginalStatus != FiscalYearStatus.Closed,
        FiscalPeriodEditor.FormModel m => m.OriginalStatus != FiscalPeriodStatus.Closed,
        _ => true
    };

    private async Task SelectTabAsync(Guid tabId)
    {
        var tab = Workspace.FindTab(tabId);
        if (tab is null) return;

        Workspace.ActiveTabId = tabId;
        Workspace.NotifyStateChanged();

        if (!tab.IsInitialized && tab.EntityId is Guid id)
            await LoadExistingRecordAsync(tab, id);
    }

    private async Task CloseActiveTabAsync()
    {
        if (Workspace.ActiveTab is { IsListTab: false } tab)
            await CloseTabByIdAsync(tab.TabId);
    }

    private async Task CloseTabByIdAsync(Guid tabId)
    {
        var tab = Workspace.FindTab(tabId);
        if (tab is null || tab.IsListTab || !tab.CanClose) return;

        if (tab.IsDirty)
        {
            var confirmed = await Dialog.ConfirmAsync(
                "تغييرات غير محفوظة",
                $"توجد تغييرات لم يتم حفظها في «{tab.Title}». هل تريد تجاهلها؟",
                AlertTone.Warning,
                "تجاهل وإغلاق",
                "إلغاء");
            if (!confirmed) return;
        }

        var wasActive = Workspace.ActiveTabId == tabId;
        var pageTabsBeforeClose = Workspace.Tabs.Where(IsTabForCurrentSection).ToArray();
        var closedIndex = Array.FindIndex(pageTabsBeforeClose, x => x.TabId == tabId);

        Workspace.RemoveTab(tabId);

        if (wasActive)
            ActivateNearestCurrentPageTab(closedIndex);
    }

    private async Task CloseOtherTabsAsync(Guid tabId)
    {
        var others = Workspace.Tabs.Where(x => IsTabForCurrentSection(x) && !x.IsListTab && x.TabId != tabId && x.CanClose).ToArray();
        foreach (var tab in others)
        {
            if (tab.IsDirty)
            {
                var confirmed = await Dialog.ConfirmAsync(
                    "تغييرات غير محفوظة",
                    $"توجد تغييرات غير محفوظة في «{tab.Title}». هل تريد إغلاقها؟",
                    AlertTone.Warning,
                    "إغلاق",
                    "إلغاء");
                if (!confirmed) continue;
            }
            Workspace.RemoveTab(tab.TabId);
        }

        if (Workspace.FindTab(tabId) is { } keptTab && IsTabForCurrentSection(keptTab))
        {
            Workspace.ActiveTabId = keptTab.TabId;
            Workspace.NotifyStateChanged();
        }
        else
        {
            ReturnToSectionListIfNeeded();
        }
    }

    private async Task CloseAllTabsAsync()
    {
        var tabs = Workspace.Tabs.Where(x => IsTabForCurrentSection(x) && !x.IsListTab && x.CanClose).ToArray();
        foreach (var tab in tabs)
        {
            if (tab.IsDirty)
            {
                var confirmed = await Dialog.ConfirmAsync(
                    "تغييرات غير محفوظة",
                    $"توجد تغييرات غير محفوظة في «{tab.Title}». هل تريد إغلاقها؟",
                    AlertTone.Warning,
                    "إغلاق",
                    "إلغاء");
                if (!confirmed) continue;
            }
            Workspace.RemoveTab(tab.TabId);
        }
        ReturnToSectionListIfNeeded();
    }

    private void ActivateNearestCurrentPageTab(int previousIndex)
    {
        if (!Section.HasValue) return;

        var remaining = Workspace.Tabs.Where(IsTabForCurrentSection).ToArray();
        if (remaining.Length == 0)
        {
            Workspace.OpenOrActivateListTab(Section.Value);
            return;
        }

        var index = Math.Clamp(previousIndex < 0 ? 0 : previousIndex, 0, remaining.Length - 1);
        Workspace.ActiveTabId = remaining[index].TabId;
        Workspace.NotifyStateChanged();
    }

    private void ReturnToSectionListIfNeeded()
    {
        if (Section.HasValue &&
            (Workspace.ActiveTab is null || !IsTabForCurrentSection(Workspace.ActiveTab) || !Workspace.ActiveTab.IsListTab))
        {
            Workspace.OpenOrActivateListTab(Section.Value);
        }
    }

    private void BeginEditActive()
    {
        if (Workspace.ActiveTab is { IsListTab: false } tab && IsRecordEditable(tab))
        {
            tab.BeginEdit();
            Workspace.NotifyStateChanged();
        }
    }

    private void CancelActiveEdit()
    {
        if (Workspace.ActiveTab is not { IsListTab: false } tab) return;

        if (tab.IsNew)
        {
            var pageTabs = Workspace.Tabs.Where(IsTabForCurrentSection).ToArray();
            var index = Array.FindIndex(pageTabs, x => x.TabId == tab.TabId);
            Workspace.RemoveTab(tab.TabId);
            ActivateNearestCurrentPageTab(index);
            return;
        }

        if (tab.EntityId is Guid id)
            _ = ReloadAfterCancelAsync(tab, id);
    }

    private async Task ReloadAfterCancelAsync(AccountingTabState tab, Guid id)
    {
        await LoadExistingRecordAsync(tab, id);
        tab.CancelEdit();
        Workspace.NotifyStateChanged();
    }

    private void OpenNewRecord() => OpenNewRecord(CurrentListEntity);

    private void OpenNewRecord(AccountingEntityType entityType)
    {
        try
        {
            var tab = Workspace.OpenNewEntityTab(entityType);
            InitializeNewModel(tab);
            tab.CaptureBaseline(SerializeModel(tab.Model));
            Workspace.NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Error($"تعذر فتح نموذج جديد: {ex.Message}");
        }
    }

    private void InitializeNewModel(AccountingTabState tab)
    {
        tab.Model = tab.EntityType switch
        {
            AccountingEntityType.Accounts => new AccountEditor.FormModel(),
            AccountingEntityType.FiscalYears => new FiscalYearEditor.FormModel
            {
                StartDate = new DateOnly(DateTime.Today.Year, 1, 1),
                EndDate = new DateOnly(DateTime.Today.Year, 12, 31),
                Status = FiscalYearStatus.Open
            },
            AccountingEntityType.FiscalPeriods => new FiscalPeriodEditor.FormModel
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today),
                PeriodNumber = 1,
                Status = FiscalPeriodStatus.Open
            },
            AccountingEntityType.Journals => new JournalEntryEditor.FormModel(),
            AccountingEntityType.PostingProfiles => new PostingProfileEditor.FormModel(),
            AccountingEntityType.CostCenters => new CostCenterEditor.FormModel(),
            AccountingEntityType.CustomerAccounts => new CustomerAccountEditor.FormModel(),
            AccountingEntityType.SupplierAccounts => new SupplierAccountEditor.FormModel(),
            AccountingEntityType.ReceiptVouchers => new ReceiptVoucherEditor.FormModel(),
            AccountingEntityType.PaymentVouchers => new PaymentVoucherEditor.FormModel(),
            AccountingEntityType.PaymentAllocations => new PaymentAllocationEditor.FormModel(),
            AccountingEntityType.CashAccounts => new CashAccountEditor.FormModel(),
            AccountingEntityType.BankAccounts => new BankAccountEditor.FormModel(),
            AccountingEntityType.CashShifts => new CashShiftEditor.FormModel(),
            AccountingEntityType.Expenses => new ExpenseEditor.FormModel(),
            AccountingEntityType.ExpenseTypes => new ExpenseTypeEditor.FormModel(),
            _ => throw new InvalidOperationException($"لا يوجد نموذج تحرير للنوع {tab.EntityType}.")
        };
        tab.IsInitialized = true;
    }

    private async Task RefreshActiveAsync()
    {
        if (Workspace.ActiveTab is { IsListTab: false, EntityId: Guid id } tab)
            await LoadExistingRecordAsync(tab, id);
        else if (Section.HasValue)
            await LoadSectionDataAsync(Section.Value);
    }

    private string GetSearch(AccountingEntityType type) =>
        _searchByEntity.TryGetValue(type, out var value) ? value : string.Empty;

    private int GetPage(AccountingEntityType type) =>
        _pageByEntity.TryGetValue(type, out var value) ? Math.Max(1, value) : 1;

    private Task SetCurrentSearch(string? value)
    {
        _searchByEntity[CurrentListEntity] = value?.Trim() ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task OnSearchAsync(string? text)
    {
        var value = text?.Trim() ?? string.Empty;
        var section = CurrentListEntity;

        foreach (var type in GetSectionEntityTypes(section))
        {
            _searchByEntity[type] = value;
            _pageByEntity[type] = 1;
        }

        await LoadSectionDataAsync(section);
    }

    private PageRequest BuildPageRequest(AccountingEntityType type) => new()
    {
        PageNumber = type == AccountingEntityType.Accounts ? 1 : GetPage(type),
        PageSize = type == AccountingEntityType.Accounts ? AccountTreePageSize : PageSize,
        Search = string.IsNullOrWhiteSpace(GetSearch(type)) ? null : GetSearch(type)
    };

    private static IReadOnlyList<AccountingEntityType> GetSectionEntityTypes(AccountingEntityType section) => section switch
    {
        AccountingEntityType.FiscalYears => [AccountingEntityType.FiscalYears, AccountingEntityType.FiscalPeriods],
        AccountingEntityType.Expenses => [AccountingEntityType.Expenses, AccountingEntityType.ExpenseTypes],
        _ => [section]
    };

    private static IReadOnlySet<AccountingEntityType> GetEditorEntityTypesForSection(AccountingEntityType section) => section switch
    {
        AccountingEntityType.FiscalYears => new HashSet<AccountingEntityType> { AccountingEntityType.FiscalYears, AccountingEntityType.FiscalPeriods },
        AccountingEntityType.Expenses => new HashSet<AccountingEntityType> { AccountingEntityType.Expenses, AccountingEntityType.ExpenseTypes },
        AccountingEntityType.ReceiptVouchers => new HashSet<AccountingEntityType> { AccountingEntityType.ReceiptVouchers, AccountingEntityType.PaymentAllocations },
        AccountingEntityType.PaymentVouchers => new HashSet<AccountingEntityType> { AccountingEntityType.PaymentVouchers, AccountingEntityType.PaymentAllocations },
        _ => new HashSet<AccountingEntityType> { section }
    };

    private static string GetSectionRoute(AccountingEntityType section) => section switch
    {
        AccountingEntityType.Accounts => "/accounting/accounts",
        AccountingEntityType.FiscalYears => "/accounting/fiscal-years",
        AccountingEntityType.Journals => "/accounting/journals",
        AccountingEntityType.PostingProfiles => "/accounting/posting-profiles",
        AccountingEntityType.CustomerAccounts => "/accounting/customer-accounts",
        AccountingEntityType.SupplierAccounts => "/accounting/supplier-accounts",
        AccountingEntityType.ReceiptVouchers => "/accounting/receipt-vouchers",
        AccountingEntityType.PaymentVouchers => "/accounting/payment-vouchers",
        AccountingEntityType.CashAccounts => "/accounting/cash-accounts",
        AccountingEntityType.BankAccounts => "/accounting/bank-accounts",
        AccountingEntityType.CashShifts => "/accounting/cash-shifts",
        AccountingEntityType.Expenses => "/accounting/expenses",
        AccountingEntityType.CostCenters => "/accounting/cost-centers",
        _ => "/accounting"
    };

    private bool IsTabForCurrentSection(AccountingTabState tab)
    {
        if (!Section.HasValue) return false;
        if (tab.IsListTab) return tab.EntityType == Section.Value;
        return GetEditorEntityTypesForSection(Section.Value).Contains(tab.EntityType);
    }

    private async Task ChangePageAsync(AccountingEntityType type, int delta)
    {
        _pageByEntity[type] = Math.Max(1, GetPage(type) + delta);
        await LoadEntityListAsync(type);
    }

    private async Task LoadSectionDataAsync(AccountingEntityType section)
    {
        _isLoading = true;
        try
        {
            foreach (var type in GetSectionEntityTypes(section))
                await LoadEntityListAsync(type);

            if (section is AccountingEntityType.ReceiptVouchers or AccountingEntityType.PaymentVouchers)
                await LoadPaymentAllocationsForVoucherSectionAsync(section);
        }
        catch (Exception ex)
        {
            Snackbar.Error("تعذر تحميل بيانات المحاسبة: " + ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadEntityListAsync(AccountingEntityType type)
    {
        var request = BuildPageRequest(type);
        switch (type)
        {
            case AccountingEntityType.Accounts:
                var accountRows = new List<AccountDto>();
                for (var pageNumber = 1; ; pageNumber++)
                {
                    var page = await AccountingService.GetAccountsPageAsync(request with { PageNumber = pageNumber, PageSize = PageRequest.MaximumPageSize });
                    accountRows.AddRange(page.Items);
                    if (page.Items.Count == 0 || accountRows.Count >= page.TotalCount) break;
                }
                _accountsPage = new PagedResult<AccountDto> { Items = accountRows, PageNumber = 1, PageSize = Math.Max(1, accountRows.Count), TotalCount = accountRows.Count };
                RememberAccountLookups(_accountsPage.Items);
                break;
            case AccountingEntityType.FiscalYears:
                _fiscalYearsPage = await AccountingService.GetFiscalYearsPageAsync(request);
                RememberFiscalYearLookups(_fiscalYearsPage.Items);
                break;
            case AccountingEntityType.FiscalPeriods:
                _fiscalPeriodsPage = await AccountingService.GetFiscalPeriodsPageAsync(request);
                RememberFiscalPeriodLookups(_fiscalPeriodsPage.Items);
                break;
            case AccountingEntityType.Journals:
                _journalsPage = await AccountingService.GetJournalsPageAsync(request);
                break;
            case AccountingEntityType.PostingProfiles:
                _postingProfilesPage = await AccountingService.GetPostingProfilesPageAsync(request);
                break;
            case AccountingEntityType.CostCenters:
                _costCentersPage = await AccountingService.GetCostCentersPageAsync(request);
                RememberCostCenterLookups(_costCentersPage.Items);
                break;
            case AccountingEntityType.CustomerAccounts:
                _customerAccountsPage = await AccountingService.GetCustomerAccountsPageAsync(request);
                break;
            case AccountingEntityType.SupplierAccounts:
                _supplierAccountsPage = await AccountingService.GetSupplierAccountsPageAsync(request);
                break;
            case AccountingEntityType.ReceiptVouchers:
                _receiptsPage = await AccountingService.GetReceiptVouchersPageAsync(request);
                RememberPaymentSourceLookups(_receiptsPage.Items);
                break;
            case AccountingEntityType.PaymentVouchers:
                _paymentsPage = await AccountingService.GetPaymentVouchersPageAsync(request);
                RememberPaymentSourceLookups(_paymentsPage.Items);
                break;
            case AccountingEntityType.PaymentAllocations:
                _allocationsPage = await AccountingService.GetPaymentAllocationsPageAsync(request);
                break;
            case AccountingEntityType.CashAccounts:
                _cashAccountsPage = await AccountingService.GetCashAccountsPageAsync(request);
                RememberCashAccountLookups(_cashAccountsPage.Items);
                break;
            case AccountingEntityType.BankAccounts:
                _bankAccountsPage = await AccountingService.GetBankAccountsPageAsync(request);
                RememberBankAccountLookups(_bankAccountsPage.Items);
                break;
            case AccountingEntityType.CashShifts:
                _cashShiftsPage = await AccountingService.GetCashShiftsPageAsync(request);
                break;
            case AccountingEntityType.Expenses:
                _expensesPage = await AccountingService.GetExpensesPageAsync(request);
                break;
            case AccountingEntityType.ExpenseTypes:
                _expenseTypesPage = await AccountingService.GetExpenseTypesPageAsync(request);
                RememberExpenseTypeLookups(_expenseTypesPage.Items);
                break;
        }

        await HydrateListLookupsAsync(type);
    }

    private async Task LoadPaymentAllocationsForVoucherSectionAsync(AccountingEntityType section)
    {
        var sourceFilter = section == AccountingEntityType.ReceiptVouchers
            ? nameof(PaymentSourceType.ReceiptVoucher)
            : nameof(PaymentSourceType.PaymentVoucher);

        _allocationsPage = await AccountingService.GetPaymentAllocationsPageAsync(new PageRequest
        {
            PageNumber = GetPage(AccountingEntityType.PaymentAllocations),
            PageSize = PageSize,
            Search = sourceFilter
        });
        await HydrateListLookupsAsync(AccountingEntityType.PaymentAllocations);
    }

    private async Task ChangeAllocationPageAsync(int delta)
    {
        if (!Section.HasValue || Section.Value is not (AccountingEntityType.ReceiptVouchers or AccountingEntityType.PaymentVouchers))
            return;

        _pageByEntity[AccountingEntityType.PaymentAllocations] = Math.Max(1, GetPage(AccountingEntityType.PaymentAllocations) + delta);
        await LoadPaymentAllocationsForVoucherSectionAsync(Section.Value);
    }

    private Task OpenCurrentRecordAsync(Guid id) =>
        OpenRecordInTab(CurrentListEntity, id, GetEntityDisplayName(CurrentListEntity));

    private Task OpenAccountFromTreeAsync(Guid id) =>
        OpenRecordInTab(AccountingEntityType.Accounts, id, "حساب");

    private async Task OpenRecordInTab(AccountingEntityType entityType, Guid id, string title)
    {
        var tab = Workspace.OpenOrActivateEntityTab(entityType, id, title);
        if (!tab.IsInitialized)
            await LoadExistingRecordAsync(tab, id);
    }

    private async Task LoadExistingRecordAsync(AccountingTabState tab, Guid id)
    {
        tab.IsLoading = true;
        Workspace.NotifyStateChanged();

        try
        {
            switch (tab.EntityType)
            {
                case AccountingEntityType.Accounts:
                {
                    var dto = await AccountingService.GetAccountByIdAsync(id) ?? throw NotFound("الحساب");
                    await EnsureAccountLookupAsync(dto.ParentAccountId);
                    var model = new AccountEditor.FormModel
                    {
                        Code = dto.Code,
                        NameAr = dto.NameAr,
                        NameEn = dto.NameEn,
                        ParentAccountId = dto.ParentAccountId,
                        Level = dto.Level,
                        AccountClass = dto.AccountClass,
                        AccountType = dto.AccountType,
                        NormalBalance = dto.NormalBalance,
                        IsPostingAccount = dto.IsPostingAccount,
                        IsControlAccount = dto.IsControlAccount,
                        AllowManualPosting = dto.AllowManualPosting,
                        IsSystemAccount = dto.IsSystemAccount,
                        IsActive = dto.IsActive,
                        EffectiveDate = dto.EffectiveDate,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"{dto.Code} - {dto.NameAr}", model);
                    break;
                }
                case AccountingEntityType.FiscalYears:
                {
                    var dto = await AccountingService.GetFiscalYearByIdAsync(id) ?? throw NotFound("السنة المالية");
                    RememberFiscalYearLookups([dto]);
                    var model = new FiscalYearEditor.FormModel
                    {
                        Code = dto.Code,
                        Name = dto.Name,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        Status = dto.Status,
                        OriginalStatus = dto.Status,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"{dto.Code} - {dto.Name}", model);
                    break;
                }
                case AccountingEntityType.FiscalPeriods:
                {
                    var dto = await AccountingService.GetFiscalPeriodByIdAsync(id) ?? throw NotFound("الفترة المالية");
                    await EnsureFiscalYearLookupAsync(dto.FiscalYearId);
                    RememberFiscalPeriodLookups([dto]);
                    var model = new FiscalPeriodEditor.FormModel
                    {
                        FiscalYearId = dto.FiscalYearId,
                        PeriodNumber = dto.PeriodNumber,
                        Name = dto.Name,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        Status = dto.Status,
                        OriginalStatus = dto.Status,
                        SalesLocked = dto.SalesLocked,
                        InventoryLocked = dto.InventoryLocked,
                        AccountingLocked = dto.AccountingLocked,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"الفترة {dto.PeriodNumber} - {dto.Name}", model);
                    break;
                }
                case AccountingEntityType.Journals:
                {
                    var dto = await AccountingService.GetJournalByIdAsync(id) ?? throw NotFound("القيد");
                    await EnsureFiscalPeriodLookupAsync(dto.FiscalPeriodId);
                    var lines = new List<UiJournalLinesEditor.EditableJournalLine>();
                    foreach (var line in dto.Lines.OrderBy(x => x.LineNumber))
                    {
                        var account = await EnsureAccountLookupAsync(line.AccountId);
                        var costCenter = await EnsureCostCenterLookupAsync(line.CostCenterId);
                        lines.Add(new UiJournalLinesEditor.EditableJournalLine
                        {
                            AccountId = line.AccountId,
                            AccountDisplay = account?.PrimaryText ?? line.AccountId.ToString("D"),
                            AccountLookupItem = account,
                            Description = line.Description ?? string.Empty,
                            CostCenterId = line.CostCenterId,
                            CostCenterDisplay = costCenter?.PrimaryText,
                            CostCenterLookupItem = costCenter,
                            DebitAmount = line.DebitAmount,
                            CreditAmount = line.CreditAmount,
                            CustomerId = line.CustomerId,
                            SupplierId = line.SupplierId,
                            ProductVariantId = line.ProductVariantId,
                            WarehouseId = line.WarehouseId
                        });
                    }

                    var model = new JournalEntryEditor.FormModel
                    {
                        JournalNumber = dto.JournalNumber,
                        JournalType = dto.JournalType,
                        PostingDate = dto.PostingDate,
                        DocumentDate = dto.DocumentDate,
                        FiscalPeriodId = dto.FiscalPeriodId,
                        Description = dto.Description,
                        SourceModule = dto.SourceModule,
                        SourceDocumentType = dto.SourceDocumentType,
                        SourceDocumentId = dto.SourceDocumentId,
                        Status = dto.Status,
                        ReversedJournalId = dto.ReversedJournalId,
                        RowVersion = dto.RowVersion,
                        Lines = lines
                    };
                    CompleteLoadedTab(tab, dto.Id, dto.JournalNumber, model);
                    break;
                }
                case AccountingEntityType.PostingProfiles:
                {
                    var dto = await AccountingService.GetPostingProfileByIdAsync(id) ?? throw NotFound("ملف الترحيل");
                    var lines = new List<UiPostingProfileLinesEditor.EditablePostingProfileLine>();
                    foreach (var line in dto.Lines)
                    {
                        var account = await EnsureAccountLookupAsync(line.AccountId);
                        lines.Add(new UiPostingProfileLinesEditor.EditablePostingProfileLine
                        {
                            AccountRole = line.AccountRole,
                            AccountId = line.AccountId,
                            AccountDisplay = account?.PrimaryText ?? line.AccountId.ToString("D"),
                            AccountLookupItem = account,
                            IsRequired = line.IsRequired
                        });
                    }
                    var model = new PostingProfileEditor.FormModel
                    {
                        Code = dto.Code,
                        Name = dto.Name,
                        Module = dto.Module,
                        DocumentType = dto.DocumentType,
                        IsActive = dto.IsActive,
                        RowVersion = dto.RowVersion,
                        Lines = lines
                    };
                    CompleteLoadedTab(tab, dto.Id, $"{dto.Code} - {dto.Name}", model);
                    break;
                }
                case AccountingEntityType.CostCenters:
                {
                    var dto = await AccountingService.GetCostCenterByIdAsync(id) ?? throw NotFound("مركز التكلفة");
                    await EnsureCostCenterLookupAsync(dto.ParentCostCenterId);
                    RememberCostCenterLookups([dto]);
                    var model = new CostCenterEditor.FormModel
                    {
                        Code = dto.Code,
                        NameAr = dto.NameAr,
                        NameEn = dto.NameEn,
                        ParentCostCenterId = dto.ParentCostCenterId,
                        IsActive = dto.IsActive,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"{dto.Code} - {dto.NameAr}", model);
                    break;
                }
                case AccountingEntityType.CustomerAccounts:
                {
                    var dto = await AccountingService.GetCustomerAccountByIdAsync(id) ?? throw NotFound("حساب العميل");
                    await EnsureAccountLookupAsync(dto.AccountId);
                    await EnsureAccountLookupAsync(dto.ControlAccountId);
                    var model = new CustomerAccountEditor.FormModel
                    {
                        CustomerId = dto.CustomerId,
                        AccountId = dto.AccountId,
                        ControlAccountId = dto.ControlAccountId,
                        IsActive = dto.IsActive,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"حساب عميل {ShortId(dto.CustomerId)}", model);
                    break;
                }
                case AccountingEntityType.SupplierAccounts:
                {
                    var dto = await AccountingService.GetSupplierAccountByIdAsync(id) ?? throw NotFound("حساب المورد");
                    await EnsureAccountLookupAsync(dto.AccountId);
                    await EnsureAccountLookupAsync(dto.ControlAccountId);
                    var model = new SupplierAccountEditor.FormModel
                    {
                        SupplierId = dto.SupplierId,
                        AccountId = dto.AccountId,
                        ControlAccountId = dto.ControlAccountId,
                        IsActive = dto.IsActive,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"حساب مورد {ShortId(dto.SupplierId)}", model);
                    break;
                }
                case AccountingEntityType.ReceiptVouchers:
                {
                    var dto = await AccountingService.GetReceiptVoucherByIdAsync(id) ?? throw NotFound("سند القبض");
                    await EnsureCashAccountLookupAsync(dto.CashAccountId);
                    await EnsureBankAccountLookupAsync(dto.BankAccountId);
                    var lines = await MapVoucherLinesAsync(dto.Lines.Select(x => (x.AccountId, x.Amount, x.ReferenceType, x.ReferenceId, x.Description)));
                    var model = new ReceiptVoucherEditor.FormModel
                    {
                        VoucherNumber = dto.VoucherNumber,
                        VoucherDate = dto.VoucherDate,
                        PartyType = dto.PartyType,
                        CustomerId = dto.CustomerId,
                        ReceivedFrom = dto.ReceivedFrom,
                        PaymentMethod = dto.PaymentMethod,
                        CashAccountId = dto.CashAccountId,
                        BankAccountId = dto.BankAccountId,
                        TotalAmount = dto.TotalAmount,
                        Description = dto.Description ?? string.Empty,
                        Status = dto.Status,
                        RowVersion = dto.RowVersion,
                        Lines = lines
                    };
                    RememberPaymentSourceLookups([dto]);
                    CompleteLoadedTab(tab, dto.Id, dto.VoucherNumber, model);
                    break;
                }
                case AccountingEntityType.PaymentVouchers:
                {
                    var dto = await AccountingService.GetPaymentVoucherByIdAsync(id) ?? throw NotFound("سند الصرف");
                    await EnsureCashAccountLookupAsync(dto.CashAccountId);
                    await EnsureBankAccountLookupAsync(dto.BankAccountId);
                    var lines = await MapVoucherLinesAsync(dto.Lines.Select(x => (x.AccountId, x.Amount, x.ReferenceType, x.ReferenceId, x.Description)));
                    var model = new PaymentVoucherEditor.FormModel
                    {
                        VoucherNumber = dto.VoucherNumber,
                        VoucherDate = dto.VoucherDate,
                        PartyType = dto.PartyType,
                        SupplierId = dto.SupplierId,
                        BeneficiaryName = dto.BeneficiaryName,
                        PaymentMethod = dto.PaymentMethod,
                        CashAccountId = dto.CashAccountId,
                        BankAccountId = dto.BankAccountId,
                        TotalAmount = dto.TotalAmount,
                        Description = dto.Description ?? string.Empty,
                        Status = dto.Status,
                        RowVersion = dto.RowVersion,
                        Lines = lines
                    };
                    RememberPaymentSourceLookups([dto]);
                    CompleteLoadedTab(tab, dto.Id, dto.VoucherNumber, model);
                    break;
                }
                case AccountingEntityType.PaymentAllocations:
                {
                    var dto = await AccountingService.GetPaymentAllocationByIdAsync(id) ?? throw NotFound("تخصيص السداد");
                    await EnsurePaymentSourceLookupAsync(dto.PaymentSourceType, dto.PaymentSourceId);
                    var model = new PaymentAllocationEditor.FormModel
                    {
                        PaymentSourceType = dto.PaymentSourceType,
                        PaymentSourceId = dto.PaymentSourceId,
                        TargetDocumentType = dto.TargetDocumentType,
                        TargetDocumentId = dto.TargetDocumentId,
                        AllocatedAmount = dto.AllocatedAmount,
                        AllocatedAtUtc = dto.AllocatedAtUtc
                    };
                    CompleteLoadedTab(tab, dto.Id, $"تخصيص {dto.AllocatedAmount:N2}", model);
                    break;
                }
                case AccountingEntityType.CashAccounts:
                {
                    var dto = await AccountingService.GetCashAccountByIdAsync(id) ?? throw NotFound("الصندوق");
                    await EnsureAccountLookupAsync(dto.AccountId);
                    RememberCashAccountLookups([dto]);
                    var model = new CashAccountEditor.FormModel
                    {
                        Code = dto.Code,
                        Name = dto.Name,
                        AccountId = dto.AccountId,
                        IsDefault = dto.IsDefault,
                        IsActive = dto.IsActive,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"{dto.Code} - {dto.Name}", model);
                    break;
                }
                case AccountingEntityType.BankAccounts:
                {
                    var dto = await AccountingService.GetBankAccountByIdAsync(id) ?? throw NotFound("الحساب البنكي");
                    await EnsureAccountLookupAsync(dto.AccountId);
                    RememberBankAccountLookups([dto]);
                    var model = new BankAccountEditor.FormModel
                    {
                        Code = dto.Code,
                        BankName = dto.BankName,
                        AccountName = dto.AccountName,
                        AccountNumber = dto.AccountNumber,
                        IBAN = dto.IBAN,
                        AccountId = dto.AccountId,
                        IsActive = dto.IsActive,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"{dto.BankName} - {dto.AccountName}", model);
                    break;
                }
                case AccountingEntityType.CashShifts:
                {
                    var dto = await AccountingService.GetCashShiftByIdAsync(id) ?? throw NotFound("وردية الصندوق");
                    await EnsureCashAccountLookupAsync(dto.CashAccountId);
                    var model = new CashShiftEditor.FormModel
                    {
                        ShiftNumber = dto.ShiftNumber,
                        CashAccountId = dto.CashAccountId,
                        OpeningBalance = dto.OpeningBalance,
                        ExpectedClosingBalance = dto.ExpectedClosingBalance,
                        ActualClosingBalance = dto.ActualClosingBalance,
                        DifferenceAmount = dto.DifferenceAmount,
                        Status = dto.Status,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"وردية {dto.ShiftNumber}", model);
                    break;
                }
                case AccountingEntityType.Expenses:
                {
                    var dto = await AccountingService.GetExpenseByIdAsync(id) ?? throw NotFound("المصروف");
                    await EnsureExpenseTypeLookupAsync(dto.ExpenseTypeId);
                    await EnsureAccountLookupAsync(dto.ExpenseAccountId);
                    await EnsureCashAccountLookupAsync(dto.CashAccountId);
                    await EnsureBankAccountLookupAsync(dto.BankAccountId);
                    var model = new ExpenseEditor.FormModel
                    {
                        ExpenseNumber = dto.ExpenseNumber,
                        ExpenseDate = dto.ExpenseDate,
                        ExpenseTypeId = dto.ExpenseTypeId,
                        ExpenseAccountId = dto.ExpenseAccountId,
                        Beneficiary = dto.Beneficiary,
                        Amount = dto.Amount,
                        PaymentMethod = dto.PaymentMethod,
                        CashAccountId = dto.CashAccountId,
                        BankAccountId = dto.BankAccountId,
                        Description = dto.Description,
                        Status = dto.Status,
                        JournalEntryId = dto.JournalEntryId,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, dto.ExpenseNumber, model);
                    break;
                }
                case AccountingEntityType.ExpenseTypes:
                {
                    var dto = await AccountingService.GetExpenseTypeByIdAsync(id) ?? throw NotFound("نوع المصروف");
                    await EnsureAccountLookupAsync(dto.DefaultExpenseAccountId);
                    RememberExpenseTypeLookups([dto]);
                    var model = new ExpenseTypeEditor.FormModel
                    {
                        Code = dto.Code,
                        NameAr = dto.NameAr,
                        NameEn = dto.NameEn,
                        DefaultExpenseAccountId = dto.DefaultExpenseAccountId,
                        IsActive = dto.IsActive,
                        RowVersion = dto.RowVersion
                    };
                    CompleteLoadedTab(tab, dto.Id, $"{dto.Code} - {dto.NameAr}", model);
                    break;
                }
                default:
                    throw new InvalidOperationException("نوع السجل غير مدعوم.");
            }
        }
        catch (Exception ex)
        {
            Snackbar.Error("تعذر تحميل السجل: " + ex.Message);
        }
        finally
        {
            tab.IsLoading = false;
            Workspace.NotifyStateChanged();
        }
    }

    private static InvalidOperationException NotFound(string entityName) =>
        new($"لم يتم العثور على {entityName} المطلوب.");

    private static string SerializeModel(object? model) =>
        model is null ? string.Empty : JsonSerializer.Serialize(model, model.GetType());

    private static string ShortId(Guid value) => value.ToString("N")[..8];

    private void CompleteLoadedTab(AccountingTabState tab, Guid id, string title, object model)
    {
        tab.CompleteSave(id, title, model, SerializeModel(model));
        tab.IsLoading = false;
    }

    private async Task<List<UiVoucherLinesEditor.EditableVoucherLine>> MapVoucherLinesAsync(
        IEnumerable<(Guid AccountId, decimal Amount, string? ReferenceType, Guid? ReferenceId, string? Description)> source)
    {
        var result = new List<UiVoucherLinesEditor.EditableVoucherLine>();
        foreach (var line in source)
        {
            var account = await EnsureAccountLookupAsync(line.AccountId);
            result.Add(new UiVoucherLinesEditor.EditableVoucherLine
            {
                AccountId = line.AccountId,
                AccountDisplay = account?.PrimaryText ?? line.AccountId.ToString("D"),
                AccountLookupItem = account,
                Amount = line.Amount,
                ReferenceType = line.ReferenceType,
                ReferenceId = line.ReferenceId,
                Description = line.Description
            });
        }
        return result;
    }

    private async Task SaveActiveAsync()
    {
        if (Workspace.ActiveTab is not { IsListTab: false } tab || tab.Model is null || !CanSaveActive)
            return;

        tab.IsSaving = true;
        Workspace.NotifyStateChanged();

        try
        {
            switch (tab.EntityType)
            {
                case AccountingEntityType.Accounts:
                    await SaveAccountAsync(tab, (AccountEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.FiscalYears:
                    await SaveFiscalYearAsync(tab, (FiscalYearEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.FiscalPeriods:
                    await SaveFiscalPeriodAsync(tab, (FiscalPeriodEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.Journals:
                    await SaveJournalAsync(tab, (JournalEntryEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.PostingProfiles:
                    await SavePostingProfileAsync(tab, (PostingProfileEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.CostCenters:
                    await SaveCostCenterAsync(tab, (CostCenterEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.CustomerAccounts:
                    await SaveCustomerAccountAsync(tab, (CustomerAccountEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.SupplierAccounts:
                    await SaveSupplierAccountAsync(tab, (SupplierAccountEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.ReceiptVouchers:
                    await SaveReceiptVoucherAsync(tab, (ReceiptVoucherEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.PaymentVouchers:
                    await SavePaymentVoucherAsync(tab, (PaymentVoucherEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.PaymentAllocations:
                    await SavePaymentAllocationAsync(tab, (PaymentAllocationEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.CashAccounts:
                    await SaveCashAccountAsync(tab, (CashAccountEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.BankAccounts:
                    await SaveBankAccountAsync(tab, (BankAccountEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.CashShifts:
                    await SaveCashShiftAsync(tab, (CashShiftEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.Expenses:
                    await SaveExpenseAsync(tab, (ExpenseEditor.FormModel)tab.Model);
                    break;
                case AccountingEntityType.ExpenseTypes:
                    await SaveExpenseTypeAsync(tab, (ExpenseTypeEditor.FormModel)tab.Model);
                    break;
                default:
                    Snackbar.Warning("الحفظ غير متاح لهذا النوع من السجلات.");
                    break;
            }
        }
        catch (ApiClientException ex)
        {
            // أخطاء التحقق/التعارض القادمة من الـ API يجب أن تظهر للمستخدم
            // كرسالة واضحة بدون إسقاط واجهة Blazor أو إغلاق التبويب الحالي.
            ApiFeedback.Show(ex.Error);
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            tab.IsSaving = false;
            Workspace.NotifyStateChanged();
        }
    }

    private async Task SaveAccountAsync(AccountingTabState tab, AccountEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.NameAr))
        {
            Snackbar.Warning("كود الحساب والاسم العربي مطلوبان.");
            return;
        }
        if (model.Level is < 1 or > 255)
        {
            Snackbar.Warning("مستوى الحساب يجب أن يكون بين 1 و255.");
            return;
        }

        AccountDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateAccountAsync(new CreateAccountRequest(
                model.Code.Trim(), model.NameAr.Trim(), NullIfBlank(model.NameEn), model.ParentAccountId,
                checked((byte)model.Level), model.AccountClass, model.AccountType, model.NormalBalance,
                model.IsPostingAccount, model.IsControlAccount, model.AllowManualPosting,
                model.IsSystemAccount, model.IsActive, model.EffectiveDate));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateAccountAsync(tab.EntityId!.Value, new UpdateAccountRequest(
                model.Code.Trim(), model.NameAr.Trim(), NullIfBlank(model.NameEn), model.ParentAccountId,
                checked((byte)model.Level), model.AccountClass, model.AccountType, model.NormalBalance,
                model.IsPostingAccount, model.IsControlAccount, model.AllowManualPosting,
                model.IsSystemAccount, model.IsActive, model.EffectiveDate, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.Code} - {result.NameAr}", "تم حفظ الحساب بنجاح.");
    }

    private async Task SaveFiscalYearAsync(AccountingTabState tab, FiscalYearEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name) || !model.StartDate.HasValue || !model.EndDate.HasValue)
        {
            Snackbar.Warning("الكود والاسم وتاريخا بداية ونهاية السنة المالية مطلوبة.");
            return;
        }
        if (model.EndDate < model.StartDate)
        {
            Snackbar.Warning("تاريخ نهاية السنة المالية يجب أن يكون بعد تاريخ البداية.");
            return;
        }

        FiscalYearDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateFiscalYearAsync(new CreateFiscalYearRequest(
                model.Code.Trim(), model.Name.Trim(), model.StartDate.Value, model.EndDate.Value));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = model.OriginalStatus == FiscalYearStatus.Future
                ? await AccountingService.UpdateFiscalYearAsync(tab.EntityId!.Value, new UpdateFiscalYearRequest(
                    model.Code.Trim(), model.Name.Trim(), model.StartDate.Value, model.EndDate.Value, model.RowVersion))
                : await AccountingService.GetFiscalYearByIdAsync(tab.EntityId!.Value);
        }

        if (result is not null && result.Status != model.Status)
            result = await AccountingService.SetFiscalYearStatusAsync(result.Id, new SetFiscalYearStatusRequest(model.Status, result.RowVersion));

        if (result is not null)
        {
            model.Status = result.Status;
            model.OriginalStatus = result.Status;
            model.RowVersion = result.RowVersion;
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.Code} - {result.Name}", "تم حفظ السنة المالية بنجاح.");
    }

    private async Task SaveFiscalPeriodAsync(AccountingTabState tab, FiscalPeriodEditor.FormModel model)
    {
        if (model.FiscalYearId == Guid.Empty || string.IsNullOrWhiteSpace(model.Name))
        {
            Snackbar.Warning("السنة المالية واسم الفترة مطلوبان.");
            return;
        }
        if (model.PeriodNumber is < 1 or > 255)
        {
            Snackbar.Warning("رقم الفترة غير صالح.");
            return;
        }
        if (model.EndDate < model.StartDate)
        {
            Snackbar.Warning("تاريخ نهاية الفترة يجب أن يكون بعد تاريخ البداية.");
            return;
        }

        FiscalPeriodDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateFiscalPeriodAsync(new CreateFiscalPeriodRequest(
                model.FiscalYearId, checked((byte)model.PeriodNumber), model.Name.Trim(), model.StartDate, model.EndDate));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = model.OriginalStatus == FiscalPeriodStatus.Open
                ? await AccountingService.UpdateFiscalPeriodAsync(tab.EntityId!.Value, new UpdateFiscalPeriodRequest(
                    model.Name.Trim(), model.StartDate, model.EndDate, model.RowVersion))
                : await AccountingService.GetFiscalPeriodByIdAsync(tab.EntityId!.Value);
        }

        if (result is not null && result.Status != FiscalPeriodStatus.Closed &&
            (result.SalesLocked != model.SalesLocked || result.InventoryLocked != model.InventoryLocked || result.AccountingLocked != model.AccountingLocked))
        {
            result = await AccountingService.SetPeriodLocksAsync(result.Id, new SetFiscalPeriodLocksRequest(
                model.SalesLocked, model.InventoryLocked, model.AccountingLocked, result.RowVersion));
        }

        if (result is not null && result.Status != model.Status)
            result = await AccountingService.SetFiscalPeriodStatusAsync(result.Id, new SetFiscalPeriodStatusRequest(model.Status, result.RowVersion));

        if (result is not null)
        {
            model.Status = result.Status;
            model.OriginalStatus = result.Status;
            model.SalesLocked = result.SalesLocked;
            model.InventoryLocked = result.InventoryLocked;
            model.AccountingLocked = result.AccountingLocked;
            model.RowVersion = result.RowVersion;
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"الفترة {result.PeriodNumber} - {result.Name}", "تم حفظ الفترة المالية بنجاح.");
    }

    private async Task SaveJournalAsync(AccountingTabState tab, JournalEntryEditor.FormModel model)
    {
        if (!model.FiscalPeriodId.HasValue)
        {
            Snackbar.Warning("يجب اختيار الفترة المالية قبل حفظ القيد.");
            return;
        }
        if (string.IsNullOrWhiteSpace(model.Description))
        {
            Snackbar.Warning("البيان العام للقيد مطلوب.");
            return;
        }

        var validLines = model.Lines.Where(x => x.AccountId.HasValue && (x.DebitAmount > 0 || x.CreditAmount > 0)).ToArray();
        if (validLines.Length < 2)
        {
            Snackbar.Warning("القيد يحتاج سطرين صالحين على الأقل.");
            return;
        }
        if (validLines.Any(x => x.DebitAmount > 0 && x.CreditAmount > 0))
        {
            Snackbar.Warning("لا يمكن أن يكون السطر مديناً ودائناً في نفس الوقت.");
            return;
        }
        var totalDebit = validLines.Sum(x => x.DebitAmount);
        var totalCredit = validLines.Sum(x => x.CreditAmount);
        if (totalDebit != totalCredit)
        {
            Snackbar.Warning($"القيد غير متزن. المدين {totalDebit:N2} والدائن {totalCredit:N2}.");
            return;
        }

        var lines = validLines.Select(x => new CreateJournalEntryLineRequest(
            x.AccountId!.Value, x.DebitAmount, x.CreditAmount, NullIfBlank(x.Description),
            x.CustomerId, x.SupplierId, x.CostCenterId, x.ProductVariantId, x.WarehouseId)).ToArray();

        JournalEntryDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateJournalAsync(new CreateJournalEntryRequest(
                model.JournalType, model.PostingDate, model.DocumentDate, model.FiscalPeriodId.Value,
                model.Description.Trim(), model.SourceModule, model.SourceDocumentType, model.SourceDocumentId, lines));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateJournalAsync(tab.EntityId!.Value, new UpdateJournalEntryRequest(
                model.JournalType, model.PostingDate, model.DocumentDate, model.FiscalPeriodId.Value,
                model.Description.Trim(), model.SourceModule, model.SourceDocumentType, model.SourceDocumentId,
                lines, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result?.JournalNumber, "تم حفظ القيد بنجاح.");
    }

    private async Task SavePostingProfileAsync(AccountingTabState tab, PostingProfileEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name) ||
            string.IsNullOrWhiteSpace(model.Module) || string.IsNullOrWhiteSpace(model.DocumentType))
        {
            Snackbar.Warning("الكود والاسم والوحدة ونوع المستند مطلوبة.");
            return;
        }

        var lines = model.Lines
            .Where(x => x.AccountId.HasValue && !string.IsNullOrWhiteSpace(x.AccountRole))
            .Select(x => new CreatePostingProfileLineRequest(x.AccountRole.Trim(), x.AccountId!.Value, x.IsRequired))
            .ToArray();
        if (lines.Length == 0)
        {
            Snackbar.Warning("أضف قاعدة حساب واحدة على الأقل إلى ملف الترحيل.");
            return;
        }

        PostingProfileDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreatePostingProfileAsync(new CreatePostingProfileRequest(
                model.Code.Trim(), model.Name.Trim(), model.Module.Trim(), model.DocumentType.Trim(), lines, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdatePostingProfileAsync(tab.EntityId!.Value, new UpdatePostingProfileRequest(
                model.Code.Trim(), model.Name.Trim(), model.Module.Trim(), model.DocumentType.Trim(), lines, model.IsActive, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.Code} - {result.Name}", "تم حفظ ملف الترحيل بنجاح.");
    }

    private async Task SaveCostCenterAsync(AccountingTabState tab, CostCenterEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.NameAr))
        {
            Snackbar.Warning("كود مركز التكلفة والاسم العربي مطلوبان.");
            return;
        }

        CostCenterDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateCostCenterAsync(new CreateCostCenterRequest(
                model.Code.Trim(), model.NameAr.Trim(), NullIfBlank(model.NameEn), model.ParentCostCenterId, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateCostCenterAsync(tab.EntityId!.Value, new UpdateCostCenterRequest(
                model.Code.Trim(), model.NameAr.Trim(), NullIfBlank(model.NameEn), model.ParentCostCenterId, model.IsActive, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.Code} - {result.NameAr}", "تم حفظ مركز التكلفة بنجاح.");
    }

    private async Task SaveCustomerAccountAsync(AccountingTabState tab, CustomerAccountEditor.FormModel model)
    {
        if (!model.CustomerId.HasValue || !model.AccountId.HasValue || !model.ControlAccountId.HasValue)
        {
            Snackbar.Warning("معرف العميل والحساب الفرعي وحساب المراقبة مطلوبة.");
            return;
        }

        CustomerAccountDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateCustomerAccountAsync(new CreateCustomerAccountRequest(
                model.CustomerId.Value, model.AccountId.Value, model.ControlAccountId.Value, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateCustomerAccountAsync(tab.EntityId!.Value, new UpdateCustomerAccountRequest(
                model.AccountId.Value, model.ControlAccountId.Value, model.IsActive, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"حساب عميل {ShortId(result.CustomerId)}", "تم حفظ الربط المحاسبي للعميل بنجاح.");
    }

    private async Task SaveSupplierAccountAsync(AccountingTabState tab, SupplierAccountEditor.FormModel model)
    {
        if (!model.SupplierId.HasValue || !model.AccountId.HasValue || !model.ControlAccountId.HasValue)
        {
            Snackbar.Warning("معرف المورد والحساب الفرعي وحساب المراقبة مطلوبة.");
            return;
        }

        SupplierAccountDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateSupplierAccountAsync(new CreateSupplierAccountRequest(
                model.SupplierId.Value, model.AccountId.Value, model.ControlAccountId.Value, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateSupplierAccountAsync(tab.EntityId!.Value, new UpdateSupplierAccountRequest(
                model.AccountId.Value, model.ControlAccountId.Value, model.IsActive, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"حساب مورد {ShortId(result.SupplierId)}", "تم حفظ الربط المحاسبي للمورد بنجاح.");
    }

    private async Task SaveReceiptVoucherAsync(AccountingTabState tab, ReceiptVoucherEditor.FormModel model)
    {
        if (model.TotalAmount <= 0)
        {
            Snackbar.Warning("مبلغ سند القبض يجب أن يكون أكبر من صفر.");
            return;
        }
        if (model.PartyType == ReceiptPartyType.Customer && !model.CustomerId.HasValue)
        {
            Snackbar.Warning("معرف العميل مطلوب عندما يكون نوع الطرف عميلاً.");
            return;
        }
        if (model.PartyType == ReceiptPartyType.Other && string.IsNullOrWhiteSpace(model.ReceivedFrom))
        {
            Snackbar.Warning("اسم الجهة الدافعة مطلوب.");
            return;
        }
        if (!ValidatePaymentAccount(model.PaymentMethod, model.CashAccountId, model.BankAccountId))
            return;

        var lines = model.Lines
            .Where(x => x.AccountId.HasValue && x.Amount > 0)
            .Select(x => new CreateReceiptVoucherLineRequest(
                x.AccountId!.Value, x.Amount, NullIfBlank(x.ReferenceType), x.ReferenceId, NullIfBlank(x.Description)))
            .ToArray();

        if (!ValidateVoucherLines(model.TotalAmount, lines.Select(x => x.Amount)))
            return;

        ReceiptVoucherDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateReceiptVoucherAsync(new CreateReceiptVoucherRequest(
                model.VoucherDate, model.PartyType, model.CustomerId, NullIfBlank(model.ReceivedFrom),
                model.PaymentMethod, model.CashAccountId, model.BankAccountId, model.TotalAmount,
                NullIfBlank(model.Description), lines));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateReceiptVoucherAsync(tab.EntityId!.Value, new UpdateReceiptVoucherRequest(
                model.VoucherDate, model.PartyType, model.CustomerId, NullIfBlank(model.ReceivedFrom),
                model.PaymentMethod, model.CashAccountId, model.BankAccountId, model.TotalAmount,
                NullIfBlank(model.Description), lines, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result?.VoucherNumber, "تم حفظ سند القبض بنجاح.");
    }

    private async Task SavePaymentVoucherAsync(AccountingTabState tab, PaymentVoucherEditor.FormModel model)
    {
        if (model.TotalAmount <= 0)
        {
            Snackbar.Warning("مبلغ سند الصرف يجب أن يكون أكبر من صفر.");
            return;
        }
        if (model.PartyType == PaymentPartyType.Supplier && !model.SupplierId.HasValue)
        {
            Snackbar.Warning("معرف المورد مطلوب عندما يكون نوع المستفيد مورداً.");
            return;
        }
        if (model.PartyType != PaymentPartyType.Supplier && string.IsNullOrWhiteSpace(model.BeneficiaryName))
        {
            Snackbar.Warning("اسم المستفيد مطلوب.");
            return;
        }
        if (!ValidatePaymentAccount(model.PaymentMethod, model.CashAccountId, model.BankAccountId))
            return;

        var lines = model.Lines
            .Where(x => x.AccountId.HasValue && x.Amount > 0)
            .Select(x => new CreatePaymentVoucherLineRequest(
                x.AccountId!.Value, x.Amount, NullIfBlank(x.ReferenceType), x.ReferenceId, NullIfBlank(x.Description)))
            .ToArray();

        if (!ValidateVoucherLines(model.TotalAmount, lines.Select(x => x.Amount)))
            return;

        PaymentVoucherDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreatePaymentVoucherAsync(new CreatePaymentVoucherRequest(
                model.VoucherDate, model.PartyType, model.SupplierId, NullIfBlank(model.BeneficiaryName),
                model.PaymentMethod, model.CashAccountId, model.BankAccountId, model.TotalAmount,
                NullIfBlank(model.Description), lines));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdatePaymentVoucherAsync(tab.EntityId!.Value, new UpdatePaymentVoucherRequest(
                model.VoucherDate, model.PartyType, model.SupplierId, NullIfBlank(model.BeneficiaryName),
                model.PaymentMethod, model.CashAccountId, model.BankAccountId, model.TotalAmount,
                NullIfBlank(model.Description), lines, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result?.VoucherNumber, "تم حفظ سند الصرف بنجاح.");
    }

    private async Task SavePaymentAllocationAsync(AccountingTabState tab, PaymentAllocationEditor.FormModel model)
    {
        if (model.PaymentSourceType == PaymentSourceType.CustomerAdvance)
        {
            Snackbar.Warning("تخصيص سلفة العميل غير متاح حتى يتم ربط مصدر Customer Advance من موديول العملاء. استخدم سند قبض أو سند صرف متاح حالياً.");
            return;
        }

        if (!model.PaymentSourceId.HasValue || !model.TargetDocumentId.HasValue || model.AllocatedAmount <= 0)
        {
            Snackbar.Warning("مصدر الدفعة والمستند الهدف ومبلغ التخصيص مطلوبة.");
            return;
        }

        PaymentAllocationDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreatePaymentAllocationAsync(new CreatePaymentAllocationRequest(
                model.PaymentSourceType, model.PaymentSourceId.Value,
                model.TargetDocumentType, model.TargetDocumentId.Value, model.AllocatedAmount));
        }
        else
        {
            if (!tab.EntityId.HasValue) throw new InvalidOperationException("معرف التخصيص غير متوفر.");
            result = await AccountingService.UpdatePaymentAllocationAsync(tab.EntityId.Value,
                new UpdatePaymentAllocationRequest(model.AllocatedAmount));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"تخصيص {result.AllocatedAmount:N2}", "تم حفظ تخصيص السداد بنجاح.");
    }

    private async Task SaveCashAccountAsync(AccountingTabState tab, CashAccountEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name) || !model.AccountId.HasValue)
        {
            Snackbar.Warning("كود الصندوق واسمه والحساب المحاسبي المرتبط مطلوبة.");
            return;
        }

        CashAccountDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateCashAccountAsync(new CreateCashAccountRequest(
                model.Code.Trim(), model.Name.Trim(), model.AccountId.Value, model.IsDefault, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateCashAccountAsync(tab.EntityId!.Value, new UpdateCashAccountRequest(
                model.Code.Trim(), model.Name.Trim(), model.AccountId.Value, model.IsDefault, model.IsActive, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.Code} - {result.Name}", "تم حفظ الصندوق بنجاح.");
    }

    private async Task SaveBankAccountAsync(AccountingTabState tab, BankAccountEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.BankName) ||
            string.IsNullOrWhiteSpace(model.AccountName) || string.IsNullOrWhiteSpace(model.AccountNumber) || !model.AccountId.HasValue)
        {
            Snackbar.Warning("أكمل بيانات الحساب البنكي والحساب المحاسبي المرتبط.");
            return;
        }

        BankAccountDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateBankAccountAsync(new CreateBankAccountRequest(
                model.Code.Trim(), model.BankName.Trim(), model.AccountName.Trim(), model.AccountNumber.Trim(),
                NullIfBlank(model.IBAN), model.AccountId.Value, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateBankAccountAsync(tab.EntityId!.Value, new UpdateBankAccountRequest(
                model.Code.Trim(), model.BankName.Trim(), model.AccountName.Trim(), model.AccountNumber.Trim(),
                NullIfBlank(model.IBAN), model.AccountId.Value, model.IsActive, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.BankName} - {result.AccountName}", "تم حفظ الحساب البنكي بنجاح.");
    }

    private async Task SaveCashShiftAsync(AccountingTabState tab, CashShiftEditor.FormModel model)
    {
        if (!tab.IsNew)
        {
            Snackbar.Warning("الوردية الموجودة تُدار من خلال إجراءات الإغلاق والاعتماد، ولا تُعدل كتحديث عام.");
            return;
        }
        if (!model.CashAccountId.HasValue)
        {
            Snackbar.Warning("اختر الصندوق قبل فتح الوردية.");
            return;
        }
        if (model.OpeningBalance < 0)
        {
            Snackbar.Warning("رصيد الافتتاح لا يمكن أن يكون سالباً.");
            return;
        }

        var result = await AccountingService.CreateCashShiftAsync(new CreateCashShiftRequest(
            model.CashAccountId.Value, model.OpeningBalance));
        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"وردية {result.ShiftNumber}", "تم فتح الوردية بنجاح.");
    }

    private async Task SaveExpenseAsync(AccountingTabState tab, ExpenseEditor.FormModel model)
    {
        if (!model.ExpenseTypeId.HasValue || !model.ExpenseAccountId.HasValue || model.Amount <= 0)
        {
            Snackbar.Warning("نوع المصروف والحساب والمبلغ مطلوبة.");
            return;
        }
        if (!ValidatePaymentAccount(model.PaymentMethod, model.CashAccountId, model.BankAccountId))
            return;

        ExpenseDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateExpenseAsync(new CreateExpenseRequest(
                model.ExpenseDate, model.ExpenseTypeId.Value, model.ExpenseAccountId.Value,
                NullIfBlank(model.Beneficiary), model.Amount, model.PaymentMethod,
                model.CashAccountId, model.BankAccountId, NullIfBlank(model.Description)));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateExpenseAsync(tab.EntityId!.Value, new UpdateExpenseRequest(
                model.ExpenseDate, model.ExpenseTypeId.Value, model.ExpenseAccountId.Value,
                NullIfBlank(model.Beneficiary), model.Amount, model.PaymentMethod,
                model.CashAccountId, model.BankAccountId, NullIfBlank(model.Description), model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result?.ExpenseNumber, "تم حفظ المصروف بنجاح.");
    }

    private async Task SaveExpenseTypeAsync(AccountingTabState tab, ExpenseTypeEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.NameAr))
        {
            Snackbar.Warning("كود نوع المصروف والاسم العربي مطلوبان.");
            return;
        }

        ExpenseTypeDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateExpenseTypeAsync(new CreateExpenseTypeRequest(
                model.Code.Trim(), model.NameAr.Trim(), NullIfBlank(model.NameEn), model.DefaultExpenseAccountId, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateExpenseTypeAsync(tab.EntityId!.Value, new UpdateExpenseTypeRequest(
                model.Code.Trim(), model.NameAr.Trim(), NullIfBlank(model.NameEn), model.DefaultExpenseAccountId, model.IsActive, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.Code} - {result.NameAr}", "تم حفظ نوع المصروف بنجاح.");
    }

    private bool ValidatePaymentAccount(PaymentMethod method, Guid? cashAccountId, Guid? bankAccountId)
    {
        if (method == PaymentMethod.Cash && !cashAccountId.HasValue)
        {
            Snackbar.Warning("اختر الصندوق عند استخدام الدفع النقدي.");
            return false;
        }
        if (method != PaymentMethod.Cash && !bankAccountId.HasValue)
        {
            Snackbar.Warning("اختر الحساب البنكي لطريقة الدفع المحددة.");
            return false;
        }
        return true;
    }

    private bool ValidateVoucherLines(decimal totalAmount, IEnumerable<decimal> amounts)
    {
        var lineAmounts = amounts.ToArray();
        if (lineAmounts.Length == 0)
        {
            Snackbar.Warning("أضف سطراً واحداً على الأقل إلى السند.");
            return false;
        }
        var linesTotal = lineAmounts.Sum();
        if (linesTotal != totalAmount)
        {
            Snackbar.Warning($"إجمالي أسطر السند ({linesTotal:N2}) يجب أن يساوي إجمالي السند ({totalAmount:N2}).");
            return false;
        }
        return true;
    }

    private static void EnsureEntityIdAndRowVersion(AccountingTabState tab, string rowVersion)
    {
        if (!tab.EntityId.HasValue)
            throw new InvalidOperationException("معرف السجل غير متوفر للتعديل.");
        if (string.IsNullOrWhiteSpace(rowVersion))
            throw new InvalidOperationException("بيانات التزامن RowVersion غير متوفرة. أعد تحميل السجل ثم حاول مرة أخرى.");
    }

    private async Task CompleteSaveAsync(AccountingTabState tab, Guid? id, string? title, string successMessage)
    {
        if (!id.HasValue)
            throw new InvalidOperationException("لم يُرجع الخادم السجل بعد الحفظ.");

        tab.CompleteSave(id.Value, string.IsNullOrWhiteSpace(title) ? GetEntityDisplayName(tab.EntityType) : title!,
            tab.Model!, SerializeModel(tab.Model));

        await LoadExistingRecordAsync(tab, id.Value);
        await RefreshListsForEntityAsync(tab.EntityType);
        Snackbar.Success(successMessage);
    }

    private async Task RefreshListsForEntityAsync(AccountingEntityType entityType)
    {
        if (entityType == AccountingEntityType.PaymentAllocations &&
            Section is AccountingEntityType.ReceiptVouchers or AccountingEntityType.PaymentVouchers)
        {
            await LoadPaymentAllocationsForVoucherSectionAsync(Section.Value);
        }
        else
        {
            await LoadEntityListAsync(entityType);
        }

        if (Section.HasValue &&
            (GetSectionEntityTypes(Section.Value).Contains(entityType) || entityType == AccountingEntityType.PaymentAllocations))
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private void MarkActiveDirty()
    {
        if (Workspace.ActiveTab is not { IsListTab: false, Model: not null } tab) return;
        tab.CheckDirty(SerializeModel(tab.Model));
        Workspace.NotifyStateChanged();
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task ChangeJournalStatusAsync(JournalEntryStatus status)
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: JournalEntryEditor.FormModel model } tab)
            return;
        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.SetJournalStatusAsync(id, new SetJournalEntryStatusRequest(status, model.RowVersion));
            if (result is null) throw new InvalidOperationException("لم يُرجع الخادم القيد المحدث.");
            await LoadExistingRecordAsync(tab, result.Id);
            await RefreshListsForEntityAsync(AccountingEntityType.Journals);
            Snackbar.Success(status switch
            {
                JournalEntryStatus.PendingApproval => "تم إرسال القيد للاعتماد.",
                JournalEntryStatus.Approved => "تم اعتماد القيد.",
                JournalEntryStatus.Posted => "تم ترحيل القيد بنجاح.",
                _ => "تم تحديث حالة القيد."
            });
        }
        catch (Exception ex) { Snackbar.Error("تعذر تحديث حالة القيد: " + ex.Message); }
    }

    private async Task ReverseActiveJournalAsync()
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id } tab) return;
        var confirmed = await Dialog.ConfirmAsync(
            "عكس القيد",
            "سيتم إنشاء قيد عكسي مستقل ولن يتم حذف القيد التاريخي. هل تريد المتابعة؟",
            AlertTone.Warning,
            "إنشاء القيد العكسي",
            "إلغاء");
        if (!confirmed) return;

        try
        {
            var reversal = await AccountingService.ReverseJournalAsync(id) ?? throw new InvalidOperationException("لم يُرجع الخادم القيد العكسي.");
            await LoadExistingRecordAsync(tab, id);
            await LoadEntityListAsync(AccountingEntityType.Journals);
            var reversalTab = Workspace.OpenOrActivateEntityTab(AccountingEntityType.Journals, reversal.Id, reversal.JournalNumber);
            await LoadExistingRecordAsync(reversalTab, reversal.Id);
            Snackbar.Success("تم إنشاء القيد العكسي بنجاح.");
        }
        catch (Exception ex) { Snackbar.Error("تعذر عكس القيد: " + ex.Message); }
    }

    private async Task ChangeReceiptStatusAsync(ReceiptVoucherStatus status)
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: ReceiptVoucherEditor.FormModel model } tab) return;
        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.SetReceiptVoucherStatusAsync(id, new SetReceiptVoucherStatusRequest(status, model.RowVersion));
            if (result is null) throw new InvalidOperationException("لم يُرجع الخادم سند القبض المحدث.");
            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.ReceiptVouchers);
            Snackbar.Success(status == ReceiptVoucherStatus.Posted ? "تم ترحيل سند القبض وربطه بالقيد المحاسبي." : "تم اعتماد سند القبض.");
        }
        catch (Exception ex) { Snackbar.Error("تعذر تحديث سند القبض: " + ex.Message); }
    }

    private async Task ChangePaymentStatusAsync(PaymentVoucherStatus status)
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: PaymentVoucherEditor.FormModel model } tab) return;
        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.SetPaymentVoucherStatusAsync(id, new SetPaymentVoucherStatusRequest(status, model.RowVersion));
            if (result is null) throw new InvalidOperationException("لم يُرجع الخادم سند الصرف المحدث.");
            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.PaymentVouchers);
            Snackbar.Success(status == PaymentVoucherStatus.Posted ? "تم ترحيل سند الصرف وربطه بالقيد المحاسبي." : "تم اعتماد سند الصرف.");
        }
        catch (Exception ex) { Snackbar.Error("تعذر تحديث سند الصرف: " + ex.Message); }
    }

    private async Task ChangeExpenseStatusAsync(ExpenseStatus status)
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: ExpenseEditor.FormModel model } tab) return;
        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.SetExpenseStatusAsync(id, new SetExpenseStatusRequest(status, model.RowVersion));
            if (result is null) throw new InvalidOperationException("لم يُرجع الخادم المصروف المحدث.");
            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.Expenses);
            Snackbar.Success(status == ExpenseStatus.Posted ? "تم ترحيل المصروف وربطه بالقيد المحاسبي." : "تم اعتماد المصروف.");
        }
        catch (Exception ex) { Snackbar.Error("تعذر تحديث حالة المصروف: " + ex.Message); }
    }

    private async Task CloseActiveCashShiftAsync()
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: CashShiftEditor.FormModel model } tab) return;
        if (!model.ActualClosingBalance.HasValue)
        {
            Snackbar.Warning("أدخل قيمة الجرد الفعلي قبل إغلاق الوردية.");
            return;
        }

        var confirmed = await Dialog.ConfirmAsync(
            "إغلاق الوردية",
            $"سيتم إغلاق الوردية على جرد فعلي قدره {model.ActualClosingBalance.Value:N2}. هل تريد المتابعة؟",
            AlertTone.Warning,
            "إغلاق الوردية",
            "إلغاء");
        if (!confirmed) return;

        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.CloseCashShiftAsync(id,
                new SetCashShiftClosingRequest(model.ActualClosingBalance.Value, model.RowVersion));
            if (result is null) throw new InvalidOperationException("لم يُرجع الخادم الوردية المحدثة.");
            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.CashShifts);
            Snackbar.Success("تم إغلاق الوردية وتثبيت نتيجة الجرد.");
        }
        catch (Exception ex) { Snackbar.Error("تعذر إغلاق الوردية: " + ex.Message); }
    }

    private async Task ApproveActiveCashShiftAsync()
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: CashShiftEditor.FormModel model } tab) return;
        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.ApproveCashShiftAsync(id, model.RowVersion);
            if (result is null) throw new InvalidOperationException("لم يُرجع الخادم الوردية المحدثة.");
            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.CashShifts);
            Snackbar.Success("تم اعتماد الوردية.");
        }
        catch (Exception ex) { Snackbar.Error("تعذر اعتماد الوردية: " + ex.Message); }
    }

    // Lookup providers. All accounting-owned lookups query the API; no UI mock data is generated.
    private async Task<IReadOnlyList<UiLookupItem>> SearchAccountsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
        RememberAccountLookups(result.Items);
        return result.Items.Select(ToAccountLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCostCentersAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetCostCentersPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
        RememberCostCenterLookups(result.Items);
        return result.Items.Select(ToCostCenterLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchFiscalYearsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetFiscalYearsPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
        RememberFiscalYearLookups(result.Items);
        return result.Items.Select(ToFiscalYearLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchFiscalPeriodsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetFiscalPeriodsPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
        RememberFiscalPeriodLookups(result.Items);
        return result.Items.Select(ToFiscalPeriodLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCashAccountsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetCashAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
        RememberCashAccountLookups(result.Items);
        return result.Items.Select(ToCashAccountLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchBankAccountsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetBankAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
        RememberBankAccountLookups(result.Items);
        return result.Items.Select(ToBankAccountLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchExpenseTypesAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetExpenseTypesPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
        RememberExpenseTypeLookups(result.Items);
        return result.Items.Select(ToExpenseTypeLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchPaymentSourcesAsync(string text, CancellationToken cancellationToken)
    {
        if (Workspace.ActiveTab?.Model is not PaymentAllocationEditor.FormModel model)
            return [];

        if (model.PaymentSourceType == PaymentSourceType.ReceiptVoucher)
        {
            var result = await AccountingService.GetReceiptVouchersPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
            RememberPaymentSourceLookups(result.Items);
            return result.Items.Select(x => new UiLookupItem(x.Id.ToString("D"), x.VoucherNumber, $"{x.TotalAmount:N2} · {AccountingArabicPresenter.GetReceiptVoucherStatusText(x.Status)}", "fa-solid fa-money-bill-trend-up")).ToArray();
        }

        if (model.PaymentSourceType == PaymentSourceType.PaymentVoucher)
        {
            var result = await AccountingService.GetPaymentVouchersPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = text }, cancellationToken);
            RememberPaymentSourceLookups(result.Items);
            return result.Items.Select(x => new UiLookupItem(x.Id.ToString("D"), x.VoucherNumber, $"{x.TotalAmount:N2} · {AccountingArabicPresenter.GetPaymentVoucherStatusText(x.Status)}", "fa-solid fa-money-bill-transfer")).ToArray();
        }

        return [];
    }

    private UiLookupItem? GetAccountLookup(Guid? id) => GetLookup(_accountLookups, id);
    private UiLookupItem? GetCostCenterLookup(Guid? id) => GetLookup(_costCenterLookups, id);
    private UiLookupItem? GetFiscalYearLookup(Guid? id) => GetLookup(_fiscalYearLookups, id);
    private UiLookupItem? GetFiscalPeriodLookup(Guid? id) => GetLookup(_fiscalPeriodLookups, id);
    private UiLookupItem? GetCashAccountLookup(Guid? id) => GetLookup(_cashAccountLookups, id);
    private UiLookupItem? GetBankAccountLookup(Guid? id) => GetLookup(_bankAccountLookups, id);
    private UiLookupItem? GetExpenseTypeLookup(Guid? id) => GetLookup(_expenseTypeLookups, id);
    private UiLookupItem? GetPaymentSourceLookup(Guid? id) => GetLookup(_paymentSourceLookups, id);

    private static UiLookupItem? GetLookup(IReadOnlyDictionary<Guid, UiLookupItem> source, Guid? id) =>
        id.HasValue && source.TryGetValue(id.Value, out var item) ? item : null;

    private static UiLookupItem? GetExternalReferenceLookup(Guid? id, string label) =>
        id.HasValue
            ? new UiLookupItem(id.Value.ToString("D"), $"{label} مرتبط", id.Value.ToString("D"), "fa-solid fa-link")
            : null;

    private async Task<UiLookupItem?> EnsureAccountLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_accountLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetAccountByIdAsync(id.Value);
        if (dto is null) return null;
        var item = ToAccountLookup(dto);
        _accountLookups[dto.Id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureCostCenterLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_costCenterLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetCostCenterByIdAsync(id.Value);
        if (dto is null) return null;
        var item = ToCostCenterLookup(dto);
        _costCenterLookups[dto.Id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureFiscalYearLookupAsync(Guid id)
    {
        if (_fiscalYearLookups.TryGetValue(id, out var existing)) return existing;
        var dto = await AccountingService.GetFiscalYearByIdAsync(id);
        if (dto is null) return null;
        var item = ToFiscalYearLookup(dto);
        _fiscalYearLookups[id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureFiscalPeriodLookupAsync(Guid id)
    {
        if (_fiscalPeriodLookups.TryGetValue(id, out var existing)) return existing;
        var dto = await AccountingService.GetFiscalPeriodByIdAsync(id);
        if (dto is null) return null;
        var item = ToFiscalPeriodLookup(dto);
        _fiscalPeriodLookups[id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureCashAccountLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_cashAccountLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetCashAccountByIdAsync(id.Value);
        if (dto is null) return null;
        var item = ToCashAccountLookup(dto);
        _cashAccountLookups[id.Value] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureBankAccountLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_bankAccountLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetBankAccountByIdAsync(id.Value);
        if (dto is null) return null;
        var item = ToBankAccountLookup(dto);
        _bankAccountLookups[id.Value] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureExpenseTypeLookupAsync(Guid id)
    {
        if (_expenseTypeLookups.TryGetValue(id, out var existing)) return existing;
        var dto = await AccountingService.GetExpenseTypeByIdAsync(id);
        if (dto is null) return null;
        var item = ToExpenseTypeLookup(dto);
        _expenseTypeLookups[id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsurePaymentSourceLookupAsync(PaymentSourceType type, Guid id)
    {
        if (_paymentSourceLookups.TryGetValue(id, out var existing)) return existing;
        UiLookupItem? item = null;
        if (type == PaymentSourceType.ReceiptVoucher)
        {
            var dto = await AccountingService.GetReceiptVoucherByIdAsync(id);
            if (dto is not null)
                item = new UiLookupItem(dto.Id.ToString("D"), dto.VoucherNumber, $"{dto.TotalAmount:N2}", "fa-solid fa-money-bill-trend-up");
        }
        else if (type == PaymentSourceType.PaymentVoucher)
        {
            var dto = await AccountingService.GetPaymentVoucherByIdAsync(id);
            if (dto is not null)
                item = new UiLookupItem(dto.Id.ToString("D"), dto.VoucherNumber, $"{dto.TotalAmount:N2}", "fa-solid fa-money-bill-transfer");
        }
        if (item is not null) _paymentSourceLookups[id] = item;
        return item;
    }

    private static UiLookupItem ToAccountLookup(AccountDto x) =>
        new(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", AccountingArabicPresenter.GetAccountTypeText(x.AccountType), "fa-solid fa-folder-tree");
    private static UiLookupItem ToCostCenterLookup(CostCenterDto x) =>
        new(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", x.NameEn, "fa-solid fa-network-wired");
    private static UiLookupItem ToFiscalYearLookup(FiscalYearDto x) =>
        new(x.Id.ToString("D"), $"{x.Code} - {x.Name}", $"{x.StartDate:yyyy-MM-dd} — {x.EndDate:yyyy-MM-dd}", "fa-solid fa-calendar-days");
    private static UiLookupItem ToFiscalPeriodLookup(FiscalPeriodDto x) =>
        new(x.Id.ToString("D"), $"{x.PeriodNumber} - {x.Name}", AccountingArabicPresenter.GetFiscalPeriodStatusText(x.Status), "fa-solid fa-calendar-week");
    private static UiLookupItem ToCashAccountLookup(CashAccountDto x) =>
        new(x.Id.ToString("D"), $"{x.Code} - {x.Name}", x.IsDefault ? "الصندوق الافتراضي" : null, "fa-solid fa-vault");
    private static UiLookupItem ToBankAccountLookup(BankAccountDto x) =>
        new(x.Id.ToString("D"), $"{x.BankName} - {x.AccountName}", x.AccountNumber, "fa-solid fa-building-columns");
    private static UiLookupItem ToExpenseTypeLookup(ExpenseTypeDto x) =>
        new(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", x.NameEn, "fa-solid fa-tags");

    private void RememberAccountLookups(IEnumerable<AccountDto> items)
    {
        foreach (var item in items) _accountLookups[item.Id] = ToAccountLookup(item);
    }
    private void RememberCostCenterLookups(IEnumerable<CostCenterDto> items)
    {
        foreach (var item in items) _costCenterLookups[item.Id] = ToCostCenterLookup(item);
    }
    private void RememberFiscalYearLookups(IEnumerable<FiscalYearDto> items)
    {
        foreach (var item in items) _fiscalYearLookups[item.Id] = ToFiscalYearLookup(item);
    }
    private void RememberFiscalPeriodLookups(IEnumerable<FiscalPeriodDto> items)
    {
        foreach (var item in items) _fiscalPeriodLookups[item.Id] = ToFiscalPeriodLookup(item);
    }
    private void RememberCashAccountLookups(IEnumerable<CashAccountDto> items)
    {
        foreach (var item in items) _cashAccountLookups[item.Id] = ToCashAccountLookup(item);
    }
    private void RememberBankAccountLookups(IEnumerable<BankAccountDto> items)
    {
        foreach (var item in items) _bankAccountLookups[item.Id] = ToBankAccountLookup(item);
    }
    private void RememberExpenseTypeLookups(IEnumerable<ExpenseTypeDto> items)
    {
        foreach (var item in items) _expenseTypeLookups[item.Id] = ToExpenseTypeLookup(item);
    }
    private void RememberPaymentSourceLookups(IEnumerable<ReceiptVoucherDto> items)
    {
        foreach (var x in items)
            _paymentSourceLookups[x.Id] = new UiLookupItem(x.Id.ToString("D"), x.VoucherNumber, $"{x.TotalAmount:N2}", "fa-solid fa-money-bill-trend-up");
    }
    private void RememberPaymentSourceLookups(IEnumerable<PaymentVoucherDto> items)
    {
        foreach (var x in items)
            _paymentSourceLookups[x.Id] = new UiLookupItem(x.Id.ToString("D"), x.VoucherNumber, $"{x.TotalAmount:N2}", "fa-solid fa-money-bill-transfer");
    }

    private async Task HydrateListLookupsAsync(AccountingEntityType type)
    {
        switch (type)
        {
            case AccountingEntityType.FiscalPeriods:
                foreach (var x in _fiscalPeriodsPage.Items) await EnsureFiscalYearLookupAsync(x.FiscalYearId);
                break;
            case AccountingEntityType.PostingProfiles:
                foreach (var id in _postingProfilesPage.Items.SelectMany(x => x.Lines).Select(x => x.AccountId).Distinct())
                    await EnsureAccountLookupAsync(id);
                break;
            case AccountingEntityType.CustomerAccounts:
                foreach (var id in _customerAccountsPage.Items.SelectMany(x => new[] { x.AccountId, x.ControlAccountId }).Distinct())
                    await EnsureAccountLookupAsync(id);
                break;
            case AccountingEntityType.SupplierAccounts:
                foreach (var id in _supplierAccountsPage.Items.SelectMany(x => new[] { x.AccountId, x.ControlAccountId }).Distinct())
                    await EnsureAccountLookupAsync(id);
                break;
            case AccountingEntityType.ReceiptVouchers:
                foreach (var x in _receiptsPage.Items)
                {
                    await EnsureCashAccountLookupAsync(x.CashAccountId);
                    await EnsureBankAccountLookupAsync(x.BankAccountId);
                }
                break;
            case AccountingEntityType.PaymentVouchers:
                foreach (var x in _paymentsPage.Items)
                {
                    await EnsureCashAccountLookupAsync(x.CashAccountId);
                    await EnsureBankAccountLookupAsync(x.BankAccountId);
                }
                break;
            case AccountingEntityType.PaymentAllocations:
                foreach (var x in _allocationsPage.Items) await EnsurePaymentSourceLookupAsync(x.PaymentSourceType, x.PaymentSourceId);
                break;
            case AccountingEntityType.CashAccounts:
                foreach (var x in _cashAccountsPage.Items) await EnsureAccountLookupAsync(x.AccountId);
                break;
            case AccountingEntityType.BankAccounts:
                foreach (var x in _bankAccountsPage.Items) await EnsureAccountLookupAsync(x.AccountId);
                break;
            case AccountingEntityType.CashShifts:
                foreach (var x in _cashShiftsPage.Items) await EnsureCashAccountLookupAsync(x.CashAccountId);
                break;
            case AccountingEntityType.Expenses:
                foreach (var x in _expensesPage.Items)
                {
                    await EnsureExpenseTypeLookupAsync(x.ExpenseTypeId);
                    await EnsureAccountLookupAsync(x.ExpenseAccountId);
                    await EnsureCashAccountLookupAsync(x.CashAccountId);
                    await EnsureBankAccountLookupAsync(x.BankAccountId);
                }
                break;
            case AccountingEntityType.ExpenseTypes:
                foreach (var x in _expenseTypesPage.Items) await EnsureAccountLookupAsync(x.DefaultExpenseAccountId);
                break;
        }
    }

    private IReadOnlyList<AccountingRecordItem> CurrentRecordItems => CurrentListEntity switch
    {
        AccountingEntityType.Accounts => BuildAccountItems(),
        AccountingEntityType.Journals => BuildJournalItems(),
        AccountingEntityType.PostingProfiles => BuildPostingProfileItems(),
        AccountingEntityType.CostCenters => BuildCostCenterItems(),
        AccountingEntityType.CustomerAccounts => BuildCustomerAccountItems(),
        AccountingEntityType.SupplierAccounts => BuildSupplierAccountItems(),
        AccountingEntityType.ReceiptVouchers => BuildReceiptItems(),
        AccountingEntityType.PaymentVouchers => BuildPaymentItems(),
        AccountingEntityType.PaymentAllocations => BuildAllocationItems(),
        AccountingEntityType.CashShifts => BuildCashShiftItems(),
        _ => []
    };

    private string CurrentEmptyTitle => CurrentListEntity switch
    {
        AccountingEntityType.Accounts => "لا توجد حسابات",
        AccountingEntityType.Journals => "لا توجد قيود يومية",
        AccountingEntityType.PostingProfiles => "لا توجد ملفات ترحيل",
        AccountingEntityType.CostCenters => "لا توجد مراكز تكلفة",
        AccountingEntityType.CustomerAccounts => "لا توجد روابط محاسبية للعملاء",
        AccountingEntityType.SupplierAccounts => "لا توجد روابط محاسبية للموردين",
        AccountingEntityType.ReceiptVouchers => "لا توجد سندات قبض",
        AccountingEntityType.PaymentVouchers => "لا توجد سندات صرف",
        AccountingEntityType.CashShifts => "لا توجد ورديات صندوق",
        _ => "لا توجد بيانات"
    };

    private string CurrentEmptyDescription =>
        string.IsNullOrWhiteSpace(CurrentSearch)
            ? "استخدم زر الإضافة لإنشاء أول سجل في هذه الشاشة."
            : "لم تطابق أي سجلات عبارة البحث الحالية.";

    private string CurrentEmptyIcon => CurrentListEntity switch
    {
        AccountingEntityType.Accounts => "fa-solid fa-folder-tree",
        AccountingEntityType.Journals => "fa-solid fa-book-journal-whills",
        AccountingEntityType.PostingProfiles => "fa-solid fa-sliders",
        AccountingEntityType.CostCenters => "fa-solid fa-network-wired",
        AccountingEntityType.CustomerAccounts => "fa-solid fa-users-line",
        AccountingEntityType.SupplierAccounts => "fa-solid fa-boxes-packing",
        AccountingEntityType.ReceiptVouchers => "fa-solid fa-money-bill-trend-up",
        AccountingEntityType.PaymentVouchers => "fa-solid fa-money-bill-transfer",
        AccountingEntityType.CashShifts => "fa-solid fa-cash-register",
        _ => "fa-regular fa-folder-open"
    };

    private int CurrentPageNumber => GetPageInfo(CurrentListEntity).PageNumber;
    private int CurrentTotalPages => GetPageInfo(CurrentListEntity).TotalPages;
    private long CurrentTotalCount => GetPageInfo(CurrentListEntity).TotalCount;

    private (int PageNumber, int TotalPages, long TotalCount) GetPageInfo(AccountingEntityType type) => type switch
    {
        AccountingEntityType.Accounts => (_accountsPage.PageNumber, _accountsPage.TotalPages, _accountsPage.TotalCount),
        AccountingEntityType.FiscalYears => (_fiscalYearsPage.PageNumber, _fiscalYearsPage.TotalPages, _fiscalYearsPage.TotalCount),
        AccountingEntityType.FiscalPeriods => (_fiscalPeriodsPage.PageNumber, _fiscalPeriodsPage.TotalPages, _fiscalPeriodsPage.TotalCount),
        AccountingEntityType.Journals => (_journalsPage.PageNumber, _journalsPage.TotalPages, _journalsPage.TotalCount),
        AccountingEntityType.PostingProfiles => (_postingProfilesPage.PageNumber, _postingProfilesPage.TotalPages, _postingProfilesPage.TotalCount),
        AccountingEntityType.CostCenters => (_costCentersPage.PageNumber, _costCentersPage.TotalPages, _costCentersPage.TotalCount),
        AccountingEntityType.CustomerAccounts => (_customerAccountsPage.PageNumber, _customerAccountsPage.TotalPages, _customerAccountsPage.TotalCount),
        AccountingEntityType.SupplierAccounts => (_supplierAccountsPage.PageNumber, _supplierAccountsPage.TotalPages, _supplierAccountsPage.TotalCount),
        AccountingEntityType.ReceiptVouchers => (_receiptsPage.PageNumber, _receiptsPage.TotalPages, _receiptsPage.TotalCount),
        AccountingEntityType.PaymentVouchers => (_paymentsPage.PageNumber, _paymentsPage.TotalPages, _paymentsPage.TotalCount),
        AccountingEntityType.PaymentAllocations => (_allocationsPage.PageNumber, _allocationsPage.TotalPages, _allocationsPage.TotalCount),
        AccountingEntityType.CashAccounts => (_cashAccountsPage.PageNumber, _cashAccountsPage.TotalPages, _cashAccountsPage.TotalCount),
        AccountingEntityType.BankAccounts => (_bankAccountsPage.PageNumber, _bankAccountsPage.TotalPages, _bankAccountsPage.TotalCount),
        AccountingEntityType.CashShifts => (_cashShiftsPage.PageNumber, _cashShiftsPage.TotalPages, _cashShiftsPage.TotalCount),
        AccountingEntityType.Expenses => (_expensesPage.PageNumber, _expensesPage.TotalPages, _expensesPage.TotalCount),
        AccountingEntityType.ExpenseTypes => (_expenseTypesPage.PageNumber, _expenseTypesPage.TotalPages, _expenseTypesPage.TotalCount),
        _ => (1, 0, 0)
    };

    private IReadOnlyList<AccountTreeNode> BuildAccountTreeNodes()
    {
        if (_accountsPage.Items.Count == 0)
            return [];

        var accounts = _accountsPage.Items
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var knownIds = accounts
            .Select(x => x.Id)
            .ToHashSet();

        /*
         * لا نضع ParentAccountId = null داخل Dictionary.
         * الحسابات التي ParentAccountId لها null هي Roots
         * ويتم التعامل معها بشكل مستقل بالأسفل.
         */
        var childrenByParent = accounts
            .Where(x => x.ParentAccountId.HasValue)
            .GroupBy(x => x.ParentAccountId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .ToList());

        IReadOnlyList<AccountTreeNode> BuildChildren(
            Guid parentId,
            HashSet<Guid> path)
        {
            if (!childrenByParent.TryGetValue(
                    parentId,
                    out var children))
            {
                return [];
            }

            var result = new List<AccountTreeNode>();

            foreach (var account in children)
            {
                /*
                 * حماية من بيانات خاطئة تحتوي Cycle:
                 *
                 * A -> B -> A
                 *
                 * حتى لا يحدث StackOverflow.
                 */
                if (path.Contains(account.Id))
                    continue;

                var childPath = new HashSet<Guid>(path)
            {
                account.Id
            };

                result.Add(
                    new AccountTreeNode(
                        account.Id,
                        account.Code,
                        account.NameAr,
                        account.IsActive,
                        BuildChildren(
                            account.Id,
                            childPath)));
            }

            return result;
        }

        /*
         * Root:
         * 1- ليس له ParentAccountId.
         * 2- أو يشير إلى Parent غير موجود ضمن البيانات الحالية.
         *
         * الحالة الثانية مهمة أيضاً عند استخدام البحث.
         */
        var rootAccounts = accounts
            .Where(x =>
                !x.ParentAccountId.HasValue ||
                !knownIds.Contains(x.ParentAccountId.Value))
            .OrderBy(
                x => x.Code,
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roots = new List<AccountTreeNode>();

        foreach (var account in rootAccounts)
        {
            var path = new HashSet<Guid>
        {
            account.Id
        };

            roots.Add(
                new AccountTreeNode(
                    account.Id,
                    account.Code,
                    account.NameAr,
                    account.IsActive,
                    BuildChildren(
                        account.Id,
                        path)));
        }

        return roots;
    }

    private IReadOnlyList<AccountingRecordItem> BuildAccountItems() => _accountsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.NameAr}",
            x.NameEn,
            "fa-solid fa-folder-tree",
            [
                new("التصنيف", AccountingArabicPresenter.GetAccountClassText(x.AccountClass)),
                new("النوع", AccountingArabicPresenter.GetAccountTypeText(x.AccountType)),
                new("المستوى", x.Level.ToString()),
                new("الطبيعة", AccountingArabicPresenter.GetNormalBalanceText(x.NormalBalance), x.NormalBalance == NormalBalance.Debit ? "ui-acc-val--debit" : "ui-acc-val--credit")
            ],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildFiscalYearItems() => _fiscalYearsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.Name}",
            $"{x.StartDate:yyyy-MM-dd} — {x.EndDate:yyyy-MM-dd}",
            "fa-solid fa-calendar-days",
            [new("البداية", x.StartDate.ToString("yyyy-MM-dd")), new("النهاية", x.EndDate.ToString("yyyy-MM-dd"))],
            AccountingArabicPresenter.GetFiscalYearStatusText(x.Status),
            FiscalYearState(x.Status))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildFiscalPeriodItems() => _fiscalPeriodsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"الفترة {x.PeriodNumber} - {x.Name}",
            GetFiscalYearLookup(x.FiscalYearId)?.PrimaryText,
            "fa-solid fa-calendar-week",
            [
                new("من", x.StartDate.ToString("yyyy-MM-dd")),
                new("إلى", x.EndDate.ToString("yyyy-MM-dd")),
                new("المحاسبة", x.AccountingLocked ? "مقفلة" : "مفتوحة")
            ],
            AccountingArabicPresenter.GetFiscalPeriodStatusText(x.Status),
            FiscalPeriodState(x.Status))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildJournalItems() => _journalsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            x.JournalNumber,
            x.Description,
            "fa-solid fa-book-journal-whills",
            [
                new("تاريخ الترحيل", x.PostingDate.ToString("yyyy-MM-dd")),
                new("تاريخ المستند", x.DocumentDate.ToString("yyyy-MM-dd")),
                new("النوع", AccountingArabicPresenter.GetJournalTypeText(x.JournalType)),
                new("المصدر", string.IsNullOrWhiteSpace(x.SourceModule) ? "يدوي" : x.SourceModule)
            ],
            AccountingArabicPresenter.GetJournalStatusText(x.Status),
            JournalState(x.Status))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildPostingProfileItems() => _postingProfilesPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.Name}",
            $"{x.Module} / {x.DocumentType}",
            "fa-solid fa-sliders",
            [new("الوحدة", x.Module), new("نوع المستند", x.DocumentType)],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildCostCenterItems() => _costCentersPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.NameAr}",
            x.NameEn,
            "fa-solid fa-network-wired",
            [new("المركز الأب", x.ParentCostCenterId.HasValue ? GetCostCenterLookup(x.ParentCostCenterId)?.PrimaryText ?? ShortId(x.ParentCostCenterId.Value) : "رئيسي")],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildCustomerAccountItems() => _customerAccountsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"عميل {ShortId(x.CustomerId)}",
            x.CustomerId.ToString("D"),
            "fa-solid fa-user-tag",
            [
                new("الحساب", GetAccountLookup(x.AccountId)?.PrimaryText ?? ShortId(x.AccountId)),
                new("حساب المراقبة", GetAccountLookup(x.ControlAccountId)?.PrimaryText ?? ShortId(x.ControlAccountId))
            ],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildSupplierAccountItems() => _supplierAccountsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"مورد {ShortId(x.SupplierId)}",
            x.SupplierId.ToString("D"),
            "fa-solid fa-boxes-packing",
            [
                new("الحساب", GetAccountLookup(x.AccountId)?.PrimaryText ?? ShortId(x.AccountId)),
                new("حساب المراقبة", GetAccountLookup(x.ControlAccountId)?.PrimaryText ?? ShortId(x.ControlAccountId))
            ],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildReceiptItems() => _receiptsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            x.VoucherNumber,
            x.PartyType == ReceiptPartyType.Customer
                ? (x.CustomerId.HasValue ? $"عميل {ShortId(x.CustomerId.Value)}" : "عميل")
                : x.ReceivedFrom,
            "fa-solid fa-money-bill-trend-up",
            [
                new("التاريخ", x.VoucherDate.ToString("yyyy-MM-dd")),
                new("المبلغ", x.TotalAmount.ToString("N2"), "ui-acc-val--debit"),
                new("الطريقة", AccountingArabicPresenter.GetPaymentMethodText(x.PaymentMethod)),
                new("الحساب", GetMoneyAccountText(x.PaymentMethod, x.CashAccountId, x.BankAccountId))
            ],
            AccountingArabicPresenter.GetReceiptVoucherStatusText(x.Status),
            VoucherState(x.Status.ToString()))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildPaymentItems() => _paymentsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            x.VoucherNumber,
            x.PartyType == PaymentPartyType.Supplier
                ? (x.SupplierId.HasValue ? $"مورد {ShortId(x.SupplierId.Value)}" : "مورد")
                : x.BeneficiaryName,
            "fa-solid fa-money-bill-transfer",
            [
                new("التاريخ", x.VoucherDate.ToString("yyyy-MM-dd")),
                new("المبلغ", x.TotalAmount.ToString("N2"), "ui-acc-val--credit"),
                new("الطريقة", AccountingArabicPresenter.GetPaymentMethodText(x.PaymentMethod)),
                new("الحساب", GetMoneyAccountText(x.PaymentMethod, x.CashAccountId, x.BankAccountId))
            ],
            AccountingArabicPresenter.GetPaymentVoucherStatusText(x.Status),
            VoucherState(x.Status.ToString()))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildCashAccountItems() => _cashAccountsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.Name}",
            x.IsDefault ? "الصندوق الافتراضي" : null,
            "fa-solid fa-vault",
            [new("حساب GL", GetAccountLookup(x.AccountId)?.PrimaryText ?? ShortId(x.AccountId))],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildBankAccountItems() => _bankAccountsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.BankName}",
            x.AccountName,
            "fa-solid fa-building-columns",
            [
                new("رقم الحساب", x.AccountNumber),
                new("IBAN", string.IsNullOrWhiteSpace(x.IBAN) ? "-" : x.IBAN),
                new("حساب GL", GetAccountLookup(x.AccountId)?.PrimaryText ?? ShortId(x.AccountId))
            ],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildCashShiftItems() => _cashShiftsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"وردية {x.ShiftNumber}",
            GetCashAccountLookup(x.CashAccountId)?.PrimaryText,
            "fa-solid fa-cash-register",
            [
                new("الافتتاح", x.OpeningBalance.ToString("N2")),
                new("المتوقع", x.ExpectedClosingBalance?.ToString("N2") ?? "-"),
                new("الفعلي", x.ActualClosingBalance?.ToString("N2") ?? "-"),
                new("الفارق", x.DifferenceAmount?.ToString("N2") ?? "-")
            ],
            AccountingArabicPresenter.GetCashShiftStatusText(x.Status),
            CashShiftState(x.Status))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildExpenseItems() => _expensesPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            x.ExpenseNumber,
            x.Beneficiary,
            "fa-solid fa-receipt",
            [
                new("التاريخ", x.ExpenseDate.ToString("yyyy-MM-dd")),
                new("النوع", GetExpenseTypeLookup(x.ExpenseTypeId)?.PrimaryText ?? ShortId(x.ExpenseTypeId)),
                new("المبلغ", x.Amount.ToString("N2"), "ui-acc-val--credit"),
                new("الدفع", AccountingArabicPresenter.GetPaymentMethodText(x.PaymentMethod))
            ],
            AccountingArabicPresenter.GetExpenseStatusText(x.Status),
            ExpenseState(x.Status))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildExpenseTypeItems() => _expenseTypesPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.NameAr}",
            x.NameEn,
            "fa-solid fa-tags",
            [new("الحساب الافتراضي", x.DefaultExpenseAccountId.HasValue ? GetAccountLookup(x.DefaultExpenseAccountId)?.PrimaryText ?? ShortId(x.DefaultExpenseAccountId.Value) : "غير محدد")],
            x.IsActive ? "نشط" : "معطل",
            x.IsActive ? "active" : "inactive")).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildAllocationItems() => _allocationsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{AccountingArabicPresenter.GetPaymentSourceTypeText(x.PaymentSourceType)} - {GetPaymentSourceLookup(x.PaymentSourceId)?.PrimaryText ?? ShortId(x.PaymentSourceId)}",
            AccountingArabicPresenter.GetAllocationTargetDocumentTypeText(x.TargetDocumentType),
            "fa-solid fa-link",
            [
                new("المستند الهدف", ShortId(x.TargetDocumentId)),
                new("المبلغ", x.AllocatedAmount.ToString("N2"), "ui-acc-val--debit"),
                new("التاريخ", x.AllocatedAtUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm"))
            ],
            "مخصص",
            "active")).ToArray();

    private string GetMoneyAccountText(PaymentMethod method, Guid? cashId, Guid? bankId) =>
        method == PaymentMethod.Cash
            ? GetCashAccountLookup(cashId)?.PrimaryText ?? (cashId.HasValue ? ShortId(cashId.Value) : "-")
            : GetBankAccountLookup(bankId)?.PrimaryText ?? (bankId.HasValue ? ShortId(bankId.Value) : "-");

    private static string JournalState(JournalEntryStatus value) => value switch
    {
        JournalEntryStatus.Draft => "draft",
        JournalEntryStatus.PendingApproval => "pending",
        JournalEntryStatus.Approved => "approved",
        JournalEntryStatus.Posted => "posted",
        JournalEntryStatus.Reversed => "cancelled",
        _ => "draft"
    };

    private static string VoucherState(string value) => value switch
    {
        nameof(ReceiptVoucherStatus.Draft) => "draft",
        nameof(ReceiptVoucherStatus.Approved) => "approved",
        nameof(ReceiptVoucherStatus.Posted) => "posted",
        nameof(ReceiptVoucherStatus.Cancelled) => "cancelled",
        _ => "draft"
    };

    private static string ExpenseState(ExpenseStatus value) => value switch
    {
        ExpenseStatus.Draft => "draft",
        ExpenseStatus.Approved => "approved",
        ExpenseStatus.Posted => "posted",
        ExpenseStatus.Cancelled => "cancelled",
        _ => "draft"
    };

    private static string FiscalYearState(FiscalYearStatus value) => value switch
    {
        FiscalYearStatus.Future => "draft",
        FiscalYearStatus.Open => "active",
        FiscalYearStatus.Closing => "pending",
        FiscalYearStatus.Closed => "inactive",
        _ => "draft"
    };

    private static string FiscalPeriodState(FiscalPeriodStatus value) => value switch
    {
        FiscalPeriodStatus.Open => "active",
        FiscalPeriodStatus.SoftClosed => "pending",
        FiscalPeriodStatus.Closed => "inactive",
        _ => "draft"
    };

    private static string CashShiftState(CashShiftStatus value) => value switch
    {
        CashShiftStatus.Open => "active",
        CashShiftStatus.Closing => "pending",
        CashShiftStatus.Closed => "inactive",
        CashShiftStatus.Approved => "approved",
        _ => "draft"
    };

    private static string GetEntityDisplayName(AccountingEntityType type) => type switch
    {
        AccountingEntityType.Accounts => "حساب",
        AccountingEntityType.FiscalYears => "سنة مالية",
        AccountingEntityType.FiscalPeriods => "فترة مالية",
        AccountingEntityType.Journals => "قيد يومية",
        AccountingEntityType.PostingProfiles => "ملف ترحيل",
        AccountingEntityType.CostCenters => "مركز تكلفة",
        AccountingEntityType.CustomerAccounts => "حساب عميل",
        AccountingEntityType.SupplierAccounts => "حساب مورد",
        AccountingEntityType.ReceiptVouchers => "سند قبض",
        AccountingEntityType.PaymentVouchers => "سند صرف",
        AccountingEntityType.PaymentAllocations => "تخصيص دفعة",
        AccountingEntityType.CashAccounts => "صندوق",
        AccountingEntityType.BankAccounts => "حساب بنكي",
        AccountingEntityType.CashShifts => "وردية صندوق",
        AccountingEntityType.Expenses => "مصروف",
        AccountingEntityType.ExpenseTypes => "نوع مصروف",
        _ => "سجل"
    };
}
