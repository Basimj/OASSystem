using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Layout;

public partial class UiContainer
{
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? Direction { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}
