using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiWorkspaceFilterBar
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool Compact { get; set; }

    private string CssClass => Compact
        ? "ui-workspace-filterbar ui-workspace-filterbar--compact"
        : "ui-workspace-filterbar";
}
