using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Core.Models;

public sealed record UiDialogOptions
{
    public UiDialogSize Size { get; init; } = UiDialogSize.Medium;
    public bool CloseOnBackdrop { get; init; } = true;
    public bool CloseOnEscape { get; init; } = true;
    public bool ShowCloseButton { get; init; } = true;
}
