namespace OAS.Contracts.Identity.Users;

public sealed record UserDetailsDto(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    bool IsSuperAdmin,
    bool MustChangePassword,
    int AccessFailedCount,
    DateTimeOffset? LockoutEndUtc,
    bool IsLocked,
    DateTimeOffset? LastLoginAtUtc,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy,
    string RowVersion);
