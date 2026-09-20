namespace OAS.Client.Accounting.Workspace;

public sealed class AccountingTabState
{
    private string? _baselineJson;

    public AccountingTabState(
        AccountingEntityType entityType,
        Guid? entityId = null,
        bool isListTab = false,
        string? title = null,
        bool canClose = true)
    {
        EntityType = entityType;
        EntityId = entityId;
        IsListTab = isListTab;
        CanClose = canClose;
        IsEditMode = !isListTab && entityId is null;
        Title = title ?? GetDefaultTitle(entityType, isListTab, entityId is null);
    }

    public Guid TabId { get; } = Guid.NewGuid();
    public AccountingEntityType EntityType { get; }
    public Guid? EntityId { get; private set; }
    public bool IsListTab { get; }
    public bool CanClose { get; set; }
    public string Title { get; set; }
    public bool IsNew => !IsListTab && EntityId is null;
    public bool IsEditMode { get; private set; }
    public bool IsLoading { get; set; }
    public bool IsSaving { get; set; }
    public bool IsInitialized { get; set; }
    public object? Model { get; set; }

    public bool IsDirty { get; set; }

    public void CaptureBaseline(string json)
    {
        _baselineJson = json;
        IsDirty = false;
    }

    public void CheckDirty(string currentJson)
    {
        if (_baselineJson is not null)
        {
            IsDirty = !string.Equals(_baselineJson, currentJson, StringComparison.Ordinal);
        }
    }

    public void BeginEdit()
    {
        if (IsListTab) return;
        IsEditMode = true;
    }

    public void CancelEdit()
    {
        if (IsListTab) return;
        IsEditMode = IsNew;
        IsDirty = false;
    }

    public void CompleteSave(Guid id, string title, object savedModel, string baselineJson)
    {
        EntityId = id;
        Title = title;
        Model = savedModel;
        _baselineJson = baselineJson;
        IsDirty = false;
        IsEditMode = false;
        IsInitialized = true;
    }

    public string GetIconCss()
    {
        var baseIcon = EntityType switch
        {
            AccountingEntityType.Overview => "fa-solid fa-chart-pie",
            AccountingEntityType.Accounts => IsListTab ? "fa-solid fa-folder-tree" : (IsNew ? "fa-solid fa-folder-plus" : "fa-regular fa-folder-open"),
            AccountingEntityType.FiscalYears => "fa-solid fa-calendar-days",
            AccountingEntityType.FiscalPeriods => "fa-solid fa-calendar-week",
            AccountingEntityType.Journals => IsListTab ? "fa-solid fa-book-journal-whills" : (IsNew ? "fa-solid fa-square-plus" : "fa-solid fa-file-lines"),
            AccountingEntityType.PostingProfiles => "fa-solid fa-sliders",
            AccountingEntityType.CustomerAccounts => "fa-solid fa-users-line",
            AccountingEntityType.SupplierAccounts => "fa-solid fa-boxes-packing",
            AccountingEntityType.ReceiptVouchers => IsListTab ? "fa-solid fa-file-invoice-dollar" : (IsNew ? "fa-solid fa-plus-circle" : "fa-solid fa-money-bill-trend-up"),
            AccountingEntityType.PaymentVouchers => IsListTab ? "fa-solid fa-money-bill-transfer" : (IsNew ? "fa-solid fa-minus-circle" : "fa-solid fa-money-bill-wave"),
            AccountingEntityType.PaymentAllocations => "fa-solid fa-link",
            AccountingEntityType.CashAccounts => "fa-solid fa-vault",
            AccountingEntityType.BankAccounts => "fa-solid fa-building-columns",
            AccountingEntityType.CashShifts => "fa-solid fa-cash-register",
            AccountingEntityType.ExpenseTypes => "fa-solid fa-tags",
            AccountingEntityType.Expenses => IsListTab ? "fa-solid fa-receipt" : (IsNew ? "fa-solid fa-plus" : "fa-solid fa-file-invoice"),
            AccountingEntityType.CostCenters => "fa-solid fa-network-wired",
            _ => "fa-solid fa-file"
        };

        return IsDirty ? $"{baseIcon} ui-accounting-tab-icon--dirty" : baseIcon;
    }

    private static string GetDefaultTitle(AccountingEntityType type, bool isList, bool isNew)
    {
        if (isList)
        {
            return type switch
            {
                AccountingEntityType.Overview => "لوحة المحاسبة",
                AccountingEntityType.Accounts => "شجرة الحسابات",
                AccountingEntityType.FiscalYears => "السنوات المالية",
                AccountingEntityType.FiscalPeriods => "الفترات المالية",
                AccountingEntityType.Journals => "القيود اليومية",
                AccountingEntityType.PostingProfiles => "ملفات الترحيل",
                AccountingEntityType.CustomerAccounts => "حسابات العملاء",
                AccountingEntityType.SupplierAccounts => "حسابات الموردين",
                AccountingEntityType.ReceiptVouchers => "سندات القبض",
                AccountingEntityType.PaymentVouchers => "سندات الصرف",
                AccountingEntityType.PaymentAllocations => "تخصيصات السداد",
                AccountingEntityType.CashAccounts => "الحسابات النقدية",
                AccountingEntityType.BankAccounts => "الحسابات البنكية",
                AccountingEntityType.CashShifts => "ورديات الصندوق",
                AccountingEntityType.ExpenseTypes => "أنواع المصروفات",
                AccountingEntityType.Expenses => "المصروفات",
                AccountingEntityType.CostCenters => "مراكز التكلفة",
                _ => "المحاسبة"
            };
        }

        if (isNew)
        {
            return type switch
            {
                AccountingEntityType.Accounts => "إضافة حساب",
                AccountingEntityType.FiscalYears => "سنة مالية جديدة",
                AccountingEntityType.FiscalPeriods => "فترة مالية جديدة",
                AccountingEntityType.Journals => "قيد يومية جديد",
                AccountingEntityType.PostingProfiles => "ملف ترحيل جديد",
                AccountingEntityType.CustomerAccounts => "حساب عميل جديد",
                AccountingEntityType.SupplierAccounts => "حساب مورد جديد",
                AccountingEntityType.ReceiptVouchers => "سند قبض جديد",
                AccountingEntityType.PaymentVouchers => "سند صرف جديد",
                AccountingEntityType.PaymentAllocations => "تخصيص سداد جديد",
                AccountingEntityType.CashAccounts => "حساب نقدي جديد",
                AccountingEntityType.BankAccounts => "حساب بنكي جديد",
                AccountingEntityType.CashShifts => "فتح وردية جديدة",
                AccountingEntityType.ExpenseTypes => "نوع مصروف جديد",
                AccountingEntityType.Expenses => "مصروف جديد",
                AccountingEntityType.CostCenters => "مركز تكلفة جديد",
                _ => "جديد"
            };
        }

        return "تفاصيل السجل";
    }
}
