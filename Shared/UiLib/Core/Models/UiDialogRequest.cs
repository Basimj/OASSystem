using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Core.Models;

public sealed record UiDialogRequest
{
    public required Guid Id { get; init; }
    public string? Title { get; init; }
    public Type? ComponentType { get; init; }
    public IDictionary<string, object>? Parameters { get; init; }
    public string? Message { get; init; }
    public bool IsConfirm { get; init; }
    public string? ConfirmText { get; init; }
    public string? CancelText { get; init; }
    public AlertTone Tone { get; init; } = AlertTone.Info;
    public UiDialogOptions Options { get; init; } = new();
}
