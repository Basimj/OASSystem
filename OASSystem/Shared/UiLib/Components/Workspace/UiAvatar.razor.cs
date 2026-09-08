using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiAvatar
{
    [Parameter] public string Initials { get; set; } = string.Empty;
}
