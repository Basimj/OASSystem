namespace OAS.UiLib.Core.Models;

public sealed record UiDialogResult(bool Cancelled, object? Value)
{
    public static UiDialogResult Confirmed(object? value = null) => new(false, value);
    public static UiDialogResult Canceled() => new(true, null);
}
