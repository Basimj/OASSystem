namespace OAS.Client.Sales.Workspace;

public sealed class SalesWorkspaceTabState
{
    public SalesWorkspaceTabState(SalesEntityType entityType, Guid? entityId = null, bool isListTab = false, string? title = null, bool canClose = true)
    {
        EntityType = entityType;
        EntityId = entityId;
        IsListTab = isListTab;
        CanClose = canClose;
        IsEditMode = !isListTab && entityId is null;
        Title = title ?? DefaultTitle(entityType, isListTab, entityId is null);
    }

    public Guid TabId { get; } = Guid.NewGuid();
    public SalesEntityType EntityType { get; }
    public Guid? EntityId { get; private set; }
    public bool IsListTab { get; }
    public bool CanClose { get; set; }
    public string Title { get; set; }
    public bool IsNew => !IsListTab && EntityId is null;
    public bool IsEditMode { get; private set; }
    public bool IsLoading { get; set; }
    public bool IsSaving { get; set; }
    public bool IsInitialized { get; set; }
    public bool IsDirty { get; set; }
    public object? Model { get; set; }

    public void BeginEdit() { if (!IsListTab) IsEditMode = true; }
    public void CancelEdit() { if (!IsListTab) { IsEditMode = IsNew; IsDirty = false; } }
    public void CompleteSave(Guid id, string title, object model) { EntityId = id; Title = title; Model = model; IsEditMode = false; IsDirty = false; IsInitialized = true; }

    public string IconCss() => EntityType switch
    {
        SalesEntityType.Prescriptions => IsListTab ? "fa-solid fa-glasses" : (IsNew ? "fa-solid fa-notes-medical" : "fa-solid fa-file-medical"),
        SalesEntityType.CustomerOrders => IsListTab ? "fa-solid fa-clipboard-list" : (IsNew ? "fa-solid fa-cart-plus" : "fa-solid fa-clipboard-check"),
        SalesEntityType.SalesInvoices => IsListTab ? "fa-solid fa-file-invoice-dollar" : (IsNew ? "fa-solid fa-file-circle-plus" : "fa-solid fa-file-invoice"),
        _ => "fa-solid fa-file"
    };

    public static string DefaultTitle(SalesEntityType type, bool isList, bool isNew) => (type, isList, isNew) switch
    {
        (SalesEntityType.Prescriptions, true, _) => "الوصفات",
        (SalesEntityType.CustomerOrders, true, _) => "طلبات العملاء",
        (SalesEntityType.SalesInvoices, true, _) => "فواتير المبيعات",
        (SalesEntityType.Prescriptions, false, true) => "وصفة جديدة",
        (SalesEntityType.CustomerOrders, false, true) => "طلب جديد",
        (SalesEntityType.SalesInvoices, false, true) => "فاتورة جديدة",
        _ => "المبيعات"
    };
}
