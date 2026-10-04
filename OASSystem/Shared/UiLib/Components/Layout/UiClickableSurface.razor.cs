using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace OAS.UiLib.Components.Layout;

public partial class UiClickableSurface
{
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private Task HandleClickAsync(MouseEventArgs args) =>
        Disabled ? Task.CompletedTask : OnClick.InvokeAsync(args);
}
