using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeePager
{
    [Parameter] public int PageNumber { get; set; } = 1;
    [Parameter] public int TotalPages { get; set; }
    [Parameter] public long TotalCount { get; set; }
    [Parameter] public int PageSize { get; set; } = 12;
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    private bool HasPrevious => PageNumber > 1;
    private bool HasNext => PageNumber < TotalPages;
    private long FirstItem => TotalCount == 0 ? 0 : ((long)(PageNumber - 1) * PageSize) + 1;
    private long LastItem => Math.Min((long)PageNumber * PageSize, TotalCount);
    private IReadOnlyList<int> VisiblePages
    {
        get
        {
            if (TotalPages <= 0) return [];
            var start = Math.Max(1, PageNumber - 2);
            var end = Math.Min(TotalPages, start + 4);
            start = Math.Max(1, end - 4);
            return Enumerable.Range(start, end - start + 1).ToArray();
        }
    }

    private Task PreviousAsync() => HasPrevious ? OnPageChanged.InvokeAsync(PageNumber - 1) : Task.CompletedTask;
    private Task NextAsync() => HasNext ? OnPageChanged.InvokeAsync(PageNumber + 1) : Task.CompletedTask;
    private Task SelectPageAsync(int page) => page == PageNumber ? Task.CompletedTask : OnPageChanged.InvokeAsync(page);
}
