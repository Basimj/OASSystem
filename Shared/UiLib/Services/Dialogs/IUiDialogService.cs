using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Services.Dialogs;

public interface IUiDialogService
{
    event Action? Changed;
    UiDialogRequest? Current { get; }

    Task<UiDialogResult> ShowAsync<TComponent>(
        string? title = null,
        IReadOnlyDictionary<string, object>? parameters = null,
        UiDialogOptions? options = null) where TComponent : IComponent;

    Task<bool> ConfirmAsync(
        string title,
        string message,
        AlertTone tone = AlertTone.Warning,
        string? confirmText = null,
        string? cancelText = null,
        UiDialogOptions? options = null);

    void Close(object? value = null);
    void Cancel();
}
