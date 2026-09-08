using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiWorkspaceLayout
{
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public RenderFragment? Tabs { get; set; }
    [Parameter] public RenderFragment? Filters { get; set; }
    [Parameter, EditorRequired] public RenderFragment? Content { get; set; }
    [Parameter] public RenderFragment? Aside { get; set; }
    [Parameter] public bool ShowAside { get; set; } = true;
    [Parameter] public bool ShowFilters { get; set; } = true;
}
