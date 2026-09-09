using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Profile;
using OAS.Application.Identity.Users.Common;

namespace OAS.Application.Identity.Users.Commands.SetUserProfileImage;

public sealed class SetUserProfileImageCommandHandler(IIdentityRepository repository, IUserProfileImageStore store, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<SetUserProfileImageCommand>
{
    public async Task Handle(SetUserProfileImageCommand request, CancellationToken cancellationToken)
    {
        var target = await repository.GetUserAsync(request.UserId, false, cancellationToken) ?? throw new NotFoundException("UserAccount", request.UserId);
        await UserManagementGuard.EnsureCanManageTargetAsync(target, currentUser, repository, cancellationToken);
        ProfileImagePolicy.EnsureValid(request.ContentType, request.Content);
        await store.SaveAsync(new UserProfileImageData(request.UserId, request.ContentType, request.Content, timeProvider.GetUtcNow()), cancellationToken);
    }
}
