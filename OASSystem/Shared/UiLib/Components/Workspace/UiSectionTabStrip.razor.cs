using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Workspace;

public partial class UiSectionTabStrip
{
    [Parameter] public IReadOnlyList<UiSectionTabItem> Tabs { get; set; } = [];
    [Parameter] public EventCallback<string> OnSelect { get; set; }
    private Task SelectAsync(string key) => OnSelect.InvokeAsync(key);
}
