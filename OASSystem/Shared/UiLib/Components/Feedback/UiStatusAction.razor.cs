using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Feedback;

public partial class UiStatusAction
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public string? StatusText { get; set; }
    [Parameter] public AlertTone Tone { get; set; } = AlertTone.Info;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool AlignToLabeledControl { get; set; }
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Secondary;
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private string CssClass => string.Join(' ', new[]
    {
        "ui-status-action",
        $"ui-status-action--{Tone.ToString().ToLowerInvariant()}",
        $"ui-status-action--{Size.ToString().ToLowerInvariant()}",
        AlignToLabeledControl ? "ui-status-action--labeled" : null
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private string IndicatorText => Tone == AlertTone.Warning ? "?" : string.Empty;
}
