namespace OAS.Client.Purchasing.Workspace;

public sealed class PurchasingTabState(
    PurchasingEntityType entityType,
    Guid? entityId,
    bool isListTab,
    string title,
    bool canClose)
{
    public Guid TabId { get; } = Guid.NewGuid();
    public PurchasingEntityType EntityType { get; } = entityType;
    public Guid? EntityId { get; set; } = entityId;
    public bool IsListTab { get; } = isListTab;
    public bool IsNew => !IsListTab && !EntityId.HasValue;
    public string Title { get; set; } = title;
    public bool CanClose { get; } = canClose;
    public bool IsLoading { get; set; }
    public bool IsSaving { get; set; }
    public bool IsDirty { get; set; }
    public bool IsEditMode { get; set; } = entityId is null;
    public object? Model { get; set; }
}
