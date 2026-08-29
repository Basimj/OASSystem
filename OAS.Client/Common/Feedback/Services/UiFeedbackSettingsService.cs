using OAS.Client.Common.Feedback.Models;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Common.Feedback.Services;

public sealed class UiFeedbackSettingsService(IUiSnackbarService snackbar) : IUiFeedbackSettingsService
{
    public UiFeedbackSettings Current { get; private set; } = new();

    public void Apply(UiFeedbackSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Current = settings with
        {
            SnackbarDurationMilliseconds = Math.Clamp(settings.SnackbarDurationMilliseconds, 1000, 60000),
            SnackbarMaxVisible = Math.Clamp(settings.SnackbarMaxVisible, 1, 10)
        };
        snackbar.Configure(new UiSnackbarOptions
        {
            Position = Current.SnackbarPosition,
            DurationMilliseconds = Current.SnackbarDurationMilliseconds,
            MaxVisible = Current.SnackbarMaxVisible,
            ShowCloseButton = true
        });
    }
}
