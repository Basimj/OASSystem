using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;

namespace OAS.Application.Features.Employees.Queries.GetEmployees;

public sealed record GetEmployeesQuery(
    PageRequest Request)
    : IQuery<PagedResult<EmployeeDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.View];
}