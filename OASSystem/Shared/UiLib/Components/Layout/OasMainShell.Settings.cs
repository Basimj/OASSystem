using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Layout;

public partial class OasMainShell
{
    private const string SettingsContextKey = "oas-settings-pages";
    private bool _settingsGroupExpanded = true;

    private Task ToggleSettingsGroupAsync()
    {
        _topHeader?.CloseAccountMenu();
        if (isSidebarCollapsed)
        {
            _contextGroupKey = _contextGroupKey == SettingsContextKey ? null : SettingsContextKey;
        }
        else
        {
            _settingsGroupExpanded = !_settingsGroupExpanded;
        }
        return Task.CompletedTask;
    }

    private IReadOnlyList<UiContextMenuItem> BuildSettingsContextItems()
    {
        var result = new List<UiContextMenuItem>
        {
            new("settings-home", "الإعدادات العامة", "fa-solid fa-sliders", _currentPath.Equals("/settings", StringComparison.OrdinalIgnoreCase)),
            new("settings-appearance", "المظهر", "fa-solid fa-palette", _currentPath.StartsWith("/settings/appearance", StringComparison.OrdinalIgnoreCase))
        };
        result.AddRange(VisibleModules.Where(m => m.Key is "users" or "roles")
            .Select(m => new UiContextMenuItem(m.Key,
                m.Key == "users" ? "إدارة المستخدمين" : "الأدوار والصلاحيات",
                m.IconCssClass, IsModuleActive(m))));
        return result;
    }

    private async Task SelectSettingsContextItemAsync(string key)
    {
        _contextGroupKey = null;
        var settings = VisibleModules.FirstOrDefault(m =>
            string.Equals(m.Href, "/settings", StringComparison.OrdinalIgnoreCase));
        if (key == "settings-home" && settings is not null)
            await NavigateSettingsRouteAsync(settings, "/settings");
        else if (key == "settings-appearance" && settings is not null)
            await NavigateSettingsRouteAsync(settings, "/settings/appearance");
        else
        {
            var module = VisibleModules.FirstOrDefault(m => m.Key == key);
            if (module is not null) await NavigateModuleAsync(module);
        }
    }

    private Task NavigateSettingsHomeAsync() => NavigateSettingsLinkAsync("/settings");
    private Task NavigateSettingsAppearanceAsync() => NavigateSettingsLinkAsync("/settings/appearance");

    private async Task NavigateSettingsLinkAsync(string route)
    {
        var module = VisibleModules.FirstOrDefault(m => string.Equals(m.Href, "/settings", StringComparison.OrdinalIgnoreCase));
        if (module is not null) await NavigateSettingsRouteAsync(module, route);
    }

    private async Task NavigateSettingsRouteAsync(UiShellModuleItem settings, string path)
    {
        if (!IsVisible(settings)) return;
        _topHeader?.CloseAccountMenu();
        var tab = EnsureTab(settings);
        tab.LastHref = path;
        _activeTabId = tab.Id;
        _currentPath = path;
        isMobileMenuOpen = false;
        _contextGroupKey = null;
        await PersistStateAsync();
        Navigation.NavigateTo(path);
    }
}
