using System.Threading;
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
    private long _searchVersion;
    private bool _disposed;

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

    private string RootClass =>
        Disabled
            ? "ui-lookup__control ui-lookup__control--disabled"
            : "ui-lookup__control";

    protected override void OnParametersSet()
    {
        if (string.Equals(_lastExternalValue, Value, StringComparison.Ordinal))
            return;

        _lastExternalValue = Value;

        if (string.IsNullOrWhiteSpace(Value))
        {
            _query = string.Empty;
        }
        else if (SelectedItem is not null &&
                 string.Equals(SelectedItem.Value, Value, StringComparison.Ordinal))
        {
            _query = SelectedItem.PrimaryText;
        }
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

    private Task HandleFocusAsync(FocusEventArgs _) =>
        QueueSearchAsync(_query);

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
        if (_disposed || Disabled || SearchAsync is null)
            return;

        /*
         * Important:
         * The CTS is used only for the debounce delay.
         *
         * We deliberately do NOT pass this token to SearchAsync after the
         * HTTP request starts. Cancelling a previous lookup used to abort the
         * request inside API authorization/EF Core and produced normal
         * TaskCanceledException/OperationCanceledException exceptions on the
         * server. Those exceptions are expected cancellation semantics, but
         * they are noisy in the debugger and can look like application stops.
         *
         * Instead, every search gets a monotonically increasing version.
         * Older HTTP requests may finish, but their results are ignored when
         * a newer search has already started. This keeps debounce behavior
         * without aborting in-flight server work.
         */
        var version = Interlocked.Increment(ref _searchVersion);

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var debounceToken = _searchCts.Token;

        if (query.Trim().Length < MinimumSearchLength)
        {
            _items.Clear();
            _open = true;
            _loading = false;
            return;
        }

        try
        {
            _loading = true;
            _open = true;
            StateHasChanged();

            if (DebounceMilliseconds > 0)
            {
                await Task.Delay(
                    DebounceMilliseconds,
                    debounceToken);
            }

            if (!IsCurrentSearch(version, debounceToken))
                return;

            /*
             * Never tie a started HTTP request to the debounce CTS.
             * Stale results are rejected by the version check below.
             */
            var results = await SearchAsync(
                query.Trim(),
                CancellationToken.None);

            if (!IsCurrentSearch(version, debounceToken))
                return;

            _items.Clear();
            _items.AddRange(results);
        }
        catch (OperationCanceledException)
        {
            /*
             * Cancellation is not an application failure for a lookup.
             * This also protects the component from provider-level cancellation
             * such as component disposal or a cancelled navigation request.
             */
        }
        finally
        {
            if (!_disposed && version == Volatile.Read(ref _searchVersion))
            {
                _loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private bool IsCurrentSearch(
        long version,
        CancellationToken debounceToken) =>
        !_disposed &&
        !debounceToken.IsCancellationRequested &&
        version == Volatile.Read(ref _searchVersion);

    private async Task SelectAsync(UiLookupItem item)
    {
        if (item.Disabled)
            return;

        /*
         * Invalidate any older search result before committing the selected
         * value. An in-flight search may finish later, but it can no longer
         * overwrite the lookup items for this selection.
         */
        Interlocked.Increment(ref _searchVersion);
        _searchCts?.Cancel();

        _query = item.PrimaryText;
        _open = false;
        _items.Clear();
        _lastExternalValue = item.Value;

        await ValueChanged.InvokeAsync(item.Value);
    }

    private async Task ClearAsync()
    {
        Interlocked.Increment(ref _searchVersion);
        _searchCts?.Cancel();

        _query = string.Empty;
        _open = false;
        _items.Clear();
        _lastExternalValue = null;

        await ValueChanged.InvokeAsync(null);
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        Interlocked.Increment(ref _searchVersion);

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        return ValueTask.CompletedTask;
    }
}
