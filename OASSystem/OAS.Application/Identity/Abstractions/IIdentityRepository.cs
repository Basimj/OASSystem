using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Abstractions;

public interface IIdentityRepository
{
    Task<IdentityUserRecord?> FindByLoginAsync(string login, bool forUpdate, CancellationToken cancellationToken = default);
    Task<IdentityUserRecord?> GetUserAsync(Guid userId, bool forUpdate, CancellationToken cancellationToken = default);
    Task<PagedData<IdentityUserRecord>> GetUsersPageAsync(
        PageRequest request,
        bool? isActive = null,
        Guid? roleId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
    Task<bool> UserNameExistsAsync(string normalizedUserName, Guid? excludingUserId = null, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default);
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default);
    Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPasswordHistory>> GetPasswordHistoryAsync(Guid userId, int take, CancellationToken cancellationToken = default);
    Task AddUserAsync(UserAccount user, CancellationToken cancellationToken = default);
    Task AddRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
    Task AddPasswordHistoryAsync(UserPasswordHistory history, CancellationToken cancellationToken = default);
    void UpdateUser(UserAccount user);
}
