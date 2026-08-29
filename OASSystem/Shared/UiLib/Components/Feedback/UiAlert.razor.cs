using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Feedback;

public partial class UiAlert
{
    [Parameter] public string? Message { get; set; }
    [Parameter] public AlertTone Tone { get; set; } = AlertTone.Info;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private string CssClass => $"ui-alert ui-alert--{Tone.ToString().ToLowerInvariant()}";
}
