using System.Text.Json;
using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Common;
using OAS.Client.Accounting.Services;
using OAS.Client.Accounting.Workspace;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Printing.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.EmployeeAccounts;
using OAS.Contracts.Accounting.Customers;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Accounting.Suppliers;
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
using UiSettlementPartyKind = OAS.UiLib.Components.Accounting.Vouchers.UiVoucherSettlementLinesEditor.SettlementPartyKind;
using UiPaymentMethodKind = OAS.UiLib.Components.Accounting.Vouchers.UiVoucherSettlementLinesEditor.PaymentMethodKind;
using UiExchangeRateSourceKind = OAS.UiLib.Components.Accounting.Vouchers.UiVoucherSettlementLinesEditor.ExchangeRateSourceKind;

namespace OAS.Client.Accounting.Components;

public partial class AccountingWorkspaceHost : IDisposable
{
    [Inject] private IAccountingWorkspaceState Workspace { get; set; } = default!;
    [Inject] private IAccountingClientService AccountingService { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IUiDialogService Dialog { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IPrintingClientService PrintingService { get; set; } = default!;

    [Parameter] public AccountingEntityType? Section { get; set; }

    private const int PageSize = 25;
    private bool _isLoading;
    private bool _isPrinting;
    private bool _initialized;
    private AccountingEntityType? _loadedSection;

    private readonly Dictionary<AccountingEntityType, string> _searchByEntity = [];
    private readonly Dictionary<AccountingEntityType, int> _pageByEntity = [];

    private readonly Dictionary<Guid, UiLookupItem> _accountLookups = [];
    private readonly Dictionary<Guid, AccountDto> _accountDtos = [];
    private readonly Dictionary<Guid, UiLookupItem> _costCenterLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _fiscalYearLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _fiscalPeriodLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _cashAccountLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _bankAccountLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _expenseTypeLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _paymentSourceLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _customerLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _supplierLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _currencyLookups = [];
    private readonly Dictionary<Guid, UiLookupItem> _employeeAccountLookups = [];

    private PagedResult<AccountDto> _accountsPage = new();
    private PagedResult<JournalEntryDto> _journalsPage = new();
    private PagedResult<ReceiptVoucherDto> _receiptsPage = new();
    private PagedResult<PaymentVoucherDto> _paymentsPage = new();
    private PagedResult<CashAccountDto> _cashAccountsPage = new();
    private PagedResult<BankAccountDto> _bankAccountsPage = new();
    private PagedResult<CashShiftDto> _cashShiftsPage = new();
    private PagedResult<ExpenseDto> _expensesPage = new();
    private PagedResult<ExpenseTypeDto> _expenseTypesPage = new();
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

    private bool ShowStandardEditActions => Workspace.ActiveTab?.Model switch
    {
        ReceiptVoucherEditor.FormModel m => m.Status == ReceiptVoucherStatus.Draft,
        PaymentVoucherEditor.FormModel m => m.Status == PaymentVoucherStatus.Draft,
        _ => true
    };

    private bool CanSaveActive =>
        Workspace.ActiveTab is { IsListTab: false, IsSaving: false } tab &&
        (tab.IsNew || tab.IsEditMode) &&
        IsRecordEditable(tab);

    private bool CanCancelActive =>
        Workspace.ActiveTab is { IsListTab: false, IsSaving: false } tab &&
        (tab.IsNew || tab.IsEditMode);

    private bool CanPrintActive =>
        Workspace.ActiveTab is
        {
            IsListTab: false,
            IsNew: false,
            IsEditMode: false,
            IsDirty: false,
            IsSaving: false
        } tab &&
        tab.EntityType is AccountingEntityType.ReceiptVouchers or AccountingEntityType.PaymentVouchers;

    private bool CanApproveActiveVoucher => IsActiveVoucherStatusActionAvailable(ReceiptVoucherStatus.Draft, PaymentVoucherStatus.Draft);

    private bool CanPostActiveVoucher => IsActiveVoucherStatusActionAvailable(ReceiptVoucherStatus.Approved, PaymentVoucherStatus.Approved);

    private bool CanCancelActiveVoucher =>
        IsActiveVoucherStatusActionAvailable(ReceiptVoucherStatus.Draft, PaymentVoucherStatus.Draft) ||
        IsActiveVoucherStatusActionAvailable(ReceiptVoucherStatus.Approved, PaymentVoucherStatus.Approved);

    private bool CanViewGeneratedJournal =>
        Workspace.ActiveTab is { IsListTab: false, IsNew: false, IsDirty: false, IsSaving: false } tab &&
        tab.Model switch
        {
            ReceiptVoucherEditor.FormModel m => m.Status == ReceiptVoucherStatus.Posted && m.JournalEntryId.HasValue,
            PaymentVoucherEditor.FormModel m => m.Status == PaymentVoucherStatus.Posted && m.JournalEntryId.HasValue,
            _ => false
        };

    private bool IsActiveVoucherStatusActionAvailable(
        ReceiptVoucherStatus receiptStatus,
        PaymentVoucherStatus paymentStatus)
    {
        if (Workspace.ActiveTab is not
            {
                IsListTab: false,
                IsNew: false,
                IsEditMode: false,
                IsDirty: false,
                IsLoading: false,
                IsSaving: false
            } tab)
            return false;

        return tab.Model switch
        {
            ReceiptVoucherEditor.FormModel m => m.Status == receiptStatus && !string.IsNullOrWhiteSpace(m.RowVersion),
            PaymentVoucherEditor.FormModel m => m.Status == paymentStatus && !string.IsNullOrWhiteSpace(m.RowVersion),
            _ => false
        };
    }

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

    private Task OpenNewRecord() => OpenNewRecord(CurrentListEntity);

    private async Task OpenNewRecord(AccountingEntityType entityType)
    {
        try
        {
            var tab = Workspace.OpenNewEntityTab(entityType);
            InitializeNewModel(tab);
            await ReserveAutomaticNumberAsync(tab);
            tab.CaptureBaseline(SerializeModel(tab.Model));
            Workspace.NotifyStateChanged();
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
        }
    }

    private async Task ReserveAutomaticNumberAsync(AccountingTabState tab)
    {
        switch (tab.Model)
        {
            case JournalEntryEditor.FormModel model:
                model.JournalNumber = (await AccountingService.ReserveJournalNumberAsync(model.PostingDate))?.Number ?? string.Empty;
                break;
            case ReceiptVoucherEditor.FormModel model:
                model.VoucherNumber = (await AccountingService.ReserveReceiptVoucherNumberAsync(model.VoucherDate))?.Number ?? string.Empty;
                break;
            case PaymentVoucherEditor.FormModel model:
                model.VoucherNumber = (await AccountingService.ReservePaymentVoucherNumberAsync(model.VoucherDate))?.Number ?? string.Empty;
                break;
            case ExpenseEditor.FormModel model:
                model.ExpenseNumber = (await AccountingService.ReserveExpenseNumberAsync(model.ExpenseDate))?.Number ?? string.Empty;
                break;
            case CashShiftEditor.FormModel model:
                model.ShiftNumber = (await AccountingService.ReserveCashShiftNumberAsync())?.Number ?? string.Empty;
                break;
            case CashAccountEditor.FormModel model:
                model.Code = (await AccountingService.ReserveCashAccountCodeAsync())?.CashAccountCode ?? string.Empty;
                break;
            case BankAccountEditor.FormModel model:
                model.Code = (await AccountingService.ReserveBankAccountCodeAsync())?.BankAccountCode ?? string.Empty;
                break;
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
        PageSize = type == AccountingEntityType.Accounts ? PageRequest.MaximumPageSize : PageSize,
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
        AccountingEntityType.ReceiptVouchers => new HashSet<AccountingEntityType> { AccountingEntityType.ReceiptVouchers, AccountingEntityType.PaymentAllocations, AccountingEntityType.Journals },
        AccountingEntityType.PaymentVouchers => new HashSet<AccountingEntityType> { AccountingEntityType.PaymentVouchers, AccountingEntityType.PaymentAllocations, AccountingEntityType.Journals },
        _ => new HashSet<AccountingEntityType> { section }
    };

    private static string GetSectionRoute(AccountingEntityType section) => section switch
    {
        AccountingEntityType.Accounts => "/accounting/accounts",
        AccountingEntityType.FiscalYears => "/accounting/fiscal-years",
        AccountingEntityType.Journals => "/accounting/journals",
        AccountingEntityType.PostingProfiles => "/accounting/posting-profiles",
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
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
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
                await RememberCashAccountLookupsAsync(_cashAccountsPage.Items);
                break;
            case AccountingEntityType.BankAccounts:
                _bankAccountsPage = await AccountingService.GetBankAccountsPageAsync(request);
                await RememberBankAccountLookupsAsync(_bankAccountsPage.Items);
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
                            var customer = await EnsureCustomerLookupAsync(line.CustomerId);
                            var supplier = await EnsureSupplierLookupAsync(line.SupplierId);
                            var employee = await EnsureEmployeeAccountLookupAsync(line.EmployeeId);
                            var currency = await EnsureCurrencyLookupAsync(line.TransactionCurrencyId);
                            lines.Add(new UiJournalLinesEditor.EditableJournalLine
                            {
                                AccountId = line.AccountId,
                                AccountDisplay = account?.PrimaryText ?? line.AccountId.ToString("D"),
                                AccountLookupItem = account,
                                Description = line.Description ?? string.Empty,
                                CostCenterId = line.CostCenterId,
                                CostCenterDisplay = costCenter?.PrimaryText,
                                CostCenterLookupItem = costCenter,
                                DebitAmount = line.TransactionDebitAmount ?? line.DebitAmount,
                                CreditAmount = line.TransactionCreditAmount ?? line.CreditAmount,
                                CustomerId = line.CustomerId,
                                CustomerLookupItem = customer,
                                SupplierId = line.SupplierId,
                                SupplierLookupItem = supplier,
                                EmployeeId = line.EmployeeId,
                                EmployeeLookupItem = employee,
                                ProductVariantId = line.ProductVariantId,
                                WarehouseId = line.WarehouseId,
                                TransactionCurrencyId = line.TransactionCurrencyId,
                                CurrencyCode = line.TransactionCurrencyCodeSnapshot,
                                CurrencyLookupItem = currency,
                                ResolvedExchangeRate = line.ExchangeRateSource == ExchangeRateSource.Manual ? null : line.ExchangeRate,
                                ManualExchangeRate = line.ExchangeRateSource == ExchangeRateSource.Manual ? line.ExchangeRate : null
                            });
                        }

                        var sourceDocumentCode = await ResolveSourceDocumentCodeAsync(
                            dto.SourceDocumentType,
                            dto.SourceDocumentId);

                        var reversedJournalCode = await ResolveJournalNumberAsync(dto.ReversedJournalId);

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
                            SourceDocumentCode = sourceDocumentCode,
                            Status = dto.Status,
                            ReversedJournalId = dto.ReversedJournalId,
                            ReversedJournalCode = reversedJournalCode,
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
                case AccountingEntityType.ReceiptVouchers:
                    {
                        var dto = await AccountingService.GetReceiptVoucherByIdAsync(id) ?? throw NotFound("سند القبض");
                        var lines = await MapReceiptSettlementLinesAsync(dto.Lines);
                        var model = new ReceiptVoucherEditor.FormModel
                        {
                            VoucherNumber = dto.VoucherNumber,
                            VoucherDate = dto.VoucherDate,
                            BaseCurrencyId = dto.BaseCurrencyId,
                            BaseCurrencyCode = dto.BaseCurrencyCodeSnapshot,
                            BaseCurrencyDecimalPlaces = dto.BaseCurrencyDecimalPlacesSnapshot,
                            BaseTotalAmount = dto.BaseTotalAmount,
                            Description = dto.Description ?? string.Empty,
                            Status = dto.Status,
                            JournalEntryId = dto.JournalEntryId,
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
                        var lines = await MapPaymentSettlementLinesAsync(dto.Lines);
                        var model = new PaymentVoucherEditor.FormModel
                        {
                            VoucherNumber = dto.VoucherNumber,
                            VoucherDate = dto.VoucherDate,
                            BaseCurrencyId = dto.BaseCurrencyId,
                            BaseCurrencyCode = dto.BaseCurrencyCodeSnapshot,
                            BaseCurrencyDecimalPlaces = dto.BaseCurrencyDecimalPlacesSnapshot,
                            BaseTotalAmount = dto.BaseTotalAmount,
                            Description = dto.Description ?? string.Empty,
                            Status = dto.Status,
                            JournalEntryId = dto.JournalEntryId,
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
                        var sourceType = dto.ReceiptVoucherLineId.HasValue ? PaymentSourceType.ReceiptVoucher : PaymentSourceType.PaymentVoucher;
                        var sourceLineId = dto.ReceiptVoucherLineId ?? dto.PaymentVoucherLineId;
                        if (sourceLineId.HasValue) await EnsurePaymentSourceLineLookupAsync(sourceType, sourceLineId.Value);
                        var model = new PaymentAllocationEditor.FormModel
                        {
                            PaymentSourceType = sourceType,
                            ReceiptVoucherLineId = dto.ReceiptVoucherLineId,
                            PaymentVoucherLineId = dto.PaymentVoucherLineId,
                            TargetDocumentType = dto.TargetDocumentType,
                            TargetDocumentId = dto.TargetDocumentId,
                            CurrencyId = dto.CurrencyId,
                            CurrencyCodeSnapshot = dto.CurrencyCodeSnapshot,
                            AllocatedAmount = dto.AllocatedAmount,
                            ExchangeRate = dto.ExchangeRate,
                            BaseAllocatedAmount = dto.BaseAllocatedAmount,
                            AllocatedAtUtc = dto.AllocatedAtUtc
                        };
                        CompleteLoadedTab(tab, dto.Id, $"تخصيص {dto.AllocatedAmount:N2} {dto.CurrencyCodeSnapshot}", model);
                        break;
                    }
                case AccountingEntityType.CashAccounts:
                    {
                        var dto = await AccountingService.GetCashAccountByIdAsync(id) ?? throw NotFound("الصندوق");
                        await EnsureAccountLookupAsync(dto.AccountId);
                        await EnsureCurrencyLookupAsync(dto.CurrencyId);
                        await RememberCashAccountLookupsAsync([dto]);
                        var model = new CashAccountEditor.FormModel
                        {
                            Code = dto.Code,
                            Name = dto.Name,
                            AccountId = dto.AccountId,
                            CurrencyId = dto.CurrencyId,
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
                        await EnsureCurrencyLookupAsync(dto.CurrencyId);
                        await RememberBankAccountLookupsAsync([dto]);
                        var model = new BankAccountEditor.FormModel
                        {
                            Code = dto.Code,
                            BankName = dto.BankName,
                            AccountName = dto.AccountName,
                            AccountNumber = dto.AccountNumber,
                            IBAN = dto.IBAN,
                            AccountId = dto.AccountId,
                            CurrencyId = dto.CurrencyId,
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
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch (InvalidOperationException)
        {
            Snackbar.Error("تعذر تحميل السجل المطلوب أو أن نوعه غير مدعوم.");
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
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

    private async Task<List<UiVoucherSettlementLinesEditor.EditableSettlementLine>> MapReceiptSettlementLinesAsync(
        IEnumerable<ReceiptVoucherLineDto> source)
    {
        var result = new List<UiVoucherSettlementLinesEditor.EditableSettlementLine>();
        foreach (var line in source)
        {
            result.Add(await MapSettlementLineAsync(
                line.PartyType, line.CustomerId, line.SupplierId, line.EmployeeId,
                line.PartyNameSnapshot, line.CounterpartyAccountId,
                line.PaymentMethod, line.CashAccountId, line.BankAccountId, line.SettlementAccountId,
                line.CurrencyId, line.CurrencyCodeSnapshot, line.Amount, line.ExchangeRate,
                line.ExchangeRateDate, line.ExchangeRateSource, line.BaseAmount,
                line.ReferenceNumber, line.ReferenceDate, line.ReferenceType, line.ReferenceId,
                line.Description, line.AccountId));
        }
        return result;
    }

    private async Task<List<UiVoucherSettlementLinesEditor.EditableSettlementLine>> MapPaymentSettlementLinesAsync(
        IEnumerable<PaymentVoucherLineDto> source)
    {
        var result = new List<UiVoucherSettlementLinesEditor.EditableSettlementLine>();
        foreach (var line in source)
        {
            result.Add(await MapSettlementLineAsync(
                line.PartyType, line.CustomerId, line.SupplierId, line.EmployeeId,
                line.PartyNameSnapshot, line.CounterpartyAccountId,
                line.PaymentMethod, line.CashAccountId, line.BankAccountId, line.SettlementAccountId,
                line.CurrencyId, line.CurrencyCodeSnapshot, line.Amount, line.ExchangeRate,
                line.ExchangeRateDate, line.ExchangeRateSource, line.BaseAmount,
                line.ReferenceNumber, line.ReferenceDate, line.ReferenceType, line.ReferenceId,
                line.Description, line.AccountId));
        }
        return result;
    }

    private async Task<UiVoucherSettlementLinesEditor.EditableSettlementLine> MapSettlementLineAsync(
        SettlementPartyType? partyType, Guid? customerId, Guid? supplierId, Guid? employeeId,
        string? partyName, Guid? counterpartyAccountId, PaymentMethod? paymentMethod,
        Guid? cashAccountId, Guid? bankAccountId, Guid? settlementAccountId,
        Guid? currencyId, string? currencyCode, decimal amount, decimal? exchangeRate,
        DateOnly? exchangeRateDate, ExchangeRateSource? exchangeRateSource, decimal? baseAmount,
        string? referenceNumber, DateOnly? referenceDate, string? referenceType, Guid? referenceId,
        string? description, Guid legacyAccountId)
    {
        UiLookupItem? partyLookup = null;
        if (customerId.HasValue) partyLookup = await EnsureCustomerLookupAsync(customerId);
        else if (supplierId.HasValue) partyLookup = await EnsureSupplierLookupAsync(supplierId);
        else if (employeeId.HasValue) partyLookup = await EnsureEmployeeAccountLookupAsync(employeeId);

        var counterpartyLookup = await EnsureAccountLookupAsync(counterpartyAccountId ?? (partyType is null ? legacyAccountId : null));
        UiLookupItem? settlementLookup = null;
        if (cashAccountId.HasValue) settlementLookup = await EnsureCashAccountLookupAsync(cashAccountId);
        else if (bankAccountId.HasValue) settlementLookup = await EnsureBankAccountLookupAsync(bankAccountId);
        else if (settlementAccountId.HasValue) settlementLookup = await EnsureAccountLookupAsync(settlementAccountId);

        var currencyLookup = await EnsureCurrencyLookupAsync(currencyId);
        var selection = AccountingVoucherSettlementSelectionRules.Normalize(
            paymentMethod, cashAccountId, bankAccountId, settlementAccountId);

        return new UiVoucherSettlementLinesEditor.EditableSettlementLine
        {
            PartyType = (UiSettlementPartyKind)(byte)(partyType ?? SettlementPartyType.Other),
            CustomerId = customerId,
            SupplierId = supplierId,
            EmployeeId = employeeId,
            PartyName = partyName ?? (partyType is null ? "سطر تاريخي" : null),
            CounterpartyAccountId = counterpartyAccountId ?? (partyType is null ? legacyAccountId : null),
            PaymentMethod = (UiPaymentMethodKind)(byte)(paymentMethod ?? PaymentMethod.Other),
            CashAccountId = selection.CashAccountId,
            BankAccountId = selection.BankAccountId,
            SettlementAccountId = selection.SettlementAccountId,
            CurrencyId = currencyId,
            CurrencyCodeSnapshot = currencyCode,
            Amount = amount,
            ResolvedExchangeRate = exchangeRateSource == ExchangeRateSource.Manual ? null : exchangeRate,
            ManualExchangeRate = exchangeRateSource == ExchangeRateSource.Manual ? exchangeRate : null,
            ExchangeRateDate = exchangeRateDate,
            ExchangeRateSource = exchangeRateSource.HasValue ? (UiExchangeRateSourceKind?)(byte)exchangeRateSource.Value : null,
            BaseAmount = baseAmount,
            ReferenceNumber = referenceNumber,
            ReferenceDate = referenceDate,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = description,
            PartyLookupItem = partyLookup,
            CounterpartyAccountLookupItem = counterpartyLookup,
            SettlementLookupItem = settlementLookup,
            CurrencyLookupItem = currencyLookup
        };
    }

    private async Task SaveActiveAsync()
    {
        if (Workspace.ActiveTab is not { IsListTab: false } tab || tab.Model is null || !CanSaveActive)
            return;

        tab.IsSaving = true;
        Workspace.NotifyStateChanged();

        try
        {
            if (!await ValidateUniqueFieldsBeforeSaveAsync(tab))
                return;

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
            // القيود الفريدة تُفحص قبل الحفظ، وهذا الفرع مجرد حماية احتياطية
            // لسباق نادر بين عمليتي حفظ متزامنتين.
            if (!TryApplyUniqueConflictToModel(tab, ex.Error.Code))
                ShowAccountingError(ex.Error);
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
        var totalDebit = validLines.Sum(x => x.BaseDebitAmount);
        var totalCredit = validLines.Sum(x => x.BaseCreditAmount);
        if (totalDebit != totalCredit)
        {
            Snackbar.Warning($"القيد غير متزن. المدين {totalDebit:N2} والدائن {totalCredit:N2}.");
            return;
        }

        for (var i = 0; i < validLines.Length; i++)
        {
            if (!await ValidateAccountSelectionAsync(
                    validLines[i].AccountId!.Value,
                    AccountingAccountEligibilityContext.ManualJournal,
                    $"سطر القيد {i + 1}"))
                return;
        }

        var lines = validLines.Select(x => new CreateJournalEntryLineRequest(
            x.AccountId!.Value,
            x.DebitAmount,
            x.CreditAmount,
            x.TransactionCurrencyId,
            x.ManualExchangeRate,
            ExchangeRateType.Accounting,
            NullIfBlank(x.Description),
            x.CustomerId,
            x.SupplierId,
            x.EmployeeId,
            x.CostCenterId,
            x.ProductVariantId,
            x.WarehouseId)).ToArray();

        JournalEntryDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateJournalAsync(new CreateJournalEntryRequest(
                model.JournalType, model.PostingDate, model.DocumentDate, model.FiscalPeriodId.Value,
                model.Description.Trim(), model.SourceModule, model.SourceDocumentType, model.SourceDocumentId, lines, model.JournalNumber));
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

        for (var i = 0; i < model.Lines.Count; i++)
        {
            var line = model.Lines[i];
            if (!line.AccountId.HasValue) continue;
            if (!await ValidateAccountSelectionAsync(
                    line.AccountId.Value,
                    AccountingAccountEligibilityContext.PostingProfile,
                    $"دور الترحيل {line.AccountRole}"))
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

    private async Task SaveReceiptVoucherAsync(AccountingTabState tab, ReceiptVoucherEditor.FormModel model)
    {
        if (!ValidateSettlementLines(model.Lines)) return;
        if (!await ValidateSettlementAccountEligibilityAsync(model.Lines)) return;

        var lines = model.Lines.Select(x =>
        {
            var paymentMethod = (PaymentMethod)(byte)x.PaymentMethod;
            var settlement = AccountingVoucherSettlementSelectionRules.Normalize(
                paymentMethod, x.CashAccountId, x.BankAccountId, x.SettlementAccountId);

            return new CreateReceiptVoucherLineRequest(
                (SettlementPartyType)(byte)x.PartyType, x.CustomerId, x.SupplierId, x.EmployeeId,
                NullIfBlank(x.PartyName), x.CounterpartyAccountId, paymentMethod,
                settlement.CashAccountId, settlement.BankAccountId, settlement.SettlementAccountId,
                x.CurrencyId!.Value, x.Amount, x.ManualExchangeRate, ExchangeRateType.Accounting,
                NullIfBlank(x.ReferenceNumber), x.ReferenceDate, NullIfBlank(x.ReferenceType), x.ReferenceId,
                NullIfBlank(x.Description));
        }).ToArray();

        ReceiptVoucherDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateReceiptVoucherAsync(new CreateReceiptVoucherRequest(
                model.VoucherDate, NullIfBlank(model.Description), lines, model.VoucherNumber));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateReceiptVoucherAsync(tab.EntityId!.Value, new UpdateReceiptVoucherRequest(
                model.VoucherDate, NullIfBlank(model.Description), lines, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result?.VoucherNumber, "تم حفظ سند القبض بنجاح.");
    }

    private async Task SavePaymentVoucherAsync(AccountingTabState tab, PaymentVoucherEditor.FormModel model)
    {
        if (!ValidateSettlementLines(model.Lines)) return;
        if (!await ValidateSettlementAccountEligibilityAsync(model.Lines)) return;

        var lines = model.Lines.Select(x =>
        {
            var paymentMethod = (PaymentMethod)(byte)x.PaymentMethod;
            var settlement = AccountingVoucherSettlementSelectionRules.Normalize(
                paymentMethod, x.CashAccountId, x.BankAccountId, x.SettlementAccountId);

            return new CreatePaymentVoucherLineRequest(
                (SettlementPartyType)(byte)x.PartyType, x.CustomerId, x.SupplierId, x.EmployeeId,
                NullIfBlank(x.PartyName), x.CounterpartyAccountId, paymentMethod,
                settlement.CashAccountId, settlement.BankAccountId, settlement.SettlementAccountId,
                x.CurrencyId!.Value, x.Amount, x.ManualExchangeRate, ExchangeRateType.Accounting,
                NullIfBlank(x.ReferenceNumber), x.ReferenceDate, NullIfBlank(x.ReferenceType), x.ReferenceId,
                NullIfBlank(x.Description));
        }).ToArray();

        PaymentVoucherDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreatePaymentVoucherAsync(new CreatePaymentVoucherRequest(
                model.VoucherDate, NullIfBlank(model.Description), lines, model.VoucherNumber));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdatePaymentVoucherAsync(tab.EntityId!.Value, new UpdatePaymentVoucherRequest(
                model.VoucherDate, NullIfBlank(model.Description), lines, model.RowVersion));
        }

        await CompleteSaveAsync(tab, result?.Id, result?.VoucherNumber, "تم حفظ سند الصرف بنجاح.");
    }

    private bool ValidateSettlementLines(IReadOnlyList<UiVoucherSettlementLinesEditor.EditableSettlementLine> lines)
    {
        if (lines.Count == 0) { Snackbar.Warning("يجب إضافة سطر تسوية واحد على الأقل."); return false; }
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i]; var number = i + 1;
            if (line.Amount <= 0 || !line.CurrencyId.HasValue) { Snackbar.Warning($"السطر {number}: العملة والمبلغ الأكبر من صفر مطلوبان."); return false; }
            var invalidParty =
                (line.PartyType == UiSettlementPartyKind.Customer && !line.CustomerId.HasValue) ||
                (line.PartyType == UiSettlementPartyKind.Supplier && !line.SupplierId.HasValue) ||
                (line.PartyType == UiSettlementPartyKind.Employee && !line.EmployeeId.HasValue) ||
                (line.PartyType == UiSettlementPartyKind.Other && (string.IsNullOrWhiteSpace(line.PartyName) || !line.CounterpartyAccountId.HasValue));
            if (invalidParty) { Snackbar.Warning($"السطر {number}: أكمل بيانات الطرف والحساب المقابل."); return false; }
            var invalidSettlement =
                (line.PaymentMethod == UiPaymentMethodKind.Cash && !line.CashAccountId.HasValue) ||
                (line.PaymentMethod is UiPaymentMethodKind.BankTransfer or UiPaymentMethodKind.Cheque or UiPaymentMethodKind.Card && !line.BankAccountId.HasValue) ||
                (line.PaymentMethod == UiPaymentMethodKind.Other && !line.SettlementAccountId.HasValue);
            if (invalidSettlement) { Snackbar.Warning($"السطر {number}: اختر حساب التسوية الموافق لطريقة الدفع."); return false; }
            if (line.PaymentMethod == UiPaymentMethodKind.Cheque && string.IsNullOrWhiteSpace(line.ReferenceNumber)) { Snackbar.Warning($"السطر {number}: رقم الشيك مطلوب."); return false; }
        }
        return true;
    }

    private async Task<bool> ValidateSettlementAccountEligibilityAsync(
        IReadOnlyList<UiVoucherSettlementLinesEditor.EditableSettlementLine> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var label = $"السطر {i + 1}";

            if (line.PartyType == UiSettlementPartyKind.Other && line.CounterpartyAccountId.HasValue &&
                !await ValidateAccountSelectionAsync(
                    line.CounterpartyAccountId.Value,
                    AccountingAccountEligibilityContext.OtherCounterparty,
                    $"{label} - الحساب المقابل"))
                return false;

            switch (line.PaymentMethod)
            {
                case UiPaymentMethodKind.Cash when line.CashAccountId.HasValue:
                    if (!await ValidateCashAccountSelectionAsync(
                            line.CashAccountId.Value,
                            line.CurrencyId,
                            $"{label} - الصندوق"))
                        return false;
                    break;

                case UiPaymentMethodKind.BankTransfer or UiPaymentMethodKind.Cheque or UiPaymentMethodKind.Card
                    when line.BankAccountId.HasValue:
                    if (!await ValidateBankAccountSelectionAsync(
                            line.BankAccountId.Value,
                            line.CurrencyId,
                            $"{label} - الحساب البنكي"))
                        return false;
                    break;

                case UiPaymentMethodKind.Other when line.SettlementAccountId.HasValue:
                    if (!await ValidateAccountSelectionAsync(
                            line.SettlementAccountId.Value,
                            AccountingAccountEligibilityContext.OtherSettlement,
                            $"{label} - حساب التسوية"))
                        return false;
                    break;
            }
        }

        return true;
    }

    private async Task<bool> ValidateAccountSelectionAsync(
        Guid accountId,
        AccountingAccountEligibilityContext context,
        string fieldLabel)
    {
        var account = await AccountingService.GetAccountByIdAsync(accountId);
        if (account is null)
        {
            Snackbar.Warning($"{fieldLabel}: الحساب المحدد لم يعد موجوداً.");
            return false;
        }

        _accountDtos[account.Id] = account;
        _accountLookups[account.Id] = ToAccountLookup(account);

        var eligibility = AccountingAccountEligibility.Evaluate(account, context);
        if (eligibility.IsEligible)
            return true;

        Snackbar.Warning($"{fieldLabel}: الحساب {account.Code} - {account.NameAr} غير صالح لهذه العملية. {eligibility.Reason}");
        return false;
    }

    private async Task<bool> ValidateCashAccountSelectionAsync(
        Guid cashAccountId,
        Guid? requiredCurrencyId,
        string fieldLabel)
    {
        var cash = await AccountingService.GetCashAccountByIdAsync(cashAccountId);
        if (cash is null)
        {
            Snackbar.Warning($"{fieldLabel}: الصندوق المحدد لم يعد موجوداً.");
            return false;
        }

        var lookup = await BuildCashAccountLookupAsync(cash, requiredCurrencyId, CancellationToken.None);
        if (!lookup.Disabled)
        {
            _cashAccountLookups[cash.Id] = lookup;
            return true;
        }

        Snackbar.Warning($"{fieldLabel}: {lookup.SecondaryText ?? "الصندوق غير صالح لهذه العملية."}");
        return false;
    }

    private async Task<bool> ValidateBankAccountSelectionAsync(
        Guid bankAccountId,
        Guid? requiredCurrencyId,
        string fieldLabel)
    {
        var bank = await AccountingService.GetBankAccountByIdAsync(bankAccountId);
        if (bank is null)
        {
            Snackbar.Warning($"{fieldLabel}: الحساب البنكي المحدد لم يعد موجوداً.");
            return false;
        }

        var lookup = await BuildBankAccountLookupAsync(bank, requiredCurrencyId, CancellationToken.None);
        if (!lookup.Disabled)
        {
            _bankAccountLookups[bank.Id] = lookup;
            return true;
        }

        Snackbar.Warning($"{fieldLabel}: {lookup.SecondaryText ?? "الحساب البنكي غير صالح لهذه العملية."}");
        return false;
    }

    private async Task SavePaymentAllocationAsync(AccountingTabState tab, PaymentAllocationEditor.FormModel model)
    {
        if (!model.SourceLineId.HasValue || !model.TargetDocumentId.HasValue || model.AllocatedAmount <= 0)
        {
            Snackbar.Warning("سطر السداد والمستند الهدف ومبلغ التخصيص مطلوبة.");
            return;
        }

        PaymentAllocationDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreatePaymentAllocationAsync(new CreatePaymentAllocationRequest(
                model.PaymentSourceType == PaymentSourceType.ReceiptVoucher ? model.SourceLineId : null,
                model.PaymentSourceType == PaymentSourceType.PaymentVoucher ? model.SourceLineId : null,
                model.TargetDocumentType, model.TargetDocumentId.Value, model.AllocatedAmount));
        }
        else
        {
            if (!tab.EntityId.HasValue) throw new InvalidOperationException("معرف التخصيص غير متوفر.");
            result = await AccountingService.UpdatePaymentAllocationAsync(tab.EntityId.Value, new UpdatePaymentAllocationRequest(model.AllocatedAmount));
        }

        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"تخصيص {result.AllocatedAmount:N2} {result.CurrencyCodeSnapshot}", "تم حفظ تخصيص السداد بنجاح.");
    }

    private async Task SaveCashAccountAsync(AccountingTabState tab, CashAccountEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || !model.CurrencyId.HasValue)
        {
            Snackbar.Warning("اسم الصندوق وعملته مطلوبان.");
            return;
        }

        CashAccountDto? result;
        if (tab.IsNew)
        {
            var settings = await AccountingService.GetAccountingSettingsAsync();
            if (settings?.CashParentAccountId is null)
            {
                Snackbar.Warning("يجب تحديد الحساب الرئيسي للصناديق في إعدادات المحاسبة أولاً.");
                return;
            }

            if (!await ValidateAccountSelectionAsync(
                    settings.CashParentAccountId.Value,
                    AccountingAccountEligibilityContext.AssetControlParent,
                    "الحساب الرئيسي للصناديق"))
            {
                return;
            }

            result = await AccountingService.CreateCashAccountAsync(new CreateCashAccountRequest(model.Code.Trim(), model.Name.Trim(), model.CurrencyId.Value, model.IsDefault, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateCashAccountAsync(tab.EntityId!.Value, new UpdateCashAccountRequest(model.Name.Trim(), model.CurrencyId.Value, model.IsDefault, model.IsActive, model.RowVersion));
        }
        await CompleteSaveAsync(tab, result?.Id, result is null ? null : $"{result.Code} - {result.Name}", "تم حفظ الصندوق بنجاح.");
    }

    private async Task SaveBankAccountAsync(AccountingTabState tab, BankAccountEditor.FormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.BankName) ||
            string.IsNullOrWhiteSpace(model.AccountName) || string.IsNullOrWhiteSpace(model.AccountNumber) || !model.CurrencyId.HasValue)
        {
            Snackbar.Warning("أكمل بيانات الحساب البنكي وحدد العملة.");
            return;
        }

        BankAccountDto? result;
        if (tab.IsNew)
        {
            var settings = await AccountingService.GetAccountingSettingsAsync();
            if (settings?.BankParentAccountId is null)
            {
                Snackbar.Warning("يجب تحديد الحساب الرئيسي للبنوك في إعدادات المحاسبة أولاً.");
                return;
            }

            if (!await ValidateAccountSelectionAsync(
                    settings.BankParentAccountId.Value,
                    AccountingAccountEligibilityContext.AssetControlParent,
                    "الحساب الرئيسي للبنوك"))
            {
                return;
            }

            result = await AccountingService.CreateBankAccountAsync(new CreateBankAccountRequest(model.Code.Trim(), model.BankName.Trim(), model.AccountName.Trim(), model.AccountNumber.Trim(), NullIfBlank(model.IBAN), model.CurrencyId.Value, model.IsActive));
        }
        else
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            result = await AccountingService.UpdateBankAccountAsync(tab.EntityId!.Value, new UpdateBankAccountRequest(model.BankName.Trim(), model.AccountName.Trim(), model.AccountNumber.Trim(), NullIfBlank(model.IBAN), model.CurrencyId.Value, model.IsActive, model.RowVersion));
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
        if (!await ValidateCashAccountSelectionAsync(model.CashAccountId.Value, null, "الصندوق"))
            return;

        var result = await AccountingService.CreateCashShiftAsync(new CreateCashShiftRequest(
            model.CashAccountId.Value, model.OpeningBalance, model.ShiftNumber));
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
        if (!await ValidateAccountSelectionAsync(
                model.ExpenseAccountId.Value,
                AccountingAccountEligibilityContext.ExpenseAccount,
                "حساب المصروف"))
            return;
        if (model.PaymentMethod == PaymentMethod.Cash && model.CashAccountId.HasValue &&
            !await ValidateCashAccountSelectionAsync(model.CashAccountId.Value, null, "صندوق المصروف"))
            return;
        if (model.PaymentMethod != PaymentMethod.Cash && model.BankAccountId.HasValue &&
            !await ValidateBankAccountSelectionAsync(model.BankAccountId.Value, null, "الحساب البنكي للمصروف"))
            return;

        ExpenseDto? result;
        if (tab.IsNew)
        {
            result = await AccountingService.CreateExpenseAsync(new CreateExpenseRequest(
                model.ExpenseDate, model.ExpenseTypeId.Value, model.ExpenseAccountId.Value,
                NullIfBlank(model.Beneficiary), model.Amount, model.PaymentMethod,
                model.CashAccountId, model.BankAccountId, NullIfBlank(model.Description), model.ExpenseNumber));
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

        if (model.DefaultExpenseAccountId.HasValue &&
            !await ValidateAccountSelectionAsync(
                model.DefaultExpenseAccountId.Value,
                AccountingAccountEligibilityContext.ExpenseAccount,
                "الحساب الافتراضي للمصروف"))
            return;

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

        if (status == JournalEntryStatus.Posted &&
            !await ValidateSelectedFiscalPeriodForPostingAsync(model.FiscalPeriodId, model.PostingDate))
        {
            return;
        }

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
        catch (ApiClientException ex) { ShowAccountingError(ex.Error); }
        catch (Exception) { ApiFeedback.ShowUnexpected(); }
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
        catch (ApiClientException ex) { ShowAccountingError(ex.Error); }
        catch (Exception) { ApiFeedback.ShowUnexpected(); }
    }

    private async Task PrintActiveVoucherAsync()
    {
        var tab = Workspace.ActiveTab;
        if (tab?.EntityId is not Guid documentId || tab.IsNew)
        {
            Snackbar.Warning("احفظ السند أولاً قبل الطباعة.");
            return;
        }

        if (tab.EntityType is not (AccountingEntityType.ReceiptVouchers or AccountingEntityType.PaymentVouchers))
            return;

        if (tab.IsDirty)
        {
            Snackbar.Warning("احفظ التعديلات قبل إرسال السند للطباعة.");
            return;
        }

        _isPrinting = true;
        Workspace.NotifyStateChanged();

        try
        {
            var queued = tab.EntityType == AccountingEntityType.ReceiptVouchers
                ? await PrintingService.PrintReceiptVoucherAsync(documentId)
                : await PrintingService.PrintPaymentVoucherAsync(documentId);

            if (queued is null)
            {
                Snackbar.Error("تعذر إرسال مهمة الطباعة إلى خدمة الطباعة.");
                return;
            }

            Snackbar.Success("تم إرسال السند إلى OAS Print للطباعة.");
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _isPrinting = false;
            Workspace.NotifyStateChanged();
        }
    }

    private async Task ChangeReceiptStatusAsync(ReceiptVoucherStatus status)
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: ReceiptVoucherEditor.FormModel model } tab)
            return;

        if (!ValidateVoucherStatusAction(tab, model.RowVersion))
            return;

        if (!await ConfirmVoucherStatusActionAsync("سند القبض", status.ToString()))
            return;

        tab.IsSaving = true;
        Workspace.NotifyStateChanged();

        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.SetReceiptVoucherStatusAsync(
                id,
                new SetReceiptVoucherStatusRequest(status, model.RowVersion));

            if (result is null)
                throw new InvalidOperationException("لم يُرجع الخادم سند القبض المحدث.");

            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.ReceiptVouchers);

            Snackbar.Success(status switch
            {
                ReceiptVoucherStatus.Approved => "تم اعتماد سند القبض بنجاح.",
                ReceiptVoucherStatus.Posted => "تم ترحيل سند القبض وإنشاء القيد المحاسبي بنجاح.",
                ReceiptVoucherStatus.Cancelled => "تم إلغاء سند القبض بنجاح.",
                _ => "تم تحديث سند القبض بنجاح."
            });
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
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

    private async Task ChangePaymentStatusAsync(PaymentVoucherStatus status)
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: PaymentVoucherEditor.FormModel model } tab)
            return;

        if (!ValidateVoucherStatusAction(tab, model.RowVersion))
            return;

        if (!await ConfirmVoucherStatusActionAsync("سند الصرف", status.ToString()))
            return;

        tab.IsSaving = true;
        Workspace.NotifyStateChanged();

        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.SetPaymentVoucherStatusAsync(
                id,
                new SetPaymentVoucherStatusRequest(status, model.RowVersion));

            if (result is null)
                throw new InvalidOperationException("لم يُرجع الخادم سند الصرف المحدث.");

            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.PaymentVouchers);

            Snackbar.Success(status switch
            {
                PaymentVoucherStatus.Approved => "تم اعتماد سند الصرف بنجاح.",
                PaymentVoucherStatus.Posted => "تم ترحيل سند الصرف وإنشاء القيد المحاسبي بنجاح.",
                PaymentVoucherStatus.Cancelled => "تم إلغاء سند الصرف بنجاح.",
                _ => "تم تحديث سند الصرف بنجاح."
            });
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
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

    private bool ValidateVoucherStatusAction(AccountingTabState tab, string rowVersion)
    {
        if (tab.IsNew || !tab.EntityId.HasValue || string.IsNullOrWhiteSpace(rowVersion))
        {
            Snackbar.Warning("يجب حفظ السند أولاً قبل تنفيذ هذا الإجراء.");
            return false;
        }

        if (tab.IsDirty || tab.IsEditMode)
        {
            Snackbar.Warning("احفظ التعديلات قبل تنفيذ هذا الإجراء.");
            return false;
        }

        return true;
    }

    private async Task<bool> ConfirmVoucherStatusActionAsync(string voucherName, string status)
    {
        if (string.Equals(status, nameof(ReceiptVoucherStatus.Posted), StringComparison.Ordinal))
        {
            var voucherDate = Workspace.ActiveTab?.Model switch
            {
                ReceiptVoucherEditor.FormModel receipt => receipt.VoucherDate,
                PaymentVoucherEditor.FormModel payment => payment.VoucherDate,
                _ => (DateOnly?)null
            };

            if (!voucherDate.HasValue)
            {
                Snackbar.Warning("تعذر تحديد تاريخ السند قبل الترحيل.");
                return false;
            }

            // فحص متوقع في الواجهة قبل استدعاء الترحيل.
            // يبقى تحقق الـBackend موجوداً كحماية نهائية ضد التزامن أو الطلبات الخارجية.
            if (!await ValidateFiscalPeriodForPostingAsync(voucherDate.Value))
                return false;

            return await Dialog.ConfirmAsync(
                "تأكيد ترحيل السند",
                "سيتم ترحيل السند وإنشاء قيد محاسبي رسمي.\nبعد الترحيل لن يمكن تعديل السند مباشرة.\nهل تريد المتابعة؟",
                AlertTone.Warning,
                confirmText: "ترحيل",
                cancelText: "رجوع");
        }

        if (string.Equals(status, nameof(ReceiptVoucherStatus.Cancelled), StringComparison.Ordinal))
        {
            return await Dialog.ConfirmAsync(
                "تأكيد إلغاء السند",
                $"سيتم إلغاء {voucherName} ولن يكون قابلاً للاعتماد أو الترحيل بعد ذلك. هل تريد المتابعة؟",
                AlertTone.Danger,
                confirmText: "إلغاء السند",
                cancelText: "رجوع");
        }

        return true;
    }

    private async Task<bool> ValidateFiscalPeriodForPostingAsync(DateOnly postingDate)
    {
        try
        {
            var matches = new List<FiscalPeriodDto>();
            var pageNumber = 1;

            while (true)
            {
                var page = await AccountingService.GetFiscalPeriodsPageAsync(
                    new PageRequest
                    {
                        PageNumber = pageNumber,
                        PageSize = PageRequest.MaximumPageSize,
                        SortBy = "StartDate",
                        SortDirection = SortDirection.Ascending
                    });

                matches.AddRange(page.Items.Where(x =>
                    x.StartDate <= postingDate &&
                    x.EndDate >= postingDate));

                if (matches.Count > 1 || !page.HasNextPage)
                    break;

                pageNumber++;
            }

            if (matches.Count == 0)
            {
                Snackbar.Warning(
                    $"لا توجد فترة مالية تغطي تاريخ السند {postingDate:yyyy-MM-dd}. أنشئ الفترة المالية المناسبة أو غيّر تاريخ السند قبل الترحيل.");
                return false;
            }

            if (matches.Count > 1)
            {
                Snackbar.Warning(
                    $"يوجد أكثر من فترة مالية تغطي تاريخ السند {postingDate:yyyy-MM-dd}. راجع إعداد الفترات المالية قبل الترحيل.");
                return false;
            }

            var period = matches[0];

            if (period.Status == FiscalPeriodStatus.Closed || period.AccountingLocked)
            {
                Snackbar.Warning(
                    $"الفترة المالية «{period.Name}» لا تسمح بالترحيل المحاسبي. افتح الفترة أو أزل قفل المحاسبة أولاً.");
                return false;
            }

            return true;
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
            return false;
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
            return false;
        }
    }

    private async Task<bool> ValidateSelectedFiscalPeriodForPostingAsync(
        Guid? fiscalPeriodId,
        DateOnly postingDate)
    {
        if (!fiscalPeriodId.HasValue)
        {
            Snackbar.Warning("يجب اختيار الفترة المالية قبل ترحيل القيد.");
            return false;
        }

        try
        {
            var period = await AccountingService.GetFiscalPeriodByIdAsync(fiscalPeriodId.Value);

            if (period is null)
            {
                Snackbar.Warning("الفترة المالية المحددة لم تعد موجودة. اختر فترة مالية صحيحة قبل الترحيل.");
                return false;
            }

            if (postingDate < period.StartDate || postingDate > period.EndDate)
            {
                Snackbar.Warning(
                    $"تاريخ الترحيل {postingDate:yyyy-MM-dd} لا يقع داخل الفترة المالية «{period.Name}».");
                return false;
            }

            if (period.Status == FiscalPeriodStatus.Closed || period.AccountingLocked)
            {
                Snackbar.Warning(
                    $"الفترة المالية «{period.Name}» لا تسمح بالترحيل المحاسبي. افتح الفترة أو أزل قفل المحاسبة أولاً.");
                return false;
            }

            return true;
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
            return false;
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
            return false;
        }
    }

    private async Task<string?> ResolveJournalNumberAsync(Guid? journalId)
    {
        if (!journalId.HasValue)
            return null;

        try
        {
            return (await AccountingService.GetJournalByIdAsync(journalId.Value))?.JournalNumber;
        }
        catch
        {
            // المرجع للعرض فقط؛ لا نمنع فتح القيد إذا تعذر تحميل الرقم.
            return null;
        }
    }

    private async Task<string?> ResolveSourceDocumentCodeAsync(
        string? sourceDocumentType,
        Guid? sourceDocumentId)
    {
        if (!sourceDocumentId.HasValue || string.IsNullOrWhiteSpace(sourceDocumentType))
            return null;

        try
        {
            return sourceDocumentType.Trim() switch
            {
                "Expense" =>
                    (await AccountingService.GetExpenseByIdAsync(sourceDocumentId.Value))?.ExpenseNumber,

                "ReceiptVoucher" =>
                    (await AccountingService.GetReceiptVoucherByIdAsync(sourceDocumentId.Value))?.VoucherNumber,

                "PaymentVoucher" =>
                    (await AccountingService.GetPaymentVoucherByIdAsync(sourceDocumentId.Value))?.VoucherNumber,

                "JournalEntryReversal" =>
                    (await AccountingService.GetJournalByIdAsync(sourceDocumentId.Value))?.JournalNumber,

                _ => null
            };
        }
        catch (ApiClientException)
        {
            // المرجع للعرض فقط؛ فشل تحميله لا يمنع فتح القيد.
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task ViewGeneratedJournalAsync()
    {
        Guid? journalId = Workspace.ActiveTab?.Model switch
        {
            ReceiptVoucherEditor.FormModel m when m.Status == ReceiptVoucherStatus.Posted => m.JournalEntryId,
            PaymentVoucherEditor.FormModel m when m.Status == PaymentVoucherStatus.Posted => m.JournalEntryId,
            _ => null
        };

        if (!journalId.HasValue)
        {
            Snackbar.Warning("لا يوجد قيد محاسبي مرتبط بهذا السند.");
            return;
        }

        await OpenRecordInTab(AccountingEntityType.Journals, journalId.Value, "القيد المحاسبي");
    }

    private async Task ChangeExpenseStatusAsync(ExpenseStatus status)
    {
        if (Workspace.ActiveTab is not { EntityId: Guid id, Model: ExpenseEditor.FormModel model } tab) return;

        if (status == ExpenseStatus.Posted &&
            !await ValidateFiscalPeriodForPostingAsync(model.ExpenseDate))
        {
            return;
        }

        try
        {
            EnsureEntityIdAndRowVersion(tab, model.RowVersion);
            var result = await AccountingService.SetExpenseStatusAsync(id, new SetExpenseStatusRequest(status, model.RowVersion));
            if (result is null) throw new InvalidOperationException("لم يُرجع الخادم المصروف المحدث.");
            await LoadExistingRecordAsync(tab, result.Id);
            await LoadEntityListAsync(AccountingEntityType.Expenses);
            Snackbar.Success(status == ExpenseStatus.Posted ? "تم ترحيل المصروف وربطه بالقيد المحاسبي." : "تم اعتماد المصروف.");
        }
        catch (ApiClientException ex) { ShowAccountingError(ex.Error); }
        catch (Exception) { ApiFeedback.ShowUnexpected(); }
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
        catch (ApiClientException ex) { ShowAccountingError(ex.Error); }
        catch (Exception) { ApiFeedback.ShowUnexpected(); }
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
        catch (ApiClientException ex) { ShowAccountingError(ex.Error); }
        catch (Exception) { ApiFeedback.ShowUnexpected(); }
    }

    // Lookup providers. All accounting-owned lookups query the API; no UI mock data is generated.
    private Task<IReadOnlyList<UiLookupItem>> SearchAccountsAsync(string text, CancellationToken cancellationToken) =>
        SearchAccountsAsync(text, AccountingAccountEligibilityContext.DisplayOnly, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchManualJournalAccountsAsync(string text, CancellationToken cancellationToken) =>
        SearchAccountsAsync(text, AccountingAccountEligibilityContext.ManualJournal, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchPostingProfileAccountsAsync(string text, CancellationToken cancellationToken) =>
        SearchAccountsAsync(text, AccountingAccountEligibilityContext.PostingProfile, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchExpenseAccountsAsync(string text, CancellationToken cancellationToken) =>
        SearchAccountsAsync(text, AccountingAccountEligibilityContext.ExpenseAccount, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchOtherCounterpartyAccountsAsync(string text, CancellationToken cancellationToken) =>
        SearchAccountsAsync(text, AccountingAccountEligibilityContext.OtherCounterparty, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchOtherSettlementAccountsAsync(string text, CancellationToken cancellationToken) =>
        SearchAccountsAsync(text, AccountingAccountEligibilityContext.OtherSettlement, cancellationToken);

    private async Task<IReadOnlyList<UiLookupItem>> SearchAccountsAsync(
        string text,
        AccountingAccountEligibilityContext context,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        var accounts = new List<AccountDto>();
        var pageNumber = 1;

        while (true)
        {
            var page = await AccountingService.GetAccountsPageAsync(
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

            // Empty lookup queries intentionally stop at one maximum-sized page.
            // Once the user types, all server-side matches are paged so no valid match
            // is hidden merely because it is beyond PageRequest.MaximumPageSize.
            if (normalizedSearch is null || !page.HasNextPage)
                break;

            pageNumber++;
        }

        RememberAccountLookups(accounts);
        return accounts
            .Select(x => AccountingLookupEligibility.ToAccountLookup(x, context))
            .ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCostCentersAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetCostCentersPageAsync(new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize, Search = text }, cancellationToken);
        RememberCostCenterLookups(result.Items);
        return result.Items.Select(ToCostCenterLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchFiscalYearsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetFiscalYearsPageAsync(new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize, Search = text }, cancellationToken);
        RememberFiscalYearLookups(result.Items);
        return result.Items.Select(ToFiscalYearLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchFiscalPeriodsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetFiscalPeriodsPageAsync(new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize, Search = text }, cancellationToken);
        RememberFiscalPeriodLookups(result.Items);
        return result.Items.Select(ToFiscalPeriodLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCashAccountsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetCashAccountsPageAsync(
            new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize, Search = text },
            cancellationToken);

        var lookups = await Task.WhenAll(result.Items.Select(x => BuildCashAccountLookupAsync(x, null, cancellationToken)));
        for (var i = 0; i < result.Items.Count; i++)
            _cashAccountLookups[result.Items[i].Id] = lookups[i];
        return lookups;
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchBankAccountsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetBankAccountsPageAsync(
            new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize, Search = text },
            cancellationToken);

        var lookups = await Task.WhenAll(result.Items.Select(x => BuildBankAccountLookupAsync(x, null, cancellationToken)));
        for (var i = 0; i < result.Items.Count; i++)
            _bankAccountLookups[result.Items[i].Id] = lookups[i];
        return lookups;
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCashAccountsByCurrencyAsync(
        Guid? currencyId,
        string text,
        CancellationToken cancellationToken)
    {
        if (!currencyId.HasValue)
            return Array.Empty<UiLookupItem>();

        var result = await AccountingService.GetCashAccountsPageAsync(
            new PageRequest
            {
                PageNumber = 1,
                PageSize = PageRequest.MaximumPageSize,
                Search = text
            },
            cancellationToken);

        return await Task.WhenAll(
            result.Items.Select(x => BuildCashAccountLookupAsync(x, currencyId, cancellationToken)));
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchBankAccountsByCurrencyAsync(
        Guid? currencyId,
        string text,
        CancellationToken cancellationToken)
    {
        if (!currencyId.HasValue)
            return Array.Empty<UiLookupItem>();

        var result = await AccountingService.GetBankAccountsPageAsync(
            new PageRequest
            {
                PageNumber = 1,
                PageSize = PageRequest.MaximumPageSize,
                Search = text
            },
            cancellationToken);

        return await Task.WhenAll(
            result.Items.Select(x => BuildBankAccountLookupAsync(x, currencyId, cancellationToken)));
    }

    private async Task<AccountDto?> GetAccountDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_accountDtos.TryGetValue(id, out var cached))
            return cached;

        /*
         * This is a secondary lookup used while composing cash/bank lookup
         * rows. It must not be aborted merely because the user typed another
         * character in the parent lookup. UiLookup now protects all searches
         * globally, and this extra guard also keeps this N+1 enrichment safe
         * if the method is called from another cancellable flow later.
         */
        var account = await AccountingService.GetAccountByIdAsync(
            id,
            CancellationToken.None);

        if (account is not null)
        {
            _accountDtos[id] = account;
            _accountLookups[id] = ToAccountLookup(account);
        }

        return account;
    }

    private async Task<UiLookupItem> BuildCashAccountLookupAsync(
        CashAccountDto cash,
        Guid? requiredCurrencyId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountDtoAsync(cash.AccountId, cancellationToken);
        return AccountingLookupEligibility.ToCashAccountLookup(cash, account, requiredCurrencyId);
    }

    private async Task<UiLookupItem> BuildBankAccountLookupAsync(
        BankAccountDto bank,
        Guid? requiredCurrencyId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountDtoAsync(bank.AccountId, cancellationToken);
        return AccountingLookupEligibility.ToBankAccountLookup(bank, account, requiredCurrencyId);
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchExpenseTypesAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetExpenseTypesPageAsync(new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize, Search = text }, cancellationToken);
        RememberExpenseTypeLookups(result.Items);
        return result.Items.Select(ToExpenseTypeLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCustomersAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.LookupCustomersAsync(text, cancellationToken) ?? [];
        foreach (var customer in result) _customerLookups[customer.Id] = ToCustomerLookup(customer);
        return result.Select(ToCustomerLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchSuppliersAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.LookupSuppliersAsync(text, cancellationToken) ?? [];
        foreach (var supplier in result) _supplierLookups[supplier.Id] = ToSupplierLookup(supplier);
        return result.Select(ToSupplierLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCurrenciesAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetCurrenciesPageAsync(new PageRequest { PageNumber = 1, PageSize = 50, Search = text }, cancellationToken);
        foreach (var currency in result.Items) _currencyLookups[currency.Id] = ToCurrencyLookup(currency);
        return result.Items.Where(x => x.IsActive).Select(ToCurrencyLookup).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchEmployeeAccountsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await AccountingService.GetEmployeeAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 100, Search = text }, cancellationToken);
        foreach (var mapping in result.Items) _employeeAccountLookups[mapping.EmployeeId] = ToEmployeeAccountLookup(mapping);
        return result.Items.Select(ToEmployeeAccountLookup).ToArray();
    }

    private async Task<decimal?> ResolveEffectiveRatePreviewAsync(
        Guid currencyId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await AccountingService.GetEffectiveExchangeRateAsync(
                currencyId,
                date,
                ExchangeRateType.Accounting,
                cancellationToken);
            return result?.Rate;
        }
        catch
        {
            // Saving remains server-authoritative. A missing preview must not make
            // the editor crash; the command handler will return the real business error.
            return null;
        }
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchPaymentSourcesAsync(string text, CancellationToken cancellationToken)
    {
        if (Workspace.ActiveTab?.Model is not PaymentAllocationEditor.FormModel model) return [];
        var items = new List<UiLookupItem>();
        if (model.PaymentSourceType == PaymentSourceType.ReceiptVoucher)
        {
            var page = await AccountingService.GetReceiptVouchersPageAsync(new PageRequest { PageNumber = 1, PageSize = 50, Search = text }, cancellationToken);
            foreach (var voucher in page.Items)
                foreach (var line in voucher.Lines.Where(x => x.CurrencyId.HasValue && x.ExchangeRate is > 0))
                {
                    var item = ToReceiptSourceLineLookup(voucher, line);
                    _paymentSourceLookups[line.Id] = item;
                    items.Add(item);
                }
        }
        else if (model.PaymentSourceType == PaymentSourceType.PaymentVoucher)
        {
            var page = await AccountingService.GetPaymentVouchersPageAsync(new PageRequest { PageNumber = 1, PageSize = 50, Search = text }, cancellationToken);
            foreach (var voucher in page.Items)
                foreach (var line in voucher.Lines.Where(x => x.CurrencyId.HasValue && x.ExchangeRate is > 0))
                {
                    var item = ToPaymentSourceLineLookup(voucher, line);
                    _paymentSourceLookups[line.Id] = item;
                    items.Add(item);
                }
        }
        return items;
    }

    private UiLookupItem? GetAccountLookup(Guid? id) => GetLookup(_accountLookups, id);
    private UiLookupItem? GetAccountLookup(Guid? id, AccountingAccountEligibilityContext context)
    {
        if (!id.HasValue) return null;
        return _accountDtos.TryGetValue(id.Value, out var account)
            ? AccountingLookupEligibility.ToAccountLookup(account, context)
            : GetLookup(_accountLookups, id);
    }
    private UiLookupItem? GetCostCenterLookup(Guid? id) => GetLookup(_costCenterLookups, id);
    private UiLookupItem? GetFiscalYearLookup(Guid? id) => GetLookup(_fiscalYearLookups, id);
    private UiLookupItem? GetFiscalPeriodLookup(Guid? id) => GetLookup(_fiscalPeriodLookups, id);
    private UiLookupItem? GetCashAccountLookup(Guid? id) => GetLookup(_cashAccountLookups, id);
    private UiLookupItem? GetBankAccountLookup(Guid? id) => GetLookup(_bankAccountLookups, id);
    private UiLookupItem? GetExpenseTypeLookup(Guid? id) => GetLookup(_expenseTypeLookups, id);
    private UiLookupItem? GetPaymentSourceLookup(Guid? id) => GetLookup(_paymentSourceLookups, id);
    private UiLookupItem? GetCustomerLookup(Guid? id) => GetLookup(_customerLookups, id);
    private UiLookupItem? GetSupplierLookup(Guid? id) => GetLookup(_supplierLookups, id);
    private UiLookupItem? GetCurrencyLookup(Guid? id) => GetLookup(_currencyLookups, id);
    private UiLookupItem? GetEmployeeAccountLookup(Guid? employeeId) => GetLookup(_employeeAccountLookups, employeeId);

    private static UiLookupItem? GetLookup(IReadOnlyDictionary<Guid, UiLookupItem> source, Guid? id) =>
        id.HasValue && source.TryGetValue(id.Value, out var item) ? item : null;

    private static UiLookupItem? GetExternalReferenceLookup(Guid? id, string label) =>
        id.HasValue
            ? new UiLookupItem(id.Value.ToString("D"), $"{label} مرتبط", id.Value.ToString("D"), "fa-solid fa-link")
            : null;

    private async Task<UiLookupItem?> EnsureCustomerLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_customerLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetCustomerByIdAsync(id.Value);
        if (dto is null) return null;
        var item = new UiLookupItem(
            dto.Id.ToString("D"),
            $"{dto.CustomerCode} | {dto.AccountCode} | {dto.NameAr}",
            dto.IsActive ? "نشط" : "العميل غير نشط",
            "fa-solid fa-user-tie",
            Disabled: !dto.IsActive);
        _customerLookups[dto.Id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureSupplierLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_supplierLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetSupplierByIdAsync(id.Value);
        if (dto is null) return null;
        var item = new UiLookupItem(
            dto.Id.ToString("D"),
            $"{dto.SupplierCode} | {dto.AccountCode} | {dto.NameAr}",
            dto.IsActive ? "نشط" : "المورد غير نشط",
            "fa-solid fa-truck-field",
            Disabled: !dto.IsActive);
        _supplierLookups[dto.Id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureCurrencyLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_currencyLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetCurrencyByIdAsync(id.Value);
        if (dto is null) return null;
        var item = ToCurrencyLookup(dto);
        _currencyLookups[dto.Id] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureEmployeeAccountLookupAsync(Guid? employeeId)
    {
        if (!employeeId.HasValue) return null;
        if (_employeeAccountLookups.TryGetValue(employeeId.Value, out var existing)) return existing;
        var pageNumber = 1;
        while (true)
        {
            var page = await AccountingService.GetEmployeeAccountsPageAsync(
                new PageRequest { PageNumber = pageNumber, PageSize = PageRequest.MaximumPageSize });

            foreach (var dto in page.Items)
                _employeeAccountLookups[dto.EmployeeId] = ToEmployeeAccountLookup(dto);

            if (_employeeAccountLookups.TryGetValue(employeeId.Value, out var item))
                return item;

            if (!page.HasNextPage)
                return null;

            pageNumber++;
        }
    }

    private async Task<UiLookupItem?> EnsureAccountLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        if (_accountLookups.TryGetValue(id.Value, out var existing)) return existing;
        var dto = await AccountingService.GetAccountByIdAsync(id.Value);
        if (dto is null) return null;
        _accountDtos[dto.Id] = dto;
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
        var dto = await AccountingService.GetCashAccountByIdAsync(id.Value);
        if (dto is null) return null;
        var item = await BuildCashAccountLookupAsync(dto, null, CancellationToken.None);
        _cashAccountLookups[id.Value] = item;
        return item;
    }

    private async Task<UiLookupItem?> EnsureBankAccountLookupAsync(Guid? id)
    {
        if (!id.HasValue) return null;
        var dto = await AccountingService.GetBankAccountByIdAsync(id.Value);
        if (dto is null) return null;
        var item = await BuildBankAccountLookupAsync(dto, null, CancellationToken.None);
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

    private async Task<UiLookupItem?> EnsurePaymentSourceLineLookupAsync(PaymentSourceType type, Guid lineId)
    {
        if (_paymentSourceLookups.TryGetValue(lineId, out var existing)) return existing;
        if (type == PaymentSourceType.ReceiptVoucher)
        {
            var page = await AccountingService.GetReceiptVouchersPageAsync(new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize });
            foreach (var voucher in page.Items)
            {
                var line = voucher.Lines.FirstOrDefault(x => x.Id == lineId);
                if (line is null) continue;
                var item = ToReceiptSourceLineLookup(voucher, line);
                _paymentSourceLookups[lineId] = item;
                return item;
            }
        }
        else
        {
            var page = await AccountingService.GetPaymentVouchersPageAsync(new PageRequest { PageNumber = 1, PageSize = PageRequest.MaximumPageSize });
            foreach (var voucher in page.Items)
            {
                var line = voucher.Lines.FirstOrDefault(x => x.Id == lineId);
                if (line is null) continue;
                var item = ToPaymentSourceLineLookup(voucher, line);
                _paymentSourceLookups[lineId] = item;
                return item;
            }
        }
        return null;
    }

    private static UiLookupItem ToReceiptSourceLineLookup(ReceiptVoucherDto voucher, ReceiptVoucherLineDto line) =>
        new(line.Id.ToString("D"), $"{voucher.VoucherNumber} / سطر {line.LineNumber}", $"{line.PartyNameSnapshot ?? "طرف"} · {line.Amount:N4} {line.CurrencyCodeSnapshot}", "fa-solid fa-money-bill-trend-up");

    private static UiLookupItem ToPaymentSourceLineLookup(PaymentVoucherDto voucher, PaymentVoucherLineDto line) =>
        new(line.Id.ToString("D"), $"{voucher.VoucherNumber} / سطر {line.LineNumber}", $"{line.PartyNameSnapshot ?? "طرف"} · {line.Amount:N4} {line.CurrencyCodeSnapshot}", "fa-solid fa-money-bill-transfer");

    private static UiLookupItem ToCustomerLookup(CustomerLookupDto x) =>
        new(x.Id.ToString("D"), x.DisplayText, x.IsActive ? "نشط" : "العميل غير نشط", "fa-solid fa-user-tie", Disabled: !x.IsActive);
    private static UiLookupItem ToSupplierLookup(SupplierLookupDto x) =>
        new(x.Id.ToString("D"), x.DisplayText, x.IsActive ? "نشط" : "المورد غير نشط", "fa-solid fa-truck-field", Disabled: !x.IsActive);
    private static UiLookupItem ToCurrencyLookup(CurrencyDto x) =>
        new(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", $"{x.Symbol} · {x.DecimalPlaces} منازل", "fa-solid fa-coins");
    private static UiLookupItem ToEmployeeAccountLookup(EmployeeAccountDto x) =>
        new(x.EmployeeId.ToString("D"), $"{x.EmployeeCode} - {x.EmployeeName}",
            x.IsActive ? $"{x.AccountCode} - {x.AccountName}" : "الربط المحاسبي للموظف غير نشط",
            "fa-solid fa-user-gear", Disabled: !x.IsActive);
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
        foreach (var item in items)
        {
            _accountDtos[item.Id] = item;
            _accountLookups[item.Id] = ToAccountLookup(item);
        }
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
    private async Task RememberCashAccountLookupsAsync(IEnumerable<CashAccountDto> items)
    {
        foreach (var item in items)
            _cashAccountLookups[item.Id] = await BuildCashAccountLookupAsync(item, null, CancellationToken.None);
    }

    private async Task RememberBankAccountLookupsAsync(IEnumerable<BankAccountDto> items)
    {
        foreach (var item in items)
            _bankAccountLookups[item.Id] = await BuildBankAccountLookupAsync(item, null, CancellationToken.None);
    }
    private void RememberExpenseTypeLookups(IEnumerable<ExpenseTypeDto> items)
    {
        foreach (var item in items) _expenseTypeLookups[item.Id] = ToExpenseTypeLookup(item);
    }
    private void RememberPaymentSourceLookups(IEnumerable<ReceiptVoucherDto> items)
    {
        foreach (var voucher in items)
            foreach (var line in voucher.Lines.Where(x => x.CurrencyId.HasValue))
                _paymentSourceLookups[line.Id] = ToReceiptSourceLineLookup(voucher, line);
    }
    private void RememberPaymentSourceLookups(IEnumerable<PaymentVoucherDto> items)
    {
        foreach (var voucher in items)
            foreach (var line in voucher.Lines.Where(x => x.CurrencyId.HasValue))
                _paymentSourceLookups[line.Id] = ToPaymentSourceLineLookup(voucher, line);
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
            case AccountingEntityType.ReceiptVouchers:
                foreach (var x in _receiptsPage.Items)
                    foreach (var line in x.Lines)
                    {
                        await EnsureCashAccountLookupAsync(line.CashAccountId);
                        await EnsureBankAccountLookupAsync(line.BankAccountId);
                        await EnsureCurrencyLookupAsync(line.CurrencyId);
                    }
                break;
            case AccountingEntityType.PaymentVouchers:
                foreach (var x in _paymentsPage.Items)
                    foreach (var line in x.Lines)
                    {
                        await EnsureCashAccountLookupAsync(line.CashAccountId);
                        await EnsureBankAccountLookupAsync(line.BankAccountId);
                        await EnsureCurrencyLookupAsync(line.CurrencyId);
                    }
                break;
            case AccountingEntityType.PaymentAllocations:
                foreach (var x in _allocationsPage.Items)
                {
                    var sourceType = x.ReceiptVoucherLineId.HasValue ? PaymentSourceType.ReceiptVoucher : PaymentSourceType.PaymentVoucher;
                    var sourceId = x.ReceiptVoucherLineId ?? x.PaymentVoucherLineId;
                    if (sourceId.HasValue) await EnsurePaymentSourceLineLookupAsync(sourceType, sourceId.Value);
                }
                break;
            case AccountingEntityType.CashAccounts:
                foreach (var x in _cashAccountsPage.Items) { await EnsureAccountLookupAsync(x.AccountId); await EnsureCurrencyLookupAsync(x.CurrencyId); }
                break;
            case AccountingEntityType.BankAccounts:
                foreach (var x in _bankAccountsPage.Items) { await EnsureAccountLookupAsync(x.AccountId); await EnsureCurrencyLookupAsync(x.CurrencyId); }
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

    private IReadOnlyList<AccountingRecordItem> BuildReceiptItems() => _receiptsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            x.VoucherNumber,
            x.Description ?? $"{x.Lines.Count} سطر تسوية",
            "fa-solid fa-money-bill-trend-up",
            [
                new("التاريخ", x.VoucherDate.ToString("yyyy-MM-dd")),
                new("الأسطر", x.Lines.Count.ToString()),
                new("الإجمالي الأساسي", x.BaseTotalAmount?.ToString($"N{Math.Clamp((int)(x.BaseCurrencyDecimalPlacesSnapshot ?? 2), 0, 6)}") ?? "-", "ui-acc-val--debit"),
                new("العملات", string.Join("، ", x.Lines.Select(l => l.CurrencyCodeSnapshot).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct()))
            ],
            AccountingArabicPresenter.GetReceiptVoucherStatusText(x.Status),
            VoucherState(x.Status.ToString()))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildPaymentItems() => _paymentsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            x.VoucherNumber,
            x.Description ?? $"{x.Lines.Count} سطر تسوية",
            "fa-solid fa-money-bill-transfer",
            [
                new("التاريخ", x.VoucherDate.ToString("yyyy-MM-dd")),
                new("الأسطر", x.Lines.Count.ToString()),
                new("الإجمالي الأساسي", x.BaseTotalAmount?.ToString($"N{Math.Clamp((int)(x.BaseCurrencyDecimalPlacesSnapshot ?? 2), 0, 6)}") ?? "-", "ui-acc-val--credit"),
                new("العملات", string.Join("، ", x.Lines.Select(l => l.CurrencyCodeSnapshot).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct()))
            ],
            AccountingArabicPresenter.GetPaymentVoucherStatusText(x.Status),
            VoucherState(x.Status.ToString()))).ToArray();

    private IReadOnlyList<AccountingRecordItem> BuildCashAccountItems() => _cashAccountsPage.Items.Select(x =>
        new AccountingRecordItem(
            x.Id,
            $"{x.Code} - {x.Name}",
            x.IsDefault ? "الصندوق الافتراضي" : null,
            "fa-solid fa-vault",
            [new("حساب GL", GetAccountLookup(x.AccountId)?.PrimaryText ?? ShortId(x.AccountId)), new("العملة", GetCurrencyLookup(x.CurrencyId)?.PrimaryText ?? "-")],
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
                new("العملة", GetCurrencyLookup(x.CurrencyId)?.PrimaryText ?? "-"),
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
    {
        var sourceType = x.ReceiptVoucherLineId.HasValue ? PaymentSourceType.ReceiptVoucher : PaymentSourceType.PaymentVoucher;
        var sourceId = x.ReceiptVoucherLineId ?? x.PaymentVoucherLineId;
        return new AccountingRecordItem(
            x.Id,
            $"{AccountingArabicPresenter.GetPaymentSourceTypeText(sourceType)} - {(sourceId.HasValue ? GetPaymentSourceLookup(sourceId)?.PrimaryText ?? ShortId(sourceId.Value) : "مصدر تاريخي")}",
            AccountingArabicPresenter.GetAllocationTargetDocumentTypeText(x.TargetDocumentType),
            "fa-solid fa-link",
            [
                new("المستند الهدف", ShortId(x.TargetDocumentId)),
                new("المبلغ", $"{x.AllocatedAmount:N4} {x.CurrencyCodeSnapshot}", "ui-acc-val--debit"),
                new("الأساسي", x.BaseAllocatedAmount?.ToString("N4") ?? "-"),
                new("التاريخ", x.AllocatedAtUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm"))
            ],
            "مخصص",
            "active");
    }).ToArray();

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

    private void ShowAccountingError(OAS.Contracts.Common.Errors.ApiError error)
    {
        if (AccountingApiErrorPresenter.TryGetMessage(error, out var message))
        {
            Snackbar.Error(message);
            return;
        }

        ApiFeedback.Show(error);
    }

    private static string GetEntityDisplayName(AccountingEntityType type) => type switch
    {
        AccountingEntityType.Accounts => "حساب",
        AccountingEntityType.FiscalYears => "سنة مالية",
        AccountingEntityType.FiscalPeriods => "فترة مالية",
        AccountingEntityType.Journals => "قيد يومية",
        AccountingEntityType.PostingProfiles => "ملف ترحيل",
        AccountingEntityType.CostCenters => "مركز تكلفة",
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
