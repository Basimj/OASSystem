using Microsoft.AspNetCore.Components;
using OAS.Client.Identity.Users.Workspace;
using OAS.Contracts.Identity.Roles;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Identity.Users.Components;

public partial class UserEditor
{
    [Parameter, EditorRequired] public UserEditorState State { get; set; } = default!;
    [Parameter] public IReadOnlyList<RoleDto> Roles { get; set; } = [];
    [Parameter] public bool IsActorSuperAdmin { get; set; }

    private IReadOnlyList<UiSectionTabItem> SectionTabs =>
    [
        new("basic", L["Users_BasicInfo"], State.ActiveSection == "basic"),
        new("roles", L["Users_Roles"], State.ActiveSection == "roles"),
        new("security", L["Users_Security"], State.ActiveSection == "security"),
        new("system", L["Users_SystemInfo"], State.ActiveSection == "system")
    ];

    private Task SelectSectionAsync(string key)
    {
        State.ActiveSection = key;
        return Task.CompletedTask;
    }

    private void RoleChanged(Guid roleId, bool selected) => State.SetRole(roleId, selected);

    private bool IsRoleDisabled(RoleDto role) =>
        string.Equals(role.Name, "Administrator", StringComparison.OrdinalIgnoreCase) && !IsActorSuperAdmin;

    private string TranslateRole(string role) => role switch
    {
        "Administrator" => L["Role_Administrator"],
        "User" => L["Role_User"],
        _ => role
    };

    private static string FormatDate(DateTimeOffset? value) => value?.ToLocalTime().ToString("g") ?? "—";
}
