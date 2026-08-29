namespace OAS.Contracts.Identity.Authentication;

public sealed record CurrentUserDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Roles,
    bool IsSuperAdmin,
    bool MustChangePassword,
    bool IsAuthenticated = true);
