using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Services.Dialogs;

public sealed class UiDialogService : IUiDialogService
{
    private TaskCompletionSource<UiDialogResult>? _completion;

    public event Action? Changed;
    public UiDialogRequest? Current { get; private set; }

    public Task<UiDialogResult> ShowAsync<TComponent>(
        string? title = null,
        IReadOnlyDictionary<string, object>? parameters = null,
        UiDialogOptions? options = null) where TComponent : IComponent
    {
        return OpenAsync(new UiDialogRequest
        {
            Id = Guid.NewGuid(),
            Title = title,
            ComponentType = typeof(TComponent),
            Parameters = parameters?.ToDictionary(x => x.Key, x => x.Value),
            Options = options ?? new UiDialogOptions()
        });
    }

    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        AlertTone tone = AlertTone.Warning,
        string? confirmText = null,
        string? cancelText = null,
        UiDialogOptions? options = null)
    {
        var result = await OpenAsync(new UiDialogRequest
        {
            Id = Guid.NewGuid(),
            Title = title,
            Message = message,
            IsConfirm = true,
            ConfirmText = confirmText,
            CancelText = cancelText,
            Tone = tone,
            Options = options ?? new UiDialogOptions { Size = UiDialogSize.Small }
        });
        return !result.Cancelled && result.Value is true;
    }

    public void Close(object? value = null) => Complete(UiDialogResult.Confirmed(value));
    public void Cancel() => Complete(UiDialogResult.Canceled());

    private Task<UiDialogResult> OpenAsync(UiDialogRequest request)
    {
        if (_completion is not null) Complete(UiDialogResult.Canceled());
        _completion = new TaskCompletionSource<UiDialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Current = request;
        Changed?.Invoke();
        return _completion.Task;
    }

    private void Complete(UiDialogResult result)
    {
        var completion = _completion;
        if (completion is null) return;
        _completion = null;
        Current = null;
        Changed?.Invoke();
        completion.TrySetResult(result);
    }
}
