using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Navigation;

public partial class UiApplicationTabStrip
{
    private Guid? _contextTabId;
    private double _contextX;
    private double _contextY;
    private string ContextMenuStyle => $"left:clamp(6px, {_contextX}px, calc(100vw - 184px));top:clamp(40px, {_contextY}px, calc(100vh - 112px))";

    [Parameter] public IReadOnlyList<UiApplicationTabItem> Tabs { get; set; } = [];
    [Parameter] public string AriaLabel { get; set; } = "تبويبات النظام";
    [Parameter] public bool EnableContextMenu { get; set; } = true;
    [Parameter] public EventCallback<Guid> OnSelect { get; set; }
    [Parameter] public EventCallback<Guid> OnClose { get; set; }
    [Parameter] public EventCallback<Guid> OnCloseOthers { get; set; }
    [Parameter] public EventCallback OnCloseAll { get; set; }

    private Task SelectAsync(Guid tabId)
    {
        CloseContextMenu();
        return OnSelect.InvokeAsync(tabId);
    }

    private Task CloseAsync(Guid tabId)
    {
        CloseContextMenu();
        return OnClose.InvokeAsync(tabId);
    }

    private void OpenContextMenu(Guid tabId, MouseEventArgs args)
    {
        if (!EnableContextMenu) return;
        _contextTabId = tabId;
        _contextX = args.ClientX;
        _contextY = args.ClientY;
    }

    public void DismissContextMenu()
    {
        if (!_contextTabId.HasValue) return;
        _contextTabId = null;
        _ = InvokeAsync(StateHasChanged);
    }
    private void CloseContextMenu() => DismissContextMenu();

    private async Task CloseCurrentFromMenuAsync()
    {
        if (_contextTabId is not Guid tabId) return;
        _contextTabId = null;
        await OnClose.InvokeAsync(tabId);
    }

    private async Task CloseOthersFromMenuAsync()
    {
        if (_contextTabId is not Guid tabId) return;
        _contextTabId = null;
        await OnCloseOthers.InvokeAsync(tabId);
    }

    private async Task CloseAllFromMenuAsync()
    {
        _contextTabId = null;
        await OnCloseAll.InvokeAsync();
    }
}
