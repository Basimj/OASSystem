using OAS.Application.Abstractions.Security;
using OAS.Application.Identity.Abstractions;
using OAS.Domain.Identity.Constants;

namespace OAS.API.Security;

public sealed class ClaimsPermissionChecker(ICurrentUser currentUser, IIdentityRepository identityRepository) : IPermissionChecker
{
    public async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId)) return false;
        var record = await identityRepository.GetUserAsync(userId, false, cancellationToken);
        if (record is null || !record.User.IsActive) return false;
        if (record.Roles.Any(role => string.Equals(role.Name, IdentityRoleNames.Administrator, StringComparison.OrdinalIgnoreCase))) return true;
        return false; // Explicit role-permission assignments can be added here without changing feature callers.
    }
}
