using Microsoft.EntityFrameworkCore;
using OAS.Application.Identity.Abstractions;
using OAS.Domain.Identity.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Identity.Persistence;

public sealed class IdentityRepository(OasDbContext dbContext) : IIdentityRepository
{
    public async Task<IdentityUserRecord?> FindByLoginAsync(string login, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var normalized = UserAccount.Normalize(login);
        var query = forUpdate ? dbContext.Users.AsQueryable() : dbContext.Users.AsNoTracking();
        var user = await query.FirstOrDefaultAsync(
            x => x.NormalizedUserName == normalized || x.NormalizedEmail == normalized,
            cancellationToken);
        return user is null ? null : new IdentityUserRecord(user, await GetRolesForUserAsync(user.Id, cancellationToken));
    }

    public async Task<IdentityUserRecord?> GetUserAsync(Guid userId, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var query = forUpdate ? dbContext.Users.AsQueryable() : dbContext.Users.AsNoTracking();
        var user = await query.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? null : new IdentityUserRecord(user, await GetRolesForUserAsync(user.Id, cancellationToken));
    }

    public async Task<IReadOnlyList<IdentityUserRecord>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users.AsNoTracking().OrderBy(x => x.UserName).ToListAsync(cancellationToken);
        if (users.Count == 0) return [];

        var ids = users.Select(x => x.Id).ToArray();
        var roleRows = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<Role>)x.Select(y => y.Role).ToArray());

        return users.Select(user => new IdentityUserRecord(
            user,
            rolesByUser.TryGetValue(user.Id, out var roles) ? roles : [])).ToArray();
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Roles.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(cancellationToken);

    public Task<Role?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

    public Task<bool> UserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(x => x.NormalizedUserName == normalizedUserName, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(cancellationToken);

    public Task<bool> RoleExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default) =>
        dbContext.Roles.AsNoTracking().AnyAsync(x => x.NormalizedName == normalizedRoleName, cancellationToken);

    public async Task<IReadOnlyList<UserPasswordHistory>> GetPasswordHistoryAsync(Guid userId, int take, CancellationToken cancellationToken = default) =>
        await dbContext.UserPasswordHistory.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);

    public Task AddUserAsync(UserAccount user, CancellationToken cancellationToken = default) =>
        dbContext.Users.AddAsync(user, cancellationToken).AsTask();

    public Task AddRoleAsync(Role role, CancellationToken cancellationToken = default) =>
        dbContext.Roles.AddAsync(role, cancellationToken).AsTask();

    public Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default) =>
        dbContext.UserRoles.AddAsync(userRole, cancellationToken).AsTask();

    public Task AddPasswordHistoryAsync(UserPasswordHistory history, CancellationToken cancellationToken = default) =>
        dbContext.UserPasswordHistory.AddAsync(history, cancellationToken).AsTask();

    private async Task<IReadOnlyList<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            orderby role.Name
            select role).ToListAsync(cancellationToken);
    }
}
