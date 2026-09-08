using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees;

namespace OAS.Application.Features.Employees.Queries.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(
    Guid EmployeeId)
    : IQuery<EmployeeDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.View];
}