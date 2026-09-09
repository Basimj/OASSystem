using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;

namespace OAS.Application.Identity.Profile.Commands.SetMyProfileImage;

public sealed class SetMyProfileImageCommandHandler(ICurrentUser currentUser, IUserProfileImageStore store, TimeProvider timeProvider)
    : IRequestHandler<SetMyProfileImageCommand>
{
    public async Task Handle(SetMyProfileImageCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId)) throw new ForbiddenException();
        ProfileImagePolicy.EnsureValid(request.ContentType, request.Content);
        await store.SaveAsync(new UserProfileImageData(userId, request.ContentType, request.Content, timeProvider.GetUtcNow()), cancellationToken);
    }
}
