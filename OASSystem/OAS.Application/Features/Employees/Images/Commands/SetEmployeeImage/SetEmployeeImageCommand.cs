using OAS.Application.Abstractions.Messaging;
using OAS.Application.Features.Employees.Authorization;

namespace OAS.Application.Features.Employees.Images.Commands.SetEmployeeImage;

public sealed record SetEmployeeImageCommand(Guid EmployeeId, string ContentType, byte[] Content)
    : ICommand, INonTransactionalCommandBase, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [EmployeePermissions.Edit];
}
