using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Data;

public partial class UiDataTable<TItem>
{
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];
    [Parameter] public IReadOnlyList<UiDataColumn<TItem>> Columns { get; set; } = [];
    [Parameter] public string EmptyText { get; set; } = string.Empty;
}
