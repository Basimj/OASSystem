using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Domain.Identity.Constants;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Commands.SetUserRoles;

public sealed class SetUserRolesCommandHandler(IIdentityRepository repository, ICurrentUser currentUser)
    : IRequestHandler<SetUserRolesCommand, Guid>
{
    public async Task<Guid> Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        var target = await repository.GetUserAsync(request.UserId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);
        RowVersionCodec.EnsureMatches(target.User.RowVersion, request.Request.RowVersion);
        await UserManagementGuard.EnsureCanManageTargetAsync(target, currentUser, repository, cancellationToken);

        var requestedIds = request.Request.RoleIds.Distinct().ToArray();
        var requestedRoles = await repository.GetRolesByIdsAsync(requestedIds, cancellationToken);
        if (requestedRoles.Count != requestedIds.Length)
            throw new ConflictException("identity_role_not_found", "One or more roles do not exist.");

        var hadAdministrator = target.Roles.Any(IsAdministrator);
        var willHaveAdministrator = requestedRoles.Any(IsAdministrator);
        _ = await UserManagementGuard.GetActorAsync(currentUser, repository, cancellationToken);

        if (Guid.TryParse(currentUser.UserId, out var actorId) && actorId == target.User.Id && hadAdministrator && !willHaveAdministrator)
            throw new ConflictException("identity_cannot_remove_own_administrator_role", "The current user cannot remove their own Administrator role.");

        if (target.User.IsSuperAdmin && !willHaveAdministrator)
            throw new ConflictException("identity_super_admin_administrator_role_required", "A Super Administrator must retain the Administrator role.");

        await repository.ReplaceUserRolesAsync(target.User.Id, requestedIds, cancellationToken);
        repository.UpdateUser(target.User); // Bump RowVersion and audit stamp for role changes.
        return target.User.Id;
    }

    private static bool IsAdministrator(Role role) =>
        string.Equals(role.Name, IdentityRoleNames.Administrator, StringComparison.OrdinalIgnoreCase);
}
