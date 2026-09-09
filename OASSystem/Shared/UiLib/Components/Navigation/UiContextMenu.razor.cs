using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Navigation;

public partial class UiContextMenu
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public IReadOnlyList<UiContextMenuItem> Items { get; set; } = [];
    [Parameter] public EventCallback<string> OnSelect { get; set; }
    private Task SelectAsync(string key) => OnSelect.InvokeAsync(key);
}
