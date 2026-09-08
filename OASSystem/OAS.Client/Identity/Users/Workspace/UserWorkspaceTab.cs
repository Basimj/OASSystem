namespace OAS.Client.Identity.Users.Workspace;

public sealed class UserWorkspaceTab
{
    public required Guid WorkspaceTabId { get; init; }
    public Guid? UserId { get; set; }
    public required string Title { get; set; }
    public bool IsNew { get; set; }
    public required UserEditorState Editor { get; init; }
    public bool IsLoading { get; set; }
    public bool IsDirty => Editor.IsDirty;
}
