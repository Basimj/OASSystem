using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiSearchInput : IDisposable
{
    private CancellationTokenSource? _debounce;
    private string? _value;
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public EventCallback<string?> OnSearch { get; set; }
    [Parameter] public string Placeholder { get; set; } = "بحث...";
    [Parameter] public string AriaLabel { get; set; } = "بحث";
    [Parameter] public string ClearText { get; set; } = "مسح البحث";
    [Parameter] public int DebounceMilliseconds { get; set; } = 300;
    [Parameter] public bool Disabled { get; set; }

    protected override void OnParametersSet() { if (!string.Equals(Value, _value, StringComparison.Ordinal)) _value = Value; }

    private async Task InputChanged(ChangeEventArgs args)
    {
        if (Disabled) return;
        _value = args.Value?.ToString();
        await ValueChanged.InvokeAsync(_value);
        _debounce?.Cancel(); _debounce?.Dispose();
        _debounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(Math.Max(0, DebounceMilliseconds), _debounce.Token);
            await OnSearch.InvokeAsync(_value);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ClearAsync()
    {
        if (Disabled) return;
        _debounce?.Cancel();
        _value = string.Empty;
        await ValueChanged.InvokeAsync(_value);
        await OnSearch.InvokeAsync(_value);
    }

    public void Dispose() { _debounce?.Cancel(); _debounce?.Dispose(); GC.SuppressFinalize(this); }
}
