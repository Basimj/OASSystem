using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Identity.Users.Workspace;
using OAS.Contracts.Identity.Roles;
using OAS.UiLib.Components.Inputs;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Identity.Users.Components;

public partial class UserEditor
{
    private UiInputText? _userNameInput;

    [Parameter, EditorRequired] public UserEditorState State { get; set; } = default!;
    [Parameter] public UserEditorMode Mode { get; set; } = UserEditorMode.Empty;
    [Parameter] public IReadOnlyList<RoleDto> Roles { get; set; } = [];
    [Parameter] public bool IsActorSuperAdmin { get; set; }
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public EventCallback<IBrowserFile> OnImageSelected { get; set; }
    [Parameter] public EventCallback OnImageRemoved { get; set; }

    private bool IsEditable => Mode is UserEditorMode.Create or UserEditorMode.Edit;
    private string? EffectiveImageUrl => State.PendingPhoto?.PreviewUrl ?? (State.PhotoRemoved ? null : ImageUrl);
    private string EditorDisplayName => string.Join(' ', new[] { State.Form.FirstName, State.Form.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private string EditorInitials
    {
        get
        {
            var parts = EditorDisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Concat(parts.Take(2).Select(x => x[..1])).ToUpperInvariant();
        }
    }

    private IReadOnlyList<UiSectionTabItem> SectionTabs =>
    [
        new("basic", L["Users_BasicInfo"], State.ActiveSection == "basic"),
        new("roles", L["Users_Roles"], State.ActiveSection == "roles"),
        new("security", L["Users_Security"], State.ActiveSection == "security"),
        new("system", L["Users_SystemInfo"], State.ActiveSection == "system")
    ];

    public async ValueTask FocusUserNameAsync()
    {
        State.ActiveSection = "basic";
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        if (_userNameInput is not null) await _userNameInput.FocusAsync();
    }

    private Task SelectSectionAsync(string key)
    {
        State.ActiveSection = key;
        return Task.CompletedTask;
    }

    private void RoleChanged(Guid roleId, bool selected)
    {
        if (IsEditable) State.SetRole(roleId, selected);
    }

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
