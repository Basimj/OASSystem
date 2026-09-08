using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Application.Identity.Users.Mapping;
using OAS.Contracts.Identity.Users;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(
    IIdentityRepository repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await repository.GetUserAsync(request.UserId, false, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        await UserManagementGuard.EnsureCanManageTargetAsync(record, currentUser, repository, cancellationToken);
        return UserMapping.ToDetails(record, timeProvider.GetUtcNow());
    }
}
