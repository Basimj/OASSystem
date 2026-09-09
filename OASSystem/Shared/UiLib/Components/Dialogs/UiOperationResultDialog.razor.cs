using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Services.Dialogs;

namespace OAS.UiLib.Components.Dialogs;

public partial class UiOperationResultDialog
{
    private bool _copied;
    [Inject] private IUiDialogService Dialogs { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Parameter] public UiOperationStatus Status { get; set; } = UiOperationStatus.Success;
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Message { get; set; }
    [Parameter] public string? Reason { get; set; }
    [Parameter] public string? SecretValue { get; set; }
    [Parameter] public string? SecretHint { get; set; }
    [Parameter] public string CopyText { get; set; } = "نسخ";
    [Parameter] public string CopiedText { get; set; } = "تم النسخ";
    [Parameter] public string CloseText { get; set; } = "إغلاق";

    private string StatusCss => Status.ToString().ToLowerInvariant();
    private string StatusIcon => Status switch
    {
        UiOperationStatus.Success => "fa-solid fa-check",
        UiOperationStatus.Failure => "fa-solid fa-xmark",
        UiOperationStatus.Warning => "fa-solid fa-exclamation",
        _ => "fa-solid fa-info"
    };

    private async Task CopyAsync()
    {
        if (string.IsNullOrWhiteSpace(SecretValue)) return;
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", SecretValue);
        _copied = true;
        StateHasChanged();
    }
    private Task CloseAsync(MouseEventArgs _) { Dialogs.Close(); return Task.CompletedTask; }
}
