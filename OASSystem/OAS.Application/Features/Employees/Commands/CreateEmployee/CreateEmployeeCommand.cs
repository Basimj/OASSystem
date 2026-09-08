using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees;

namespace OAS.Application.Features.Employees.Commands.CreateEmployee;

public sealed record CreateEmployeeCommand(
    CreateEmployeeRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [EmployeePermissions.Create];
}