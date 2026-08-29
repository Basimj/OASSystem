using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.UiLib.Components.Feedback;

public partial class UiSnackbarHost : IDisposable
{
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    protected override void OnInitialized() => Snackbar.Changed += HandleChanged;

    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);

    private string HostCssClass => $"ui-snackbar-host ui-snackbar-host--{ToCssToken(Snackbar.Options.Position)}";

    private static string GetItemCssClass(UiSnackbarMessage item) =>
        $"ui-snackbar ui-snackbar--{item.Tone.ToString().ToLowerInvariant()}";

    private static string GetRole(AlertTone tone) => tone == AlertTone.Danger ? "alert" : "status";

    private static string ToCssToken(UiSnackbarPosition position) => position switch
    {
        UiSnackbarPosition.TopLeft => "top-left",
        UiSnackbarPosition.TopCenter => "top-center",
        UiSnackbarPosition.TopRight => "top-right",
        UiSnackbarPosition.CenterLeft => "center-left",
        UiSnackbarPosition.Center => "center",
        UiSnackbarPosition.CenterRight => "center-right",
        UiSnackbarPosition.BottomLeft => "bottom-left",
        UiSnackbarPosition.BottomCenter => "bottom-center",
        UiSnackbarPosition.BottomRight => "bottom-right",
        _ => "top-right"
    };

    public void Dispose() => Snackbar.Changed -= HandleChanged;
}
