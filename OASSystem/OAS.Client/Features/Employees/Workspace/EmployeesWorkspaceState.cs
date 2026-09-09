using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;

namespace OAS.Client.Features.Employees.Workspace;

public sealed class EmployeesWorkspaceState : IEmployeesWorkspaceState
{
    public string? SearchText { get; set; }
    public string? AppliedSearch { get; set; }
    public int PageNumber { get; set; } = 1;
    public EmployeeListFilter Filter { get; set; } = EmployeeListFilter.All;
    public PagedResult<EmployeeDto> Page { get; set; } = new();
    public bool HasLoadedPage { get; set; }
    public EmployeeEditorWorkspaceState Editor { get; } = new();

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
