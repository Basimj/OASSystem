using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Layout;

public partial class UiGrid
{
    [Parameter] public int Columns { get; set; } = 2;
    [Parameter] public GridRatio Ratio { get; set; } = GridRatio.Equal;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private string CssClass => $"ui-grid ui-grid--{Math.Clamp(Columns, 1, 4)} ui-grid--{Ratio.ToString().ToLowerInvariant()}";
}
