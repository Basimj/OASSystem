namespace OAS.Client.Identity.Users.Workspace;

public sealed class UsersWorkspaceState : IUsersWorkspaceState
{
    private readonly List<UserWorkspaceTab> _tabs = [];
    private int _newDraftCounter;

    public Guid AllUsersTabId => Guid.Empty;
    public Guid ActiveTabId { get; private set; } = Guid.Empty;
    public IReadOnlyList<UserWorkspaceTab> UserTabs => _tabs;
    public UserWorkspaceTab? ActiveUserTab => ActiveTabId == AllUsersTabId ? null : Find(ActiveTabId);

    public void Activate(Guid workspaceTabId)
    {
        if (workspaceTabId == AllUsersTabId || _tabs.Any(x => x.WorkspaceTabId == workspaceTabId))
            ActiveTabId = workspaceTabId;
    }

    public UserWorkspaceTab OpenNew(string title, IEnumerable<Guid>? defaultRoles = null)
    {
        _newDraftCounter++;
        var editor = new UserEditorState(true);
        editor.InitializeNew(defaultRoles);
        var tab = new UserWorkspaceTab
        {
            WorkspaceTabId = Guid.NewGuid(),
            UserId = null,
            Title = $"{title} {_newDraftCounter}",
            IsNew = true,
            Editor = editor
        };
        _tabs.Add(tab);
        ActiveTabId = tab.WorkspaceTabId;
        return tab;
    }

    public UserWorkspaceTab OpenExisting(Guid userId, string title)
    {
        var existing = FindByUserId(userId);
        if (existing is not null)
        {
            ActiveTabId = existing.WorkspaceTabId;
            return existing;
        }

        var tab = new UserWorkspaceTab
        {
            WorkspaceTabId = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            IsNew = false,
            Editor = new UserEditorState(false),
            IsLoading = true
        };
        _tabs.Add(tab);
        ActiveTabId = tab.WorkspaceTabId;
        return tab;
    }

    public UserWorkspaceTab? FindByUserId(Guid userId) => _tabs.FirstOrDefault(x => x.UserId == userId);
    public UserWorkspaceTab? Find(Guid workspaceTabId) => _tabs.FirstOrDefault(x => x.WorkspaceTabId == workspaceTabId);

    public void Close(Guid workspaceTabId)
    {
        var tab = Find(workspaceTabId);
        if (tab is null) return;
        var index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);
        if (ActiveTabId != workspaceTabId) return;
        ActiveTabId = _tabs.Count == 0
            ? AllUsersTabId
            : _tabs[Math.Clamp(index - 1, 0, _tabs.Count - 1)].WorkspaceTabId;
    }
}
