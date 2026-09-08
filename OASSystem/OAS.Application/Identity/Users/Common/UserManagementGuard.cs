using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;

namespace OAS.Application.Identity.Users.Common;

internal static class UserManagementGuard
{
    public static async Task<IdentityUserRecord> GetActorAsync(
        ICurrentUser currentUser,
        IIdentityRepository repository,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var actorId))
            throw new ForbiddenException();

        return await repository.GetUserAsync(actorId, false, cancellationToken)
            ?? throw new ForbiddenException();
    }

    public static async Task EnsureCanManageTargetAsync(
        IdentityUserRecord target,
        ICurrentUser currentUser,
        IIdentityRepository repository,
        CancellationToken cancellationToken)
    {
        if (!target.User.IsSuperAdmin) return;
        var actor = await GetActorAsync(currentUser, repository, cancellationToken);
        if (!actor.User.IsSuperAdmin)
            throw new ForbiddenException("Super Administrator privileges are required.", "identity_super_admin_required");
    }
}
