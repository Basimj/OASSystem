using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Services.Dialogs;

namespace OAS.UiLib.Components.Dialogs;

public partial class UiDialogHost : IDisposable
{
    [Inject] private IUiDialogService Dialog { get; set; } = default!;

    protected override void OnInitialized() => Dialog.Changed += HandleChanged;
    private void HandleChanged() => _ = InvokeAsync(StateHasChanged);

    private void HandleBackdropClick()
    {
        if (Dialog.Current?.Options.CloseOnBackdrop == true) Dialog.Cancel();
    }

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && Dialog.Current?.Options.CloseOnEscape == true) Dialog.Cancel();
    }

    private Task CancelAsync()
    {
        Dialog.Cancel();
        return Task.CompletedTask;
    }

    private Task ConfirmAsync()
    {
        Dialog.Close(true);
        return Task.CompletedTask;
    }

    public void Dispose() => Dialog.Changed -= HandleChanged;
}
