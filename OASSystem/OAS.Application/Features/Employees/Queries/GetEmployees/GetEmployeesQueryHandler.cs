using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Mapping;
using OAS.Application.Features.Employees.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(
    IReadRepository<Employee, Guid> employeeRepository,
    IReadRepository<JobTitle, Guid> jobTitleRepository)
    : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    public async Task<PagedResult<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var jobTitles = await jobTitleRepository.ListAsync(cancellationToken: cancellationToken);

        var matchingTitleIds = string.IsNullOrWhiteSpace(normalized.Search)
            ? Array.Empty<Guid>()
            : jobTitles
                .Where(x => x.Name.Contains(normalized.Search.Trim(), StringComparison.CurrentCultureIgnoreCase))
                .Select(x => x.Id)
                .ToArray();

        var page = await employeeRepository.GetPageAsync(
            new EmployeePageSpecification(normalized, matchingTitleIds),
            cancellationToken);

        var titleMap = jobTitles.ToDictionary(x => x.Id);
        var items = page.Items
            .Where(x => titleMap.ContainsKey(x.JobTitleId))
            .Select(x => EmployeeMapping.ToDto(x, titleMap[x.JobTitleId]))
            .ToArray();

        return new PagedResult<EmployeeDto>
        {
            Items = items,
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
