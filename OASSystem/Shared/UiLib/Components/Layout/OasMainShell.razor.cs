using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace OAS.UiLib.Components.Layout;

public partial class OasMainShell
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool IsAdministrator { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnLogout { get; set; }

    private bool isSidebarCollapsed;

    private bool isMobileMenuOpen;

    private bool isSettingsOpen = true;


    private void ToggleSidebar()
    {
        isSidebarCollapsed = !isSidebarCollapsed;
    }


    private void ToggleMobileMenu()
    {
        isMobileMenuOpen = !isMobileMenuOpen;
    }


    private void CloseMobileMenu()
    {
        isMobileMenuOpen = false;
    }


    private void ToggleSettings()
    {
        isSettingsOpen = !isSettingsOpen;
    }


    private async Task LogoutAsync(MouseEventArgs args)
    {
        if (OnLogout.HasDelegate)
        {
            await OnLogout.InvokeAsync(args);
        }
    }
}