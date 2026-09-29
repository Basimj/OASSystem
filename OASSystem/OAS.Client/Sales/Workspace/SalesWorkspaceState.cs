namespace OAS.Client.Sales.Workspace;

public sealed class SalesWorkspaceState : ISalesWorkspaceState
{
    private readonly List<SalesWorkspaceTabState> _tabs = [];
    public IReadOnlyList<SalesWorkspaceTabState> Tabs => _tabs;
    public Guid ActiveTabId { get; set; }
    public SalesWorkspaceTabState? ActiveTab => FindTab(ActiveTabId) ?? _tabs.FirstOrDefault();
    public event Action? OnChange;

    public SalesWorkspaceState()
    {
        var tab = new SalesWorkspaceTabState(SalesEntityType.SalesInvoices, isListTab: true, canClose: false);
        _tabs.Add(tab); ActiveTabId = tab.TabId;
    }

    public void NotifyStateChanged() => OnChange?.Invoke();

    public SalesWorkspaceTabState OpenOrActivateListTab(SalesEntityType type)
    {
        var existing = _tabs.FirstOrDefault(x => x.EntityType == type && x.IsListTab);
        if (existing is null) { existing = new(type, isListTab: true, canClose: false); _tabs.Add(existing); }
        ActiveTabId = existing.TabId; NotifyStateChanged(); return existing;
    }

    public SalesWorkspaceTabState OpenOrActivateEntityTab(SalesEntityType type, Guid id, string title)
    {
        var existing = _tabs.FirstOrDefault(x => x.EntityType == type && x.EntityId == id);
        if (existing is null) { existing = new(type, id, false, title, true); _tabs.Add(existing); }
        else if (!string.IsNullOrWhiteSpace(title)) existing.Title = title;
        ActiveTabId = existing.TabId; NotifyStateChanged(); return existing;
    }

    public SalesWorkspaceTabState OpenNewEntityTab(SalesEntityType type, string? title = null)
    {
        var tab = new SalesWorkspaceTabState(type, null, false, title, true); _tabs.Add(tab); ActiveTabId = tab.TabId; return tab;
    }

    public SalesWorkspaceTabState? FindTab(Guid id) => _tabs.FirstOrDefault(x => x.TabId == id);

    public bool RemoveTab(Guid id)
    {
        var tab = FindTab(id); if (tab is null || !tab.CanClose) return false;
        var index = _tabs.IndexOf(tab); _tabs.Remove(tab);
        if (ActiveTabId == id)
        {
            if (_tabs.Count == 0) { var fallback = new SalesWorkspaceTabState(SalesEntityType.SalesInvoices, isListTab: true, canClose: false); _tabs.Add(fallback); ActiveTabId = fallback.TabId; }
            else ActiveTabId = _tabs[Math.Min(index, _tabs.Count - 1)].TabId;
        }
        NotifyStateChanged(); return true;
    }
}
