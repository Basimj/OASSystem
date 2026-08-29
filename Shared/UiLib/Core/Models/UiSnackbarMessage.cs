using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Core.Models;

public sealed record UiSnackbarMessage(
    Guid Id,
    string Message,
    AlertTone Tone,
    int DurationMilliseconds,
    bool AutoClose);
