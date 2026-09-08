using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees;

namespace OAS.Application.Features.Employees.Commands.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    Guid EmployeeId,
    UpdateEmployeeRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.Edit];
}