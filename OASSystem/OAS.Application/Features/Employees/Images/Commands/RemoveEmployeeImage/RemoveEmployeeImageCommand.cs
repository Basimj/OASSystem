using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;

namespace OAS.Application.Features.Employees.Images.Commands.RemoveEmployeeImage;

public sealed record RemoveEmployeeImageCommand(Guid EmployeeId)
    : ICommand, INonTransactionalCommandBase, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [EmployeePermissions.Edit];
}
