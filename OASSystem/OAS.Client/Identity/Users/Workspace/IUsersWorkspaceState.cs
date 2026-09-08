namespace OAS.Client.Identity.Users.Workspace;

public interface IUsersWorkspaceState
{
    Guid AllUsersTabId { get; }
    Guid ActiveTabId { get; }
    IReadOnlyList<UserWorkspaceTab> UserTabs { get; }
    UserWorkspaceTab? ActiveUserTab { get; }
    void Activate(Guid workspaceTabId);
    UserWorkspaceTab OpenNew(string title, IEnumerable<Guid>? defaultRoles = null);
    UserWorkspaceTab OpenExisting(Guid userId, string title);
    UserWorkspaceTab? FindByUserId(Guid userId);
    UserWorkspaceTab? Find(Guid workspaceTabId);
    void Close(Guid workspaceTabId);
}
