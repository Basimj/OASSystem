using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;

namespace OAS.Application.Identity.Users.Commands.RemoveUserProfileImage;

public sealed class RemoveUserProfileImageCommandHandler(IIdentityRepository repository, IUserProfileImageStore store, ICurrentUser currentUser) : IRequestHandler<RemoveUserProfileImageCommand>
{
    public async Task Handle(RemoveUserProfileImageCommand request, CancellationToken cancellationToken)
    {
        var target = await repository.GetUserAsync(request.UserId, false, cancellationToken) ?? throw new NotFoundException("UserAccount", request.UserId);
        await UserManagementGuard.EnsureCanManageTargetAsync(target, currentUser, repository, cancellationToken);
        await store.DeleteAsync(request.UserId, cancellationToken);
    }
}
