using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Dialogs;

public partial class UiConfirmDialog
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Message { get; set; } = string.Empty;
    [Parameter] public AlertTone Tone { get; set; } = AlertTone.Warning;
    [Parameter] public UiDialogSize Size { get; set; } = UiDialogSize.Small;
    [Parameter] public bool ShowCloseButton { get; set; } = true;
    [Parameter] public string? ConfirmText { get; set; }
    [Parameter] public string? CancelText { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string ResolvedConfirmText => ConfirmText ?? L["Confirm"].Value;
    private string ResolvedCancelText => CancelText ?? L["Cancel"].Value;
    private ButtonVariant ConfirmVariant => Tone == AlertTone.Danger ? ButtonVariant.Danger : ButtonVariant.Primary;
    private string MessageCssClass => $"ui-confirm-dialog__message ui-confirm-dialog__message--{Tone.ToString().ToLowerInvariant()}";
    private Task CloseAsync() => OnCancel.InvokeAsync();
    private Task ConfirmAsync(MouseEventArgs _) => OnConfirm.InvokeAsync();
    private Task CancelAsync(MouseEventArgs _) => OnCancel.InvokeAsync();
}
