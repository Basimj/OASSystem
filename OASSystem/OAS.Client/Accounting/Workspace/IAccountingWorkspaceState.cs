namespace OAS.Client.Accounting.Workspace;

public interface IAccountingWorkspaceState
{
    IReadOnlyList<AccountingTabState> Tabs { get; }
    Guid ActiveTabId { get; set; }
    AccountingTabState? ActiveTab { get; }

    AccountingTabState OpenOrActivateListTab(AccountingEntityType entityType);
    AccountingTabState OpenOrActivateEntityTab(AccountingEntityType entityType, Guid entityId, string title);
    AccountingTabState OpenNewEntityTab(AccountingEntityType entityType, string? defaultTitle = null);
    AccountingTabState? FindTab(Guid tabId);
    bool RemoveTab(Guid tabId);

    void NotifyStateChanged();
    event Action? OnChange;
}
