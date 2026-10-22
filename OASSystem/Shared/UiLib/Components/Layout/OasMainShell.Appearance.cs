using Microsoft.JSInterop;

namespace OAS.UiLib.Components.Layout;

public partial class OasMainShell
{
    private Task ApplyStoredSidebarModeAsync(string mode)
    {
        var collapsed = string.Equals(mode, "Collapsed", StringComparison.Ordinal);
        if (isSidebarCollapsed == collapsed) return Task.CompletedTask;
        isSidebarCollapsed = collapsed;
        return InvokeAsync(StateHasChanged);
    }

    private async Task ToggleAppearanceSidebarAsync()
    {
        await ToggleSidebarAsync();
        try
        {
            await JS.InvokeVoidAsync("oasAppearanceInterop.persistSidebarMode",
                isSidebarCollapsed ? "Collapsed" : "Expanded");
        }
        catch (JSException) { /* Default UI toggle works without persistence. */ }
    }
}
