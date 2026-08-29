using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Layout;

public partial class UiPage
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public PagePlacement Placement { get; set; } = PagePlacement.Start;

    private string CssClass => $"ui-page ui-page--{Placement.ToString().ToLowerInvariant()}";
}
