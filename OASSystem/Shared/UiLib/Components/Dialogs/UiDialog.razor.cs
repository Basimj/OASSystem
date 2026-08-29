using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Dialogs;

public partial class UiDialog
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public UiDialogSize Size { get; set; } = UiDialogSize.Medium;
    [Parameter] public bool ShowCloseButton { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private string CssClass => $"ui-dialog ui-dialog--{Size.ToString().ToLowerInvariant()}";
    private Task CloseAsync(MouseEventArgs _) => OnClose.InvokeAsync();
}
