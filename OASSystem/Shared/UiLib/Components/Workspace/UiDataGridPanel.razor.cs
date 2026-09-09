using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiDataGridPanel
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public RenderFragment? Toolbar { get; set; }
    [Parameter] public RenderFragment? Header { get; set; }
    [Parameter, EditorRequired] public RenderFragment? Body { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
}
