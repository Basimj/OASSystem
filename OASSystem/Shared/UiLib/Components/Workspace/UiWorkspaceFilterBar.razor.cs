using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiWorkspaceFilterBar
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
