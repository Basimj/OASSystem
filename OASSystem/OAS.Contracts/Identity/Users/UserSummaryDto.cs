namespace OAS.Contracts.Identity.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Email,
    bool IsActive,
    bool IsSuperAdmin,
    bool MustChangePassword,
    bool IsLocked,
    DateTimeOffset? LockoutEndUtc,
    DateTimeOffset? LastLoginAtUtc,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAtUtc,
    string RowVersion);
