using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;

namespace OAS.Application.Identity.Profile.Queries.GetUserProfileImage;

public sealed class GetUserProfileImageQueryHandler(
    ICurrentUser currentUser,
    IIdentityRepository repository,
    IUserProfileImageStore store)
    : IRequestHandler<GetUserProfileImageQuery, UserProfileImageData?>
{
    public async Task<UserProfileImageData?> Handle(GetUserProfileImageQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var actorId))
            throw new ForbiddenException();

        if (actorId != request.UserId)
        {
            var target = await repository.GetUserAsync(request.UserId, false, cancellationToken);
            if (target is null) return null;
            await UserManagementGuard.EnsureCanManageTargetAsync(target, currentUser, repository, cancellationToken);
        }

        return await store.GetAsync(request.UserId, cancellationToken);
    }
}
