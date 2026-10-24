using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Data;

public partial class UiDataTable<TItem>
{
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];
    [Parameter] public IReadOnlyList<UiDataColumn<TItem>> Columns { get; set; } = [];
    [Parameter] public IReadOnlyList<string> Headers { get; set; } = [];
    [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }
    [Parameter] public EventCallback<TItem> OnRowClick { get; set; }
    [Parameter] public Func<TItem, bool>? IsRowSelected { get; set; }
    [Parameter] public string EmptyText { get; set; } = string.Empty;
    [Parameter] public bool UniformCellTypography { get; set; }
    [Parameter] public bool Compact { get; set; }
    [Parameter] public bool FitToContainer { get; set; }
    [Parameter] public bool SidebarTypography { get; set; }
    // Opt-in paging keeps large inventory lists within the workspace viewport.
    [Parameter] public int PageSize { get; set; }
    private int _requestedPage = 1;
    private bool IsPaged => PageSize > 0;
    private int TotalPages => !IsPaged ? 1 : Math.Max(1, (Items.Count + PageSize - 1) / PageSize);
    private int CurrentPage => Math.Clamp(_requestedPage, 1, TotalPages);
    private IReadOnlyList<TItem> PagedItems => !IsPaged ? Items : Items.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToArray();
    private void PreviousPage() => _requestedPage = Math.Max(1, CurrentPage - 1);
    private void NextPage() => _requestedPage = Math.Min(TotalPages, CurrentPage + 1);

    private int ColumnCount => Math.Max(1, Headers.Count > 0 ? Headers.Count : Columns.Count);

    private string ContainerCssClass => string.Join(' ', new[]
    {
        "ui-data-table__container",
        FitToContainer ? "ui-data-table__container--fit" : null,
        IsPaged ? "ui-data-table__container--paged" : null
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private string TableCssClass => string.Join(' ', new[]
    {
        "ui-data-table",
        UniformCellTypography ? "ui-data-table--uniform-cell-typography" : null,
        Compact ? "ui-data-table--compact" : null,
        FitToContainer ? "ui-data-table--fit" : null,
        SidebarTypography ? "ui-data-table--sidebar-typography" : null
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private string GetRowCss(TItem item)
    {
        var clickable = OnRowClick.HasDelegate;
        var selected = IsRowSelected?.Invoke(item) == true;
        if (clickable && selected) return "ui-data-table__row ui-data-table__row--clickable ui-data-table__row--selected";
        if (clickable) return "ui-data-table__row ui-data-table__row--clickable";
        if (selected) return "ui-data-table__row ui-data-table__row--selected";
        return "ui-data-table__row";
    }

    private Task HandleRowClickAsync(TItem item) =>
        OnRowClick.HasDelegate ? OnRowClick.InvokeAsync(item) : Task.CompletedTask;
}
