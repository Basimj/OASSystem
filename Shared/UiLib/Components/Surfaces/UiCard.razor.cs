using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Surfaces;

public partial class UiCard
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public SurfaceWidth Width { get; set; } = SurfaceWidth.Full;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private string CssClass => $"ui-card ui-card--{Width.ToString().ToLowerInvariant()}";
}
