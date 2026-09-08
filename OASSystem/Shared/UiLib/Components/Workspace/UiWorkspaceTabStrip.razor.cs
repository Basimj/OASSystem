using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Workspace;

public partial class UiWorkspaceTabStrip
{
    [Parameter] public IReadOnlyList<UiWorkspaceTabItem> Tabs { get; set; } = [];
    [Parameter] public EventCallback<Guid> OnSelect { get; set; }
    [Parameter] public EventCallback<Guid> OnClose { get; set; }
    private Task SelectAsync(Guid id) => OnSelect.InvokeAsync(id);
    private Task CloseAsync(Guid id) => OnClose.InvokeAsync(id);
}
