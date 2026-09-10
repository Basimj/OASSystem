using MediatR;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Roles;

namespace OAS.Application.Identity.Roles.Queries;

public sealed class GetRolesQueryHandler(IIdentityRepository repository)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await repository.ListRolesAsync(cancellationToken);
        return roles.Select(x => new RoleDto(x.Id, x.Name, x.DisplayName, x.IsSystem)).ToArray();
    }
}
