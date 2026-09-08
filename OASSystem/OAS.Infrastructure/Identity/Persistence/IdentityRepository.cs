using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Common.Pagination;
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

    public async Task<PagedData<IdentityUserRecord>> GetUsersPageAsync(
        PageRequest request,
        bool? isActive = null,
        Guid? roleId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Normalize();
        IQueryable<UserAccount> query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            var normalizedSearch = UserAccount.Normalize(search);
            query = query.Where(x =>
                x.UserName.Contains(search) ||
                x.FirstName.Contains(search) ||
                x.LastName.Contains(search) ||
                (x.Email != null && x.Email.Contains(search)) ||
                x.NormalizedUserName.Contains(normalizedSearch) ||
                (x.NormalizedEmail != null && x.NormalizedEmail.Contains(normalizedSearch)));
        }

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (roleId.HasValue)
        {
            var requestedRoleId = roleId.Value;
            query = query.Where(user => dbContext.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && userRole.RoleId == requestedRoleId));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        query = ApplySort(query, normalized.SortBy, normalized.SortDirection);

        var users = await query
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return new PagedData<IdentityUserRecord>([], totalCount);

        var ids = users.Select(x => x.Id).ToArray();
        var roleRows = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<Role>)x.Select(y => y.Role).OrderBy(r => r.Name).ToArray());

        var records = users.Select(user => new IdentityUserRecord(
            user,
            rolesByUser.TryGetValue(user.Id, out var roles) ? roles : [])).ToArray();

        return new PagedData<IdentityUserRecord>(records, totalCount);
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Roles.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(cancellationToken);

    public Task<Role?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0) return [];
        var ids = roleIds.Distinct().ToArray();
        return await dbContext.Roles.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
    }

    public Task<bool> UserNameExistsAsync(string normalizedUserName, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(
            x => x.NormalizedUserName == normalizedUserName && (!excludingUserId.HasValue || x.Id != excludingUserId.Value),
            cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(
            x => x.NormalizedEmail == normalizedEmail && (!excludingUserId.HasValue || x.Id != excludingUserId.Value),
            cancellationToken);

    public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(cancellationToken);

    public Task<bool> RoleExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default) =>
        dbContext.Roles.AsNoTracking().AnyAsync(x => x.NormalizedName == normalizedRoleName, cancellationToken);

    public Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().CountAsync(x => x.IsSuperAdmin && x.IsActive, cancellationToken);

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

    public async Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var requested = roleIds.Distinct().ToHashSet();
        var current = await dbContext.UserRoles.Where(x => x.UserId == userId).ToListAsync(cancellationToken);

        var toRemove = current.Where(x => !requested.Contains(x.RoleId)).ToArray();
        if (toRemove.Length > 0) dbContext.UserRoles.RemoveRange(toRemove);

        var existing = current.Select(x => x.RoleId).ToHashSet();
        var toAdd = requested.Where(roleId => !existing.Contains(roleId))
            .Select(roleId => UserRole.Create(Guid.NewGuid(), userId, roleId));
        await dbContext.UserRoles.AddRangeAsync(toAdd, cancellationToken);
    }

    public Task AddPasswordHistoryAsync(UserPasswordHistory history, CancellationToken cancellationToken = default) =>
        dbContext.UserPasswordHistory.AddAsync(history, cancellationToken).AsTask();

    public void UpdateUser(UserAccount user) => dbContext.Users.Update(user);

    private async Task<IReadOnlyList<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            orderby role.Name
            select role).ToListAsync(cancellationToken);
    }

    private static IQueryable<UserAccount> ApplySort(IQueryable<UserAccount> query, string? sortBy, SortDirection direction)
    {
        var descending = direction == SortDirection.Descending;
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "firstname" => descending ? query.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName) : query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName),
            "lastname" => descending ? query.OrderByDescending(x => x.LastName).ThenByDescending(x => x.FirstName) : query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName),
            "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "isactive" => descending ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.UserName) : query.OrderBy(x => x.IsActive).ThenBy(x => x.UserName),
            "lastloginatutc" => descending ? query.OrderByDescending(x => x.LastLoginAtUtc) : query.OrderBy(x => x.LastLoginAtUtc),
            "createdatutc" => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc),
            _ => descending ? query.OrderByDescending(x => x.UserName) : query.OrderBy(x => x.UserName)
        };
    }
}
