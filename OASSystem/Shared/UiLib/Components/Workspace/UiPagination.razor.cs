using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiPagination
{
    [Parameter] public int PageNumber { get; set; } = 1;
    [Parameter] public int TotalPages { get; set; }
    [Parameter] public long TotalCount { get; set; }
    [Parameter] public EventCallback OnPrevious { get; set; }
    [Parameter] public EventCallback OnNext { get; set; }
    private bool HasPrevious => PageNumber > 1;
    private bool HasNext => PageNumber < TotalPages;
    private Task PreviousAsync() => HasPrevious ? OnPrevious.InvokeAsync() : Task.CompletedTask;
    private Task NextAsync() => HasNext ? OnNext.InvokeAsync() : Task.CompletedTask;
}
