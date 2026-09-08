using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Identity.Users.Components;

public partial class TemporaryPasswordDialog
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IUiDialogService Dialogs { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Parameter] public string TemporaryPassword { get; set; } = string.Empty;
    [Parameter] public string Message { get; set; } = string.Empty;

    private async Task CopyAsync()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", TemporaryPassword);
        Snackbar.Success(L["Users_PasswordCopied"]);
    }

    private Task CloseAsync(MouseEventArgs _)
    {
        Dialogs.Close();
        return Task.CompletedTask;
    }
}
