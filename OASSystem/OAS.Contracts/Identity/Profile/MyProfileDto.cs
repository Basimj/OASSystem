namespace OAS.Contracts.Identity.Profile;

public sealed record MyProfileDto(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    string RowVersion);
