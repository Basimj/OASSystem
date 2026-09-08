using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees;

namespace OAS.Application.Features.Employees.Commands.SetEmployeeStatus;

public sealed record SetEmployeeStatusCommand(
    Guid EmployeeId,
    SetEmployeeStatusRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.Disable];
}