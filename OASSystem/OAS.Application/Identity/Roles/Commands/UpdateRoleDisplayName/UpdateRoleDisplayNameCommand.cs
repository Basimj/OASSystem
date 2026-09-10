using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Roles;

namespace OAS.Application.Identity.Roles.Commands.UpdateRoleDisplayName;

public sealed record UpdateRoleDisplayNameCommand(Guid RoleId, UpdateRoleDisplayNameRequest Request)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.RolesManage];
}
