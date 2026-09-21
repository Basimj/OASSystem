namespace OAS.Client.Accounting.Workspace;

public sealed class AccountingWorkspaceState : IAccountingWorkspaceState
{
    private readonly List<AccountingTabState> _tabs = [];

    public AccountingWorkspaceState()
    {
        var defaultTab = new AccountingTabState(
            AccountingEntityType.Accounts,
            entityId: null,
            isListTab: true,
            title: "شجرة الحسابات",
            canClose: false);

        _tabs.Add(defaultTab);
        ActiveTabId = defaultTab.TabId;
    }

    public IReadOnlyList<AccountingTabState> Tabs => _tabs;

    public Guid ActiveTabId { get; set; }

    public AccountingTabState? ActiveTab =>
        FindTab(ActiveTabId) ?? _tabs.FirstOrDefault();

    public event Action? OnChange;

    public void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }

    public AccountingTabState OpenOrActivateListTab(
        AccountingEntityType entityType)
    {
        var existing = _tabs.FirstOrDefault(
            x => x.EntityType == entityType && x.IsListTab);

        if (existing is not null)
        {
            ActiveTabId = existing.TabId;
            NotifyStateChanged();
            return existing;
        }

        var tab = new AccountingTabState(
            entityType,
            entityId: null,
            isListTab: true,
            canClose: true);

        _tabs.Add(tab);
        ActiveTabId = tab.TabId;

        NotifyStateChanged();

        return tab;
    }

    public AccountingTabState OpenOrActivateEntityTab(
        AccountingEntityType entityType,
        Guid entityId,
        string title)
    {
        var existing = _tabs.FirstOrDefault(
            x => x.EntityType == entityType &&
                 x.EntityId == entityId);

        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                existing.Title = title;
            }

            ActiveTabId = existing.TabId;

            NotifyStateChanged();

            return existing;
        }

        var tab = new AccountingTabState(
            entityType,
            entityId,
            isListTab: false,
            title: title,
            canClose: true);

        _tabs.Add(tab);
        ActiveTabId = tab.TabId;

        NotifyStateChanged();

        return tab;
    }

    public AccountingTabState OpenNewEntityTab(
        AccountingEntityType entityType,
        string? defaultTitle = null)
    {
        var tab = new AccountingTabState(
            entityType,
            entityId: null,
            isListTab: false,
            title: defaultTitle,
            canClose: true);

        _tabs.Add(tab);
        ActiveTabId = tab.TabId;

        // لا نرسل NotifyStateChanged هنا.
        // يجب تهيئة Model أولاً ثم إشعار الواجهة.
        return tab;
    }

    public AccountingTabState? FindTab(Guid tabId)
    {
        return _tabs.FirstOrDefault(x => x.TabId == tabId);
    }

    public bool RemoveTab(Guid tabId)
    {
        var tab = FindTab(tabId);

        if (tab is null)
        {
            return false;
        }

        var index = _tabs.IndexOf(tab);

        var removed = _tabs.Remove(tab);

        if (removed && ActiveTabId == tabId)
        {
            if (_tabs.Count > 0)
            {
                var nextIndex = Math.Min(
                    index,
                    _tabs.Count - 1);

                ActiveTabId = _tabs[nextIndex].TabId;
            }
            else
            {
                var defaultTab = new AccountingTabState(
                    AccountingEntityType.Accounts,
                    entityId: null,
                    isListTab: true,
                    title: "شجرة الحسابات",
                    canClose: false);

                _tabs.Add(defaultTab);
                ActiveTabId = defaultTab.TabId;
            }
        }

        NotifyStateChanged();

        return removed;
    }
}