using OAS.Contracts.Identity.Users;

namespace OAS.Client.Identity.Users.Workspace;

public sealed class UserEditorState
{
    private readonly HashSet<Guid> _roleIds = [];
    private UserDetailsDto? _saved;
    private Guid[] _newDefaultRoleIds = [];

    public UserEditorState(bool isNew)
    {
        IsNew = isNew;
        Form = new UserEditorFormModel(() => BasicDirty = true);
        if (isNew) Form.Load(string.Empty, string.Empty, string.Empty, null, true);
    }

    public bool IsNew { get; private set; }
    public UserEditorFormModel Form { get; }
    public bool BasicDirty { get; private set; }
    public bool RolesDirty { get; private set; }
    public bool IsDirty => BasicDirty || RolesDirty;
    public string ActiveSection { get; set; } = "basic";
    public string? RowVersion { get; private set; }
    public UserDetailsDto? Details { get; private set; }
    public IReadOnlyCollection<Guid> RoleIds => _roleIds;

    public void Load(UserDetailsDto details)
    {
        Details = details;
        _saved = details;
        IsNew = false;
        RowVersion = details.RowVersion;
        Form.Load(details.UserName, details.FirstName, details.LastName, details.Email, details.IsActive);
        _roleIds.Clear();
        foreach (var id in details.RoleIds) _roleIds.Add(id);
        BasicDirty = false;
        RolesDirty = false;
    }

    public void InitializeNew(IEnumerable<Guid>? defaultRoles = null)
    {
        IsNew = true;
        Details = null;
        _saved = null;
        RowVersion = null;
        Form.Load(string.Empty, string.Empty, string.Empty, null, true);
        _newDefaultRoleIds = defaultRoles?.Distinct().ToArray() ?? [];
        _roleIds.Clear();
        foreach (var id in _newDefaultRoleIds) _roleIds.Add(id);
        BasicDirty = false;
        RolesDirty = false;
        ActiveSection = "basic";
    }

    public bool IsRoleSelected(Guid roleId) => _roleIds.Contains(roleId);

    public void SetRole(Guid roleId, bool selected)
    {
        var changed = selected ? _roleIds.Add(roleId) : _roleIds.Remove(roleId);
        if (changed) RolesDirty = true;
    }

    public void MarkSaved(UserDetailsDto details) => Load(details);

    public void AcceptBasicSave(UserDetailsDto details)
    {
        Details = details;
        _saved = details;
        RowVersion = details.RowVersion;
        Form.Load(details.UserName, details.FirstName, details.LastName, details.Email, details.IsActive);
        BasicDirty = false;
    }

    public void AcceptRolesSave(UserDetailsDto details)
    {
        Details = details;
        _saved = details;
        RowVersion = details.RowVersion;
        _roleIds.Clear();
        foreach (var id in details.RoleIds) _roleIds.Add(id);
        RolesDirty = false;
    }

    public void ApplyServerState(UserDetailsDto details)
    {
        Details = details;
        _saved = details;
        RowVersion = details.RowVersion;
    }

    public void ApplyPasswordReset(string rowVersion)
    {
        RowVersion = rowVersion;
        if (Details is null) return;

        var updated = Details with
        {
            MustChangePassword = true,
            AccessFailedCount = 0,
            LockoutEndUtc = null,
            IsLocked = false,
            RowVersion = rowVersion
        };
        Details = updated;
        _saved = updated;
    }

    public void Revert()
    {
        if (_saved is not null)
        {
            Load(_saved);
            return;
        }

        InitializeNew(_newDefaultRoleIds);
    }
}
