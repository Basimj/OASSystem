using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Workspace;

public partial class UiBadge
{
    [Parameter] public string Text { get; set; } = string.Empty;
    [Parameter] public AlertTone Tone { get; set; } = AlertTone.Info;
    private string CssClass => $"ui-badge ui-badge--{Tone.ToString().ToLowerInvariant()}";
}
