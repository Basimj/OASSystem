using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Core.Models;

public sealed record UiSnackbarOptions
{
    public UiSnackbarPosition Position { get; init; } = UiSnackbarPosition.TopRight;
    public int DurationMilliseconds { get; init; } = 4000;
    public int MaxVisible { get; init; } = 4;
    public bool ShowCloseButton { get; init; } = true;

    public UiSnackbarOptions Normalize() => this with
    {
        DurationMilliseconds = Math.Clamp(DurationMilliseconds, 1000, 60000),
        MaxVisible = Math.Clamp(MaxVisible, 1, 10)
    };
}
