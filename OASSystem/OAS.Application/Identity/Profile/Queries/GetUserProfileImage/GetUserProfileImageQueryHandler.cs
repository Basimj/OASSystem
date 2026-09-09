using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;

namespace OAS.Application.Identity.Profile.Queries.GetUserProfileImage;

public sealed class GetUserProfileImageQueryHandler(ICurrentUser currentUser, IUserProfileImageStore store)
    : IRequestHandler<GetUserProfileImageQuery, UserProfileImageData?>
{
    public Task<UserProfileImageData?> Handle(GetUserProfileImageQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated) throw new ForbiddenException();
        return store.GetAsync(request.UserId, cancellationToken);
    }
}
