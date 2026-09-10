using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Inputs;

public partial class UiLookup : IAsyncDisposable
{
    private readonly List<UiLookupItem> _items = [];
    private CancellationTokenSource? _searchCts;
    private string _query = string.Empty;
    private bool _open;
    private bool _loading;
    private string? _lastExternalValue;

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public UiLookupItem? SelectedItem { get; set; }
    [Parameter] public Func<string, CancellationToken, Task<IReadOnlyList<UiLookupItem>>>? SearchAsync { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string Placeholder { get; set; } = "ابحث...";
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public string EmptyText { get; set; } = "لا توجد نتائج.";
    [Parameter] public string ClearText { get; set; } = "مسح الاختيار";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public int DebounceMilliseconds { get; set; } = 250;
    [Parameter] public int MinimumSearchLength { get; set; } = 0;

    private string RootClass => Disabled ? "ui-lookup__control ui-lookup__control--disabled" : "ui-lookup__control";

    protected override void OnParametersSet()
    {
        if (string.Equals(_lastExternalValue, Value, StringComparison.Ordinal)) return;

        _lastExternalValue = Value;
        if (string.IsNullOrWhiteSpace(Value))
            _query = string.Empty;
        else if (SelectedItem is not null && string.Equals(SelectedItem.Value, Value, StringComparison.Ordinal))
            _query = SelectedItem.PrimaryText;
    }

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        _query = args.Value?.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(Value))
        {
            _lastExternalValue = null;
            await ValueChanged.InvokeAsync(null);
        }

        await QueueSearchAsync(_query);
    }

    private Task HandleFocusAsync(FocusEventArgs _) => QueueSearchAsync(_query);

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            _open = false;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    private async Task QueueSearchAsync(string query)
    {
        if (Disabled || SearchAsync is null) return;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (query.Trim().Length < MinimumSearchLength)
        {
            _items.Clear();
            _open = true;
            return;
        }

        try
        {
            _loading = true;
            _open = true;
            StateHasChanged();

            if (DebounceMilliseconds > 0)
                await Task.Delay(DebounceMilliseconds, token);

            var results = await SearchAsync(query.Trim(), token);
            if (token.IsCancellationRequested) return;

            _items.Clear();
            _items.AddRange(results);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                _loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task SelectAsync(UiLookupItem item)
    {
        if (item.Disabled) return;
        _query = item.PrimaryText;
        _open = false;
        _items.Clear();
        _lastExternalValue = item.Value;
        await ValueChanged.InvokeAsync(item.Value);
    }

    private async Task ClearAsync()
    {
        _query = string.Empty;
        _open = false;
        _items.Clear();
        _lastExternalValue = null;
        await ValueChanged.InvokeAsync(null);
    }

    public ValueTask DisposeAsync()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        return ValueTask.CompletedTask;
    }
}
