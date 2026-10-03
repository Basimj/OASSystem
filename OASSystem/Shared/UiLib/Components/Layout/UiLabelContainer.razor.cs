using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Layout;

public partial class UiLabelContainer
{
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? For { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}
