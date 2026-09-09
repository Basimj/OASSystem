namespace OAS.Contracts.Identity.Profile;

public sealed record MyProfileDto(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Roles,
    string RowVersion);
