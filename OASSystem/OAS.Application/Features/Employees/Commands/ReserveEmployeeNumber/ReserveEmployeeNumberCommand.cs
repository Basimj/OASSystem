using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees;

namespace OAS.Application.Features.Employees.Commands.ReserveEmployeeNumber;

public sealed record ReserveEmployeeNumberCommand : ICommand<EmployeeNumberReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [EmployeePermissions.Create];
}
