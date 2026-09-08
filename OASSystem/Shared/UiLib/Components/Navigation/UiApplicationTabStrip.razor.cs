using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Navigation;

public partial class UiApplicationTabStrip
{
    [Parameter]
    public IReadOnlyList<UiApplicationTabItem> Tabs { get; set; } = [];

    [Parameter]
    public EventCallback<Guid> OnSelect { get; set; }

    [Parameter]
    public EventCallback<Guid> OnClose { get; set; }

    private Task SelectAsync(Guid tabId) => OnSelect.InvokeAsync(tabId);

    private Task CloseAsync(Guid tabId) => OnClose.InvokeAsync(tabId);
}
