using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Commands.UnlockUser;

public sealed class UnlockUserCommandHandler(IIdentityRepository repository, ICurrentUser currentUser)
    : IRequestHandler<UnlockUserCommand, Guid>
{
    public async Task<Guid> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var target = await repository.GetUserAsync(request.UserId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);
        RowVersionCodec.EnsureMatches(target.User.RowVersion, request.Request.RowVersion);
        await UserManagementGuard.EnsureCanManageTargetAsync(target, currentUser, repository, cancellationToken);

        target.User.ResetAccessFailures();
        repository.UpdateUser(target.User); // Keep unlock idempotent while still advancing RowVersion/audit.
        return target.User.Id;
    }
}
