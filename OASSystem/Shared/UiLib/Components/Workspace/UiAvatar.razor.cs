using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiAvatar
{
    private bool _imageFailed;
    [Parameter] public string Initials { get; set; } = string.Empty;
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string AltText { get; set; } = string.Empty;
    [Parameter] public int Size { get; set; } = 30;
    private string AvatarStyle => $"--ui-avatar-size:{Math.Clamp(Size, 22, 120)}px";
    protected override void OnParametersSet() => _imageFailed = false;
    private void ImageFailed() => _imageFailed = true;
}
