using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Media;

public partial class UiPictureBox
{
    [Parameter] public string? Src { get; set; }
    [Parameter] public string Alt { get; set; } = string.Empty;
    [Parameter] public string FallbackText { get; set; } = "OAS";
}
