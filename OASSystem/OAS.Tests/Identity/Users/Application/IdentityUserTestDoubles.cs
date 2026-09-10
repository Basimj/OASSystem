using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Identity.Entities;

namespace OAS.Tests.Identity.Users.Application;

internal sealed class FakeCurrentUser(Guid? userId, bool isAuthenticated = true) : ICurrentUser
{
    public string? UserId { get; } = userId?.ToString();
    public bool IsAuthenticated { get; } = isAuthenticated;
}

internal sealed class FixedTemporaryPasswordGenerator(string password) : ITemporaryPasswordGenerator
{
    public string Generate() => password;
}

internal sealed class FakePasswordService : IPasswordService
{
    private const string Prefix = "hash::";

    public string Hash(UserAccount user, string password) => Prefix + password;

    public PasswordCheckResult Verify(UserAccount user, string password) =>
        VerifyHash(user, user.PasswordHash, password);

    public PasswordCheckResult VerifyHash(UserAccount user, string passwordHash, string password) =>
        string.Equals(passwordHash, Prefix + password, StringComparison.Ordinal)
            ? PasswordCheckResult.Success
            : PasswordCheckResult.Failed;

    public void SimulateVerification(string password) { }
}

internal sealed class FakeIdentityRepository : IIdentityRepository
{
    private readonly Dictionary<Guid, UserAccount> _users = [];
    private readonly Dictionary<Guid, Role> _roles = [];
    private readonly Dictionary<Guid, HashSet<Guid>> _userRoles = [];
    private readonly List<UserPasswordHistory> _passwordHistory = [];

    public IReadOnlyCollection<UserAccount> Users => _users.Values;
    public IReadOnlyCollection<UserPasswordHistory> PasswordHistory => _passwordHistory;

    public void SeedUser(UserAccount user, params Guid[] roleIds)
    {
        EnsureRowVersion(user);
        _users[user.Id] = user;
        _userRoles[user.Id] = roleIds.ToHashSet();
    }

    public void SeedRole(Role role) => _roles[role.Id] = role;

    public void SeedPasswordHistory(UserPasswordHistory item) => _passwordHistory.Add(item);

    public Task<IdentityUserRecord?> FindByLoginAsync(string login, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var normalized = UserAccount.Normalize(login);
        var user = _users.Values.FirstOrDefault(x =>
            x.NormalizedUserName == normalized || x.NormalizedEmail == normalized);
        return Task.FromResult(user is null ? null : ToRecord(user));
    }

    public Task<IdentityUserRecord?> GetUserAsync(Guid userId, bool forUpdate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.TryGetValue(userId, out var user) ? ToRecord(user) : null);

    public Task<PagedData<IdentityUserRecord>> GetUsersPageAsync(
        PageRequest request,
        bool? isActive = null,
        Guid? roleId = null,
        bool includeSuperAdmin = true,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<UserAccount> query = _users.Values;
        if (!includeSuperAdmin) query = query.Where(x => !x.IsSuperAdmin);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (roleId.HasValue) query = query.Where(x => RolesFor(x.Id).Any(role => role.Id == roleId.Value));
        var records = query.Select(ToRecord).ToArray();
        return Task.FromResult(new PagedData<IdentityUserRecord>(records, records.LongLength));
    }

    public Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>(_roles.Values.ToArray());

    public Task<Role?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.GetValueOrDefault(roleId));

    public Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>(roleIds.Distinct().Where(_roles.ContainsKey).Select(id => _roles[id]).ToArray());

    public Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.Distinct()
            .Where(id => _users.ContainsKey(id) && !_users[id].IsSuperAdmin)
            .ToDictionary(id => id, id => _users[id].UserName));

    public Task<bool> UserNameExistsAsync(string normalizedUserName, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.Any(x => x.NormalizedUserName == normalizedUserName && x.Id != excludingUserId));

    public Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludingUserId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.Any(x => x.NormalizedEmail == normalizedEmail && x.Id != excludingUserId));

    public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default) => Task.FromResult(_users.Count > 0);

    public Task<bool> RoleExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.Values.Any(x => x.NormalizedName == normalizedRoleName));

    public Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.Count(x => x.IsSuperAdmin && x.IsActive));

    public Task<IReadOnlyList<UserPasswordHistory>> GetPasswordHistoryAsync(Guid userId, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserPasswordHistory>>(_passwordHistory
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Max(1, take))
            .ToArray());

    public Task AddUserAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        EnsureRowVersion(user);
        _users[user.Id] = user;
        _userRoles.TryAdd(user.Id, []);
        return Task.CompletedTask;
    }

    public Task AddRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        _roles[role.Id] = role;
        return Task.CompletedTask;
    }

    public Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        if (!_userRoles.TryGetValue(userRole.UserId, out var roles))
        {
            roles = [];
            _userRoles[userRole.UserId] = roles;
        }
        roles.Add(userRole.RoleId);
        return Task.CompletedTask;
    }

    public Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        _userRoles[userId] = roleIds.Distinct().ToHashSet();
        return Task.CompletedTask;
    }

    public Task AddPasswordHistoryAsync(UserPasswordHistory history, CancellationToken cancellationToken = default)
    {
        _passwordHistory.Add(history);
        return Task.CompletedTask;
    }

    public void UpdateUser(UserAccount user) => _users[user.Id] = user;
    public void UpdateRole(Role role) => _roles[role.Id] = role;


    private static void EnsureRowVersion(UserAccount user)
    {
        if (user.RowVersion.Length > 0) return;

        var backingField = typeof(UserAccount).GetField("<RowVersion>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UserAccount RowVersion backing field was not found.");
        backingField.SetValue(user, BitConverter.GetBytes(1L));
    }

    private IdentityUserRecord ToRecord(UserAccount user) => new(user, RolesFor(user.Id));

    private IReadOnlyList<Role> RolesFor(Guid userId)
    {
        if (!_userRoles.TryGetValue(userId, out var ids)) return [];
        return ids.Where(_roles.ContainsKey).Select(id => _roles[id]).ToArray();
    }
}
