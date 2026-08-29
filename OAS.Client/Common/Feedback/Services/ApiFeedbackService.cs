using Microsoft.Extensions.Localization;
using OAS.Client.Localization;
using OAS.Contracts.Common.Errors;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Common.Feedback.Services;

public sealed class ApiFeedbackService(
    IStringLocalizer<SharedResources> localizer,
    IUiSnackbarService snackbar) : IApiFeedbackService
{
    public void Show(ApiError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (string.Equals(error.Code, "validation_failed", StringComparison.OrdinalIgnoreCase) && error.Errors.Count > 0)
        {
            var codes = error.Errors.Values
                .SelectMany(x => x)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToArray();

            if (codes.Length > 0)
            {
                foreach (var code in codes) snackbar.Error(Translate(code));
                return;
            }
        }

        snackbar.Error(Translate(error.Code, error.Message));
    }

    public void ShowUnexpected() => snackbar.Error(Translate("unexpected_error"));

    private string Translate(string code, string? fallback = null)
    {
        var localized = localizer[code];
        if (!localized.ResourceNotFound) return localized.Value;
        return string.IsNullOrWhiteSpace(fallback) ? localizer["Error_Generic"].Value : fallback;
    }
}
