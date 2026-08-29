using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Services.Feedback;

public interface IUiSnackbarService
{
    event Action? Changed;
    UiSnackbarOptions Options { get; }
    IReadOnlyList<UiSnackbarMessage> Messages { get; }

    void Configure(UiSnackbarOptions options);
    Guid Show(string message, AlertTone tone = AlertTone.Info, int? durationMilliseconds = null, bool autoClose = true);
    Guid Success(string message, int? durationMilliseconds = null);
    Guid Error(string message, int? durationMilliseconds = null);
    Guid Warning(string message, int? durationMilliseconds = null);
    Guid Info(string message, int? durationMilliseconds = null);
    void Dismiss(Guid id);
    void Clear();
}
