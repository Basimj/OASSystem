using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;

namespace OAS.Application.Identity.Profile.Commands.RemoveMyProfileImage;

public sealed class RemoveMyProfileImageCommandHandler(ICurrentUser currentUser, IUserProfileImageStore store) : IRequestHandler<RemoveMyProfileImageCommand>
{
    public async Task Handle(RemoveMyProfileImageCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId)) throw new ForbiddenException();
        await store.DeleteAsync(userId, cancellationToken);
    }
}
