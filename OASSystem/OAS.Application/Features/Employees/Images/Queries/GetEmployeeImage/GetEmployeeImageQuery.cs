using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;

namespace OAS.Application.Features.Employees.Images.Queries.GetEmployeeImage;

public sealed record GetEmployeeImageQuery(Guid EmployeeId) : IQuery<EmployeeImageData?>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [EmployeePermissions.View];
}
