namespace OAS.Contracts.Identity.Users;

[Obsolete("Use UserSummaryDto or UserDetailsDto.")]
public sealed record UserDto(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Email,
    bool IsActive,
    bool IsSuperAdmin,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);
