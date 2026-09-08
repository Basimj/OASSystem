using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;

namespace OAS.Application.Features.Employees.Queries.ExportEmployees;

public sealed record ExportEmployeesQuery
    : IQuery<byte[]>,
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.View];
}