using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Layout;

public partial class UiAppShell
{
    [Parameter] public string? Brand { get; set; }
    [Parameter] public RenderFragment? NavigationContent { get; set; }
    [Parameter] public RenderFragment? ActionsContent { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
