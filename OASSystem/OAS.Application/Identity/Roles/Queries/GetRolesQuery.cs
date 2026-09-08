using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Roles;

namespace OAS.Application.Identity.Roles.Queries;

public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.UsersView];
}
