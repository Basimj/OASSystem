using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Workspace;

public partial class UiDetailsHeader
{
    [Parameter] public string Initials { get; set; } = string.Empty;
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public string? StatusText { get; set; }
    [Parameter] public AlertTone StatusTone { get; set; } = AlertTone.Info;
}
