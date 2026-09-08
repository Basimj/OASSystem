using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Commands.SetUserStatus;

public sealed class SetUserStatusCommandHandler(IIdentityRepository repository, ICurrentUser currentUser)
    : IRequestHandler<SetUserStatusCommand, Guid>
{
    public async Task<Guid> Handle(SetUserStatusCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetUserAsync(request.UserId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);
        RowVersionCodec.EnsureMatches(record.User.RowVersion, request.Request.RowVersion);
        await UserManagementGuard.EnsureCanManageTargetAsync(record, currentUser, repository, cancellationToken);

        if (!request.Request.IsActive && Guid.TryParse(currentUser.UserId, out var actorId) && actorId == record.User.Id)
            throw new ConflictException("identity_cannot_disable_current_user", "The current user cannot disable their own account.");

        if (!request.Request.IsActive && record.User.IsSuperAdmin && record.User.IsActive &&
            await repository.CountActiveSuperAdminsAsync(cancellationToken) <= 1)
            throw new ConflictException("identity_last_super_admin_required", "At least one active Super Administrator is required.");

        record.User.SetActive(request.Request.IsActive);
        return record.User.Id;
    }
}
