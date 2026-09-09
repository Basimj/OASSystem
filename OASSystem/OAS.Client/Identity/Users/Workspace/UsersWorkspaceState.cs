using OAS.Contracts.Identity.Users;

namespace OAS.Client.Identity.Users.Workspace;

public sealed class UsersWorkspaceState : IUsersWorkspaceState
{
    public Guid? SelectedUserId { get; private set; }
    public string? SelectedTitle { get; private set; }
    public UserEditorMode Mode { get; private set; } = UserEditorMode.Empty;
    public UserEditorState Editor { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsDirty => Editor.IsDirty;
    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? RoleFilter { get; set; }
    public int PageNumber { get; set; } = 1;

    public void BeginCreate(IEnumerable<Guid>? defaultRoles = null)
    {
        SelectedUserId = null;
        SelectedTitle = null;
        Mode = UserEditorMode.Create;
        Editor.InitializeNew(defaultRoles);
        IsLoading = false;
    }

    public void BeginEdit()
    {
        if (SelectedUserId.HasValue && Editor.Details is not null) Mode = UserEditorMode.Edit;
    }

    public void SelectExisting(Guid userId, string title)
    {
        SelectedUserId = userId;
        SelectedTitle = title;
        Mode = UserEditorMode.View;
        IsLoading = true;
    }

    public void LoadDetails(UserDetailsDto details)
    {
        SelectedUserId = details.Id;
        SelectedTitle = string.IsNullOrWhiteSpace(details.DisplayName) ? details.UserName : details.DisplayName;
        Editor.Load(details);
        Mode = UserEditorMode.View;
        IsLoading = false;
    }

    public void ReturnToView()
    {
        if (SelectedUserId.HasValue && Editor.Details is not null) Mode = UserEditorMode.View;
        else Mode = UserEditorMode.Empty;
    }

    public void ClearSelection()
    {
        SelectedUserId = null; SelectedTitle = null; Mode = UserEditorMode.Empty; IsLoading = false;
        Editor.Clear();
    }

    public void Revert()
    {
        Editor.Revert();
        Mode = SelectedUserId.HasValue ? UserEditorMode.View : UserEditorMode.Create;
    }
}
