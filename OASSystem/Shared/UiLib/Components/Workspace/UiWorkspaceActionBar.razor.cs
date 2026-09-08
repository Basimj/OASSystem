using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiWorkspaceActionBar
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
