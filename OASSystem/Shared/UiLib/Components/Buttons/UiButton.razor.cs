using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Buttons;

public partial class UiButton
{
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public string? Text { get; set; }
    [Parameter] public string? TextResourceKey { get; set; }
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool FullWidth { get; set; }
    [Parameter] public string? AdditionalCssClass { get; set; }
    [Parameter] public string? IconCssClass { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string ButtonCssClass => string.Join(' ', new[]
    {
        "ui-button",
        $"ui-button--{Variant.ToString().ToLowerInvariant()}",
        $"ui-button--{Size.ToString().ToLowerInvariant()}",
        FullWidth ? "ui-button--full" : null,
        AdditionalCssClass
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private IReadOnlyDictionary<string, object>? SafeAdditionalAttributes => FilterAttributes(AdditionalAttributes, "class", "type", "disabled", "aria-busy");
    private Task HandleClickAsync(MouseEventArgs args) => Disabled || Loading ? Task.CompletedTask : OnClick.InvokeAsync(args);

    private static IReadOnlyDictionary<string, object>? FilterAttributes(IReadOnlyDictionary<string, object>? source, params string[] blocked)
    {
        if (source is null || source.Count == 0) return source;
        var blockedSet = blocked.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return source.Where(x => !blockedSet.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }
}
