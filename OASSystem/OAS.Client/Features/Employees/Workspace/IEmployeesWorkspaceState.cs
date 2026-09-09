using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;

namespace OAS.Client.Features.Employees.Workspace;

public interface IEmployeesWorkspaceState
{
    string? SearchText { get; set; }
    string? AppliedSearch { get; set; }
    int PageNumber { get; set; }
    EmployeeListFilter Filter { get; set; }
    PagedResult<EmployeeDto> Page { get; set; }
    bool HasLoadedPage { get; set; }
    EmployeeEditorWorkspaceState Editor { get; }
    void ResetList();
}
