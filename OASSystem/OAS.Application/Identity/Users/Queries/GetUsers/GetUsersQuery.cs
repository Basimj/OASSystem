using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Queries.GetUsers;

public sealed record GetUsersQuery(PageRequest Request, bool? IsActive = null, Guid? RoleId = null) : IQuery<PagedResult<UserSummaryDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.UsersView];
}
