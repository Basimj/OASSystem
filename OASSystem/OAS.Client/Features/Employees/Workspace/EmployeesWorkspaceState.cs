using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;

namespace OAS.Client.Features.Employees.Workspace;

public sealed class EmployeesWorkspaceState : IEmployeesWorkspaceState
{
    private static readonly Guid ListTabId = Guid.Parse("8c8c3ec7-8f57-4f91-8c96-f27c1e519e91");
    private readonly List<EmployeeWorkspaceTabState> _editorTabs = [];

    public Guid AllEmployeesTabId => ListTabId;
    public Guid ActiveTabId { get; set; } = ListTabId;
    public string? SearchText { get; set; }
    public string? AppliedSearch { get; set; }
    public int PageNumber { get; set; } = 1;
    public EmployeeListFilter Filter { get; set; } = EmployeeListFilter.All;
    public PagedResult<EmployeeDto> Page { get; set; } = new();
    public bool HasLoadedPage { get; set; }
    public IReadOnlyList<EmployeeWorkspaceTabState> EditorTabs => _editorTabs;
    public EmployeeWorkspaceTabState? ActiveEditorTab => FindEditorTab(ActiveTabId);

    public EmployeeWorkspaceTabState GetOrCreateEmployeeTab(Guid employeeId)
    {
        var existing = _editorTabs.FirstOrDefault(x => x.EmployeeId == employeeId);
        if (existing is not null) return existing;
        var tab = new EmployeeWorkspaceTabState(employeeId);
        _editorTabs.Add(tab);
        return tab;
    }

    public EmployeeWorkspaceTabState CreateNewTab(int employeeNumber)
    {
        var tab = new EmployeeWorkspaceTabState(null);
        tab.InitializeNew(employeeNumber);
        _editorTabs.Add(tab);
        return tab;
    }

    public EmployeeWorkspaceTabState? FindEditorTab(Guid tabId) =>
        _editorTabs.FirstOrDefault(x => x.TabId == tabId);

    public bool RemoveEditorTab(Guid tabId)
    {
        var tab = FindEditorTab(tabId);
        if (tab is null) return false;
        var removed = _editorTabs.Remove(tab);
        if (removed && ActiveTabId == tabId) ActiveTabId = ListTabId;
        return removed;
    }

    public void ResetList()
    {
        SearchText = null;
        AppliedSearch = null;
        PageNumber = 1;
        Filter = EmployeeListFilter.All;
        Page = new PagedResult<EmployeeDto>();
        HasLoadedPage = false;
    }
}
