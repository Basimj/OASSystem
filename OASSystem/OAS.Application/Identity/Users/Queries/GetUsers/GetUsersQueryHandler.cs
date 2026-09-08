using MediatR;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Mapping;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IIdentityRepository repository, TimeProvider timeProvider)
    : IRequestHandler<GetUsersQuery, PagedResult<UserSummaryDto>>
{
    public async Task<PagedResult<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var page = await repository.GetUsersPageAsync(normalized, request.IsActive, request.RoleId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        return new PagedResult<UserSummaryDto>
        {
            Items = page.Items.Select(x => UserMapping.ToSummary(x, now)).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
