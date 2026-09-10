using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Roles;

namespace OAS.Application.Identity.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(CreateRoleRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.RolesManage];
}
