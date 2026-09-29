namespace OAS.Client.Purchasing.Workspace;

public interface IPurchasingWorkspaceState
{
    Guid ActiveTabId { get; set; }
    IReadOnlyList<PurchasingTabState> Tabs { get; }
    PurchasingTabState? ActiveTab { get; }
    PurchasingTabState OpenList(PurchasingEntityType entityType);
    PurchasingTabState OpenNew(PurchasingEntityType entityType, string title);
    PurchasingTabState OpenRecord(PurchasingEntityType entityType, Guid id, string title);
    PurchasingTabState? Find(Guid tabId);
    bool Close(Guid tabId);
    IReadOnlyList<PurchasingTabState> VisibleTabs(PurchasingEntityType entityType);
}
