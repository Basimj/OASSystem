using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Identity.Users.Workspace;
using OAS.Contracts.Identity.Roles;
using OAS.UiLib.Components.Inputs;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Identity.Users.Components;

public partial class UserEditor
{
    private static readonly Guid BasicSectionId = Guid.Parse("F96CB609-6B71-4A55-B9A8-3D2E363A99AD");
    private static readonly Guid SecuritySectionId = Guid.Parse("FC8C49C9-3EA5-43F5-8813-E69CBFEA5E62");
    private static readonly Guid SystemSectionId = Guid.Parse("EE27B31B-67B3-4686-81DD-4712C4CFFB17");

    private UiInputText? _userNameInput;

    [Parameter, EditorRequired] public UserEditorState State { get; set; } = default!;
    [Parameter] public UserEditorMode Mode { get; set; } = UserEditorMode.Empty;
    [Parameter] public IReadOnlyList<RoleDto> Roles { get; set; } = [];
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

    private IReadOnlyList<UiSelectOption> RoleOptions =>
        Roles.Select(role => new UiSelectOption(role.Id.ToString("D"), GetRoleDisplayName(role))).ToArray();

    private string GetRoleDisplayName(RoleDto role)
    {
        if (!string.Equals(role.DisplayName, role.Name, StringComparison.OrdinalIgnoreCase)) return role.DisplayName;
        return role.Name switch
        {
            "Administrator" => L["Role_Administrator"],
            "User" => L["Role_User"],
            _ => role.DisplayName
        };
    }

    private string? SelectedRoleValue
    {
        get => State.RoleIds.FirstOrDefault() is var id && id != Guid.Empty ? id.ToString("D") : null;
        set => State.SetSingleRole(Guid.TryParse(value, out var roleId) ? roleId : null);
    }

    protected override void OnParametersSet()
    {
        if (State.ActiveSection is not ("basic" or "security" or "system"))
            State.ActiveSection = "basic";
    }

    private IReadOnlyList<UiApplicationTabItem> ApplicationSectionTabs =>
    [
        new(BasicSectionId, L["Users_BasicInfo"], string.Empty, string.Empty, State.ActiveSection == "basic", false),
        new(SecuritySectionId, L["Users_Security"], string.Empty, string.Empty, State.ActiveSection == "security", false),
        new(SystemSectionId, L["Users_SystemInfo"], string.Empty, string.Empty, State.ActiveSection == "system", false)
    ];

    public async ValueTask FocusUserNameAsync()
    {
        State.ActiveSection = "basic";
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        if (_userNameInput is not null) await _userNameInput.FocusAsync();
    }

    private Task SelectApplicationSectionAsync(Guid tabId)
    {
        State.ActiveSection = tabId == SecuritySectionId
            ? "security"
            : tabId == SystemSectionId
                ? "system"
                : "basic";
        return Task.CompletedTask;
    }

    private static string FormatDate(DateTimeOffset? value) => value?.ToLocalTime().ToString("g") ?? "—";
}
