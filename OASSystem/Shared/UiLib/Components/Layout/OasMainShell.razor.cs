using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Layout;

public partial class OasMainShell : IDisposable
{
    private sealed class OpenShellTab
    {
        public required Guid Id { get; init; }
        public required UiShellModuleItem Module { get; init; }
        public required string LastHref { get; set; }
    }

    private readonly List<OpenShellTab> _openTabs = [];
    private Guid _activeTabId;
    private string _currentPath = "/";
    private bool isSidebarCollapsed;
    private bool isMobileMenuOpen;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<UiShellModuleItem> Modules { get; set; } = [];

    [Parameter]
    public bool IsAdministrator { get; set; }

    [Parameter]
    public string SystemName { get; set; } = "OAS System";

    [Parameter]
    public string DatabaseName { get; set; } = "Default";

    [Parameter]
    public string ConnectionStatus { get; set; } = "متصل";

    [Parameter]
    public string VersionText { get; set; } = "1.0.0";

    [Parameter]
    public EventCallback<MouseEventArgs> OnLogout { get; set; }

    private IEnumerable<UiShellModuleItem> VisibleModules =>
        Modules.Where(module => !module.AdministratorOnly || IsAdministrator);

    private IReadOnlyList<UiApplicationTabItem> ApplicationTabs =>
        _openTabs
            .Select(tab => new UiApplicationTabItem(
                tab.Id,
                tab.Module.Title,
                tab.LastHref,
                tab.Module.IconCssClass,
                tab.Id == _activeTabId,
                !string.Equals(tab.Module.Key, "dashboard", StringComparison.Ordinal)))
            .ToArray();

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += HandleLocationChanged;
        EnsureDashboardTab();
        SynchronizeWithLocation(Navigation.Uri);
    }

    protected override void OnParametersSet()
    {
        if (!IsAdministrator)
        {
            _openTabs.RemoveAll(tab => tab.Module.AdministratorOnly);
            if (_openTabs.All(tab => tab.Id != _activeTabId))
            {
                var dashboard = EnsureDashboardTab();
                _activeTabId = dashboard.Id;
            }
        }

        SynchronizeWithLocation(Navigation.Uri);
    }

    private OpenShellTab EnsureDashboardTab()
    {
        var dashboard = _openTabs.FirstOrDefault(tab => tab.Module.Key == "dashboard");
        if (dashboard is not null)
        {
            return dashboard;
        }

        var dashboardModule = Modules.FirstOrDefault(module => string.Equals(module.Key, "dashboard", StringComparison.Ordinal))
            ?? Modules.FirstOrDefault(module => string.Equals(module.Href, "/", StringComparison.Ordinal))
            ?? new UiShellModuleItem("dashboard", "لوحة التحكم", "/", "fa-solid fa-house");

        dashboard = new OpenShellTab
        {
            Id = Guid.NewGuid(),
            Module = dashboardModule,
            LastHref = "/"
        };
        _openTabs.Add(dashboard);
        return dashboard;
    }

    private OpenShellTab EnsureTab(UiShellModuleItem module)
    {
        var existing = _openTabs.FirstOrDefault(tab => tab.Module.Key == module.Key);
        if (existing is not null)
        {
            return existing;
        }

        var tab = new OpenShellTab
        {
            Id = Guid.NewGuid(),
            Module = module,
            LastHref = module.Href
        };
        _openTabs.Add(tab);
        return tab;
    }

    private async Task NavigateModuleAsync(UiShellModuleItem module)
    {
        if (module.AdministratorOnly && !IsAdministrator)
        {
            return;
        }

        var tab = EnsureTab(module);
        _activeTabId = tab.Id;
        _currentPath = tab.LastHref;
        isMobileMenuOpen = false;

        Navigation.NavigateTo(tab.LastHref);
        await InvokeAsync(StateHasChanged);
    }

    private Task SelectApplicationTabAsync(Guid tabId)
    {
        var tab = _openTabs.FirstOrDefault(item => item.Id == tabId);
        if (tab is null)
        {
            return Task.CompletedTask;
        }

        _activeTabId = tab.Id;
        Navigation.NavigateTo(tab.LastHref);
        return Task.CompletedTask;
    }

    private Task CloseApplicationTabAsync(Guid tabId)
    {
        var index = _openTabs.FindIndex(tab => tab.Id == tabId);
        if (index < 0)
        {
            return Task.CompletedTask;
        }

        var tab = _openTabs[index];
        if (string.Equals(tab.Module.Key, "dashboard", StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        var wasActive = tab.Id == _activeTabId;
        _openTabs.RemoveAt(index);

        if (!wasActive)
        {
            return InvokeAsync(StateHasChanged);
        }

        var fallbackIndex = Math.Clamp(index - 1, 0, _openTabs.Count - 1);
        var fallback = _openTabs[fallbackIndex];
        _activeTabId = fallback.Id;
        Navigation.NavigateTo(fallback.LastHref);
        return Task.CompletedTask;
    }

    private bool IsModuleActive(UiShellModuleItem module)
    {
        if (module.Href == "/")
        {
            return _currentPath == "/";
        }

        return _currentPath.Equals(module.Href, StringComparison.OrdinalIgnoreCase)
            || _currentPath.StartsWith(module.Href + "/", StringComparison.OrdinalIgnoreCase);
    }

    private void SynchronizeWithLocation(string absoluteUri)
    {
        _currentPath = NormalizePath(absoluteUri);
        var module = FindModule(_currentPath);
        if (module is null || (module.AdministratorOnly && !IsAdministrator))
        {
            return;
        }

        var tab = EnsureTab(module);
        tab.LastHref = _currentPath;
        _activeTabId = tab.Id;
    }

    private UiShellModuleItem? FindModule(string path)
    {
        if (path == "/")
        {
            return Modules.FirstOrDefault(module => module.Href == "/");
        }

        return Modules
            .Where(module => module.Href != "/")
            .OrderByDescending(module => module.Href.Length)
            .FirstOrDefault(module =>
                path.Equals(module.Href, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(module.Href + "/", StringComparison.OrdinalIgnoreCase));
    }

    private string NormalizePath(string absoluteUri)
    {
        var relative = Navigation.ToBaseRelativePath(absoluteUri);
        var queryIndex = relative.IndexOfAny(['?', '#']);
        if (queryIndex >= 0)
        {
            relative = relative[..queryIndex];
        }

        relative = relative.Trim('/');
        return string.IsNullOrWhiteSpace(relative) ? "/" : "/" + relative;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _ = InvokeAsync(() =>
        {
            SynchronizeWithLocation(args.Location);
            isMobileMenuOpen = false;
            StateHasChanged();
        });
    }

    private void ToggleSidebar() => isSidebarCollapsed = !isSidebarCollapsed;

    private void ToggleMobileMenu() => isMobileMenuOpen = !isMobileMenuOpen;

    private void CloseMobileMenu() => isMobileMenuOpen = false;

    private async Task LogoutAsync(MouseEventArgs args)
    {
        if (OnLogout.HasDelegate)
        {
            await OnLogout.InvokeAsync(args);
        }
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
    }
}
