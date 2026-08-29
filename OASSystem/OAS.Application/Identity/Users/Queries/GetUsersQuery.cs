using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Queries;

public sealed record GetUsersQuery : IQuery<IReadOnlyList<UserDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.UsersManage];
}
