using OAS.UiLib.Core.Enums;

namespace OAS.Client.Common.Feedback.Models;

public sealed record UiFeedbackSettings
{
    public UiSnackbarPosition SnackbarPosition { get; init; } = UiSnackbarPosition.TopRight;
    public int SnackbarDurationMilliseconds { get; init; } = 4000;
    public int SnackbarMaxVisible { get; init; } = 4;
}
