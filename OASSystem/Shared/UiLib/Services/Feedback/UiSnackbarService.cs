using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Services.Feedback;

public sealed class UiSnackbarService : IUiSnackbarService, IDisposable
{
    private readonly List<UiSnackbarMessage> _messages = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _dismissTimers = [];
    private UiSnackbarOptions _options = new();

    public event Action? Changed;
    public UiSnackbarOptions Options => _options;
    public IReadOnlyList<UiSnackbarMessage> Messages => _messages.ToArray();

    public void Configure(UiSnackbarOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Normalize();
        TrimOverflow();
        NotifyChanged();
    }

    public Guid Show(string message, AlertTone tone = AlertTone.Info, int? durationMilliseconds = null, bool autoClose = true)
    {
        if (string.IsNullOrWhiteSpace(message)) return Guid.Empty;

        var duration = Math.Clamp(durationMilliseconds ?? _options.DurationMilliseconds, 1000, 60000);
        var item = new UiSnackbarMessage(Guid.NewGuid(), message.Trim(), tone, duration, autoClose);
        _messages.Add(item);
        TrimOverflow();
        NotifyChanged();

        if (autoClose) _ = DismissLaterAsync(item.Id, duration);
        return item.Id;
    }

    public Guid Success(string message, int? durationMilliseconds = null) => Show(message, AlertTone.Success, durationMilliseconds);
    public Guid Error(string message, int? durationMilliseconds = null) => Show(message, AlertTone.Danger, durationMilliseconds);
    public Guid Warning(string message, int? durationMilliseconds = null) => Show(message, AlertTone.Warning, durationMilliseconds);
    public Guid Info(string message, int? durationMilliseconds = null) => Show(message, AlertTone.Info, durationMilliseconds);

    public void Dismiss(Guid id)
    {
        var removed = _messages.RemoveAll(x => x.Id == id) > 0;
        CancelTimer(id);
        if (removed) NotifyChanged();
    }

    public void Clear()
    {
        if (_messages.Count == 0) return;
        _messages.Clear();
        foreach (var timer in _dismissTimers.Values)
        {
            timer.Cancel();
            timer.Dispose();
        }
        _dismissTimers.Clear();
        NotifyChanged();
    }

    private async Task DismissLaterAsync(Guid id, int durationMilliseconds)
    {
        var cts = new CancellationTokenSource();
        _dismissTimers[id] = cts;
        try
        {
            await Task.Delay(durationMilliseconds, cts.Token);
            if (!cts.IsCancellationRequested) Dismiss(id);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
    }

    private void TrimOverflow()
    {
        while (_messages.Count > _options.MaxVisible)
        {
            var id = _messages[0].Id;
            _messages.RemoveAt(0);
            CancelTimer(id);
        }
    }

    private void CancelTimer(Guid id)
    {
        if (!_dismissTimers.Remove(id, out var cts)) return;
        cts.Cancel();
        cts.Dispose();
    }

    private void NotifyChanged() => Changed?.Invoke();

    public void Dispose()
    {
        foreach (var timer in _dismissTimers.Values)
        {
            timer.Cancel();
            timer.Dispose();
        }
        _dismissTimers.Clear();
    }
}
