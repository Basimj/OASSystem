using MediatR;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Queries;

public sealed class GetUsersQueryHandler(IIdentityRepository repository)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await repository.ListUsersAsync(cancellationToken);
        return users.Select(record => new UserDto(
            record.User.Id,
            record.User.UserName,
            record.User.FirstName,
            record.User.LastName,
            record.User.DisplayName,
            record.User.Email,
            record.User.IsActive,
            record.User.IsSuperAdmin,
            record.User.MustChangePassword,
            record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(),
            record.User.CreatedAtUtc,
            record.User.LastLoginAtUtc)).ToArray();
    }
}
