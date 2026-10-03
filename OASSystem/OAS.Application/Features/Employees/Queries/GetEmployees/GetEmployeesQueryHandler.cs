using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Features.Employees.Mapping;
using OAS.Application.Features.Employees.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(
    IReadRepository<Employee, Guid> employeeRepository,
    IReadRepository<JobTitle, Guid> jobTitleRepository,
    IReadRepository<Department, Guid> departmentRepository)
    : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    public async Task<PagedResult<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var jobTitles = await jobTitleRepository.ListAsync(cancellationToken: cancellationToken);
        var departments = await departmentRepository.ListAsync(cancellationToken: cancellationToken);
        var search = normalized.Search?.Trim();
        var matchingTitleIds = string.IsNullOrWhiteSpace(search) ? Array.Empty<Guid>() : jobTitles.Where(x => x.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Id).ToArray();
        var matchingDepartmentIds = string.IsNullOrWhiteSpace(search) ? Array.Empty<Guid>() : departments.Where(x => x.NameAr.Contains(search, StringComparison.CurrentCultureIgnoreCase) || x.DepartmentCode.Contains(search, StringComparison.OrdinalIgnoreCase) || (x.NameEn != null && x.NameEn.Contains(search, StringComparison.CurrentCultureIgnoreCase))).Select(x => x.Id).ToArray();

        var page = await employeeRepository.GetPageAsync(new EmployeePageSpecification(normalized, matchingTitleIds, matchingDepartmentIds), cancellationToken);
        var managerIds = page.Items.Where(x => x.ManagerEmployeeId.HasValue).Select(x => x.ManagerEmployeeId!.Value).Distinct().ToArray();
        IReadOnlyList<Employee> managers = managerIds.Length == 0
            ? Array.Empty<Employee>()
            : await employeeRepository.ListAsync(new Specification<Employee>().Where(x => managerIds.Contains(x.Id)), cancellationToken);
        var titleMap = jobTitles.ToDictionary(x => x.Id);
        var departmentMap = departments.ToDictionary(x => x.Id);
        var managerMap = managers.ToDictionary(x => x.Id);
        var items = page.Items.Where(x => titleMap.ContainsKey(x.JobTitleId)).Select(x => EmployeeMapping.ToDto(x, titleMap[x.JobTitleId], x.DepartmentId is Guid d && departmentMap.TryGetValue(d, out var dep) ? dep : null, x.ManagerEmployeeId is Guid m && managerMap.TryGetValue(m, out var mgr) ? mgr : null)).ToArray();
        return new PagedResult<EmployeeDto> { Items = items, PageNumber = normalized.PageNumber, PageSize = normalized.PageSize, TotalCount = page.TotalCount };
    }
}
