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

    private int ColumnCount => Math.Max(1, Headers.Count > 0 ? Headers.Count : Columns.Count);
    private string ContainerCssClass => FitToContainer
        ? "ui-data-table__container ui-data-table__container--fit"
        : "ui-data-table__container";

    private string TableCssClass => string.Join(' ', new[]
    {
        "ui-data-table",
        UniformCellTypography ? "ui-data-table--uniform-cell-typography" : null,
        Compact ? "ui-data-table--compact" : null,
        FitToContainer ? "ui-data-table--fit" : null,
        SidebarTypography ? "ui-data-table--sidebar-typography" : null
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private string? GetRowCss(TItem item)
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
