namespace OAS.Client.Sales.Workspace;

public interface ISalesWorkspaceState
{
    IReadOnlyList<SalesWorkspaceTabState> Tabs { get; }
    Guid ActiveTabId { get; set; }
    SalesWorkspaceTabState? ActiveTab { get; }
    event Action? OnChange;
    SalesWorkspaceTabState OpenOrActivateListTab(SalesEntityType type);
    SalesWorkspaceTabState OpenOrActivateEntityTab(SalesEntityType type, Guid id, string title);
    SalesWorkspaceTabState OpenNewEntityTab(SalesEntityType type, string? title = null);
    SalesWorkspaceTabState? FindTab(Guid id);
    bool RemoveTab(Guid id);
    void NotifyStateChanged();
}
