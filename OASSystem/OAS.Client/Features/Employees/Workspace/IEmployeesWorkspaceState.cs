using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;

namespace OAS.Client.Features.Employees.Workspace;

public interface IEmployeesWorkspaceState
{
    Guid AllEmployeesTabId { get; }
    Guid ActiveTabId { get; set; }
    string? SearchText { get; set; }
    string? AppliedSearch { get; set; }
    int PageNumber { get; set; }
    EmployeeListFilter Filter { get; set; }
    PagedResult<EmployeeDto> Page { get; set; }
    bool HasLoadedPage { get; set; }
    IReadOnlyList<EmployeeWorkspaceTabState> EditorTabs { get; }
    EmployeeWorkspaceTabState? ActiveEditorTab { get; }
    EmployeeWorkspaceTabState GetOrCreateEmployeeTab(Guid employeeId);
    EmployeeWorkspaceTabState CreateNewTab(string employeeCode);
    EmployeeWorkspaceTabState? FindEditorTab(Guid tabId);
    bool RemoveEditorTab(Guid tabId);
    void ResetList();
}
