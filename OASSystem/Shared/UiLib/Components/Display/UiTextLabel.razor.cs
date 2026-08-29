using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Display;

public partial class UiTextLabel
{
    [Parameter] public string? For { get; set; }
    [Parameter] public string? Text { get; set; }
    [Parameter] public string? TextResourceKey { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;
    [Parameter] public TextWeight Weight { get; set; } = TextWeight.Medium;
    [Parameter] public TextTone Tone { get; set; } = TextTone.Default;
    [Parameter] public string? AdditionalCssClass { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private RenderFragment LabelContent => builder =>
    {
        if (ChildContent is not null) { builder.AddContent(0, ChildContent); return; }
        builder.AddContent(1, !string.IsNullOrWhiteSpace(TextResourceKey) ? L[TextResourceKey].Value : Text);
    };

    private string LabelCssClass => $"ui-text-label ui-text-label--size-{Size.ToString().ToLowerInvariant()} ui-text-label--weight-{Weight.ToString().ToLowerInvariant()} ui-text-label--tone-{Tone.ToString().ToLowerInvariant()} {AdditionalCssClass}".Trim();
    private IReadOnlyDictionary<string, object>? SafeAdditionalAttributes => AdditionalAttributes?.Where(x => !string.Equals(x.Key, "class", StringComparison.OrdinalIgnoreCase) && !string.Equals(x.Key, "for", StringComparison.OrdinalIgnoreCase)).ToDictionary(x => x.Key, x => x.Value);
}
