using OAS.Contracts.Identity.Users;

namespace OAS.Client.Identity.Users.Workspace;

public interface IUsersWorkspaceState
{
    Guid? SelectedUserId { get; }
    string? SelectedTitle { get; }
    UserEditorMode Mode { get; }
    UserEditorState Editor { get; }
    bool IsLoading { get; set; }
    bool IsDirty { get; }
    string? Search { get; set; }
    string StatusFilter { get; set; }
    string? RoleFilter { get; set; }
    int PageNumber { get; set; }

    void BeginCreate(IEnumerable<Guid>? defaultRoles = null);
    void BeginEdit();
    void SelectExisting(Guid userId, string title);
    void LoadDetails(UserDetailsDto details);
    void ReturnToView();
    void ClearSelection();
    void Revert();
}
