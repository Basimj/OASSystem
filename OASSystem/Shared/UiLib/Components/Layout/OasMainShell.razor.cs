using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Layout;

public partial class OasMainShell : IAsyncDisposable
{
    private sealed class OpenShellTab
    {
        public required Guid Id { get; init; }
        public required UiShellModuleItem Module { get; init; }
        public required string LastHref { get; set; }
    }

    private sealed class PersistedTab
    {
        public string ModuleKey { get; set; } = string.Empty;
        public string LastHref { get; set; } = string.Empty;
    }

    private sealed class PersistedShellState
    {
        public bool SidebarCollapsed { get; set; }
        public Dictionary<string, bool> Groups { get; set; } = new(StringComparer.Ordinal);
        public List<PersistedTab> Tabs { get; set; } = [];
        public string? ActiveModuleKey { get; set; }
    }

    private readonly List<OpenShellTab> _openTabs = [];
    private readonly Dictionary<string, bool> _groupExpanded = new(StringComparer.Ordinal);
    private Guid _activeTabId;
    private string _currentPath = "/workspace";
    private bool isSidebarCollapsed;
    private bool isMobileMenuOpen;
    private bool _restored;
    private string? _contextGroupKey;

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter, EditorRequired] public IReadOnlyList<UiShellModuleItem> Modules { get; set; } = [];
    [Parameter] public IReadOnlyList<UiShellNavigationGroup> NavigationGroups { get; set; } = [];
    [Parameter] public bool IsAdministrator { get; set; }
    [Parameter] public string SystemName { get; set; } = "OAS System";
    [Parameter] public string? SystemLogoUrl { get; set; }
    [Parameter] public string DatabaseName { get; set; } = "Default";
    [Parameter] public string ConnectionStatus { get; set; } = "متصل";
    [Parameter] public string VersionText { get; set; } = "1.0.0";
    [Parameter] public string ShellStateStorageKey { get; set; } = "oas.ui.shell.v2";
    [Parameter] public string UserDisplayName { get; set; } = "المستخدم";
    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public string? UserEmail { get; set; }
    [Parameter] public string? UserImageUrl { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnEditProfile { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnSettings { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnSupport { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnLogout { get; set; }

    private IEnumerable<UiShellModuleItem> VisibleModules => Modules.Where(IsVisible);
    private IEnumerable<UiShellModuleItem> VisibleUngroupedModules
    {
        get
        {
            var grouped = NavigationGroups.SelectMany(x => x.ModuleKeys).ToHashSet(StringComparer.Ordinal);
            return VisibleModules.Where(module => !grouped.Contains(module.Key));
        }
    }

    private IEnumerable<UiShellNavigationGroup> VisibleNavigationGroups =>
        NavigationGroups.Where(group => GetVisibleGroupModules(group).Count > 0);

    private IReadOnlyList<UiApplicationTabItem> ApplicationTabs => _openTabs
        .Select(tab => new UiApplicationTabItem(tab.Id, tab.Module.Title, tab.LastHref, tab.Module.IconCssClass, tab.Id == _activeTabId, true))
        .ToArray();

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += HandleLocationChanged;
        _currentPath = NormalizePath(Navigation.Uri);
    }

    protected override void OnParametersSet()
    {
        foreach (var group in NavigationGroups)
            _groupExpanded.TryAdd(group.Key, group.DefaultExpanded);

        var removedActive = false;
        foreach (var tab in _openTabs.Where(tab => !IsVisible(tab.Module)).ToArray())
        {
            if (tab.Id == _activeTabId) removedActive = true;
            _openTabs.Remove(tab);
        }

        if (removedActive)
            _activeTabId = _openTabs.LastOrDefault()?.Id ?? Guid.Empty;

        if (_restored) SynchronizeWithLocation(Navigation.Uri);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await RestoreStateAsync();
        _restored = true;
        SynchronizeWithLocation(Navigation.Uri);
        await PersistStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private bool IsVisible(UiShellModuleItem module) => !module.AdministratorOnly || IsAdministrator;

    private IReadOnlyList<UiShellModuleItem> GetVisibleGroupModules(UiShellNavigationGroup group)
    {
        var keys = group.ModuleKeys.ToHashSet(StringComparer.Ordinal);
        return VisibleModules.Where(x => keys.Contains(x.Key)).ToArray();
    }

    private bool IsGroupExpanded(UiShellNavigationGroup group) =>
        _groupExpanded.TryGetValue(group.Key, out var expanded) ? expanded : group.DefaultExpanded;

    private OpenShellTab EnsureTab(UiShellModuleItem module)
    {
        var existing = _openTabs.FirstOrDefault(tab => string.Equals(tab.Module.Key, module.Key, StringComparison.Ordinal));
        if (existing is not null) return existing;
        var tab = new OpenShellTab { Id = Guid.NewGuid(), Module = module, LastHref = module.Href };
        _openTabs.Add(tab);
        return tab;
    }

    private async Task NavigateModuleAsync(UiShellModuleItem module)
    {
        if (!IsVisible(module)) return;
        var tab = EnsureTab(module);
        _activeTabId = tab.Id;
        _currentPath = tab.LastHref;
        isMobileMenuOpen = false;
        _contextGroupKey = null;
        await PersistStateAsync();
        Navigation.NavigateTo(tab.LastHref);
    }

    private async Task SelectApplicationTabAsync(Guid tabId)
    {
        var tab = _openTabs.FirstOrDefault(item => item.Id == tabId);
        if (tab is null) return;
        _activeTabId = tab.Id;
        _contextGroupKey = null;
        await PersistStateAsync();
        Navigation.NavigateTo(tab.LastHref);
    }

    private async Task CloseApplicationTabAsync(Guid tabId)
    {
        var index = _openTabs.FindIndex(tab => tab.Id == tabId);
        if (index < 0) return;
        var wasActive = _openTabs[index].Id == _activeTabId;
        _openTabs.RemoveAt(index);

        if (wasActive)
        {
            if (_openTabs.Count == 0)
            {
                _activeTabId = Guid.Empty;
                _currentPath = "/workspace";
                await PersistStateAsync();
                Navigation.NavigateTo("/workspace");
                return;
            }

            var fallback = _openTabs[Math.Clamp(index - 1, 0, _openTabs.Count - 1)];
            _activeTabId = fallback.Id;
            await PersistStateAsync();
            Navigation.NavigateTo(fallback.LastHref);
            return;
        }

        await PersistStateAsync();
        StateHasChanged();
    }

    private bool IsModuleActive(UiShellModuleItem module)
    {
        if (module.Href == "/") return _currentPath == "/";
        return _currentPath.Equals(module.Href, StringComparison.OrdinalIgnoreCase)
            || _currentPath.StartsWith(module.Href + "/", StringComparison.OrdinalIgnoreCase);
    }

    private void SynchronizeWithLocation(string absoluteUri)
    {
        _currentPath = NormalizePath(absoluteUri);
        if (string.Equals(_currentPath, "/workspace", StringComparison.OrdinalIgnoreCase))
        {
            _activeTabId = Guid.Empty;
            return;
        }

        var module = FindModule(_currentPath);
        if (module is null || !IsVisible(module)) return;
        var tab = EnsureTab(module);
        tab.LastHref = _currentPath;
        _activeTabId = tab.Id;
    }

    private UiShellModuleItem? FindModule(string path)
    {
        if (path == "/") return Modules.FirstOrDefault(module => module.Href == "/");
        return Modules.Where(module => module.Href != "/").OrderByDescending(module => module.Href.Length)
            .FirstOrDefault(module => path.Equals(module.Href, StringComparison.OrdinalIgnoreCase)
                                   || path.StartsWith(module.Href + "/", StringComparison.OrdinalIgnoreCase));
    }

    private string NormalizePath(string absoluteUri)
    {
        var relative = Navigation.ToBaseRelativePath(absoluteUri);
        var queryIndex = relative.IndexOfAny(['?', '#']);
        if (queryIndex >= 0) relative = relative[..queryIndex];
        relative = relative.Trim('/');
        return string.IsNullOrWhiteSpace(relative) ? "/" : "/" + relative;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _ = InvokeAsync(async () =>
        {
            SynchronizeWithLocation(args.Location);
            isMobileMenuOpen = false;
            _contextGroupKey = null;
            await PersistStateAsync();
            StateHasChanged();
        });
    }

    private async Task ToggleSidebarAsync()
    {
        isSidebarCollapsed = !isSidebarCollapsed;
        _contextGroupKey = null;
        await PersistStateAsync();
    }

    private async Task HandleGroupClickAsync(UiShellNavigationGroup group)
    {
        if (isSidebarCollapsed)
        {
            _contextGroupKey = string.Equals(_contextGroupKey, group.Key, StringComparison.Ordinal) ? null : group.Key;
            return;
        }
        _groupExpanded[group.Key] = !IsGroupExpanded(group);
        await PersistStateAsync();
    }

    private IReadOnlyList<UiContextMenuItem> BuildContextItems(UiShellNavigationGroup group) =>
        GetVisibleGroupModules(group).Select(module => new UiContextMenuItem(module.Key, module.Title, module.IconCssClass, IsModuleActive(module))).ToArray();

    private async Task SelectCollapsedGroupItemAsync(string moduleKey)
    {
        var module = VisibleModules.FirstOrDefault(x => string.Equals(x.Key, moduleKey, StringComparison.Ordinal));
        if (module is not null) await NavigateModuleAsync(module);
        _contextGroupKey = null;
    }

    private void ToggleMobileMenu() => isMobileMenuOpen = !isMobileMenuOpen;
    private void CloseMobileMenu() => isMobileMenuOpen = false;
    private void CloseTransientMenus() => _contextGroupKey = null;

    private async Task RestoreStateAsync()
    {
        try
        {
            var json = await JS.InvokeAsync<string?>("localStorage.getItem", ShellStateStorageKey);
            if (string.IsNullOrWhiteSpace(json)) return;
            var state = JsonSerializer.Deserialize<PersistedShellState>(json);
            if (state is null) return;

            isSidebarCollapsed = state.SidebarCollapsed;
            foreach (var pair in state.Groups)
                if (_groupExpanded.ContainsKey(pair.Key)) _groupExpanded[pair.Key] = pair.Value;

            _openTabs.Clear();
            foreach (var persisted in state.Tabs)
            {
                var module = Modules.FirstOrDefault(x => string.Equals(x.Key, persisted.ModuleKey, StringComparison.Ordinal));
                if (module is null || !IsVisible(module)) continue;
                _openTabs.Add(new OpenShellTab
                {
                    Id = Guid.NewGuid(),
                    Module = module,
                    LastHref = IsRouteOwnedByModule(persisted.LastHref, module) ? persisted.LastHref : module.Href
                });
            }

            var active = _openTabs.FirstOrDefault(x => string.Equals(x.Module.Key, state.ActiveModuleKey, StringComparison.Ordinal));
            _activeTabId = active?.Id ?? Guid.Empty;
        }
        catch
        {
            // Browser storage is an optional UI preference only.
        }
    }

    private async Task PersistStateAsync()
    {
        if (!_restored) return;
        try
        {
            var active = _openTabs.FirstOrDefault(x => x.Id == _activeTabId);
            var state = new PersistedShellState
            {
                SidebarCollapsed = isSidebarCollapsed,
                Groups = new Dictionary<string, bool>(_groupExpanded, StringComparer.Ordinal),
                ActiveModuleKey = active?.Module.Key,
                Tabs = _openTabs.Select(x => new PersistedTab { ModuleKey = x.Module.Key, LastHref = x.LastHref }).ToList()
            };
            await JS.InvokeVoidAsync("localStorage.setItem", ShellStateStorageKey, JsonSerializer.Serialize(state));
        }
        catch
        {
            // UI state persistence must never block navigation.
        }
    }

    private static bool IsRouteOwnedByModule(string? href, UiShellModuleItem module)
    {
        if (string.IsNullOrWhiteSpace(href)) return false;
        if (module.Href == "/") return href == "/";
        return href.Equals(module.Href, StringComparison.OrdinalIgnoreCase) || href.StartsWith(module.Href + "/", StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
        await PersistStateAsync();
        GC.SuppressFinalize(this);
    }
}
