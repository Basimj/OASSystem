namespace OAS.Contracts.Identity.Users;

public sealed record CreateUserRequest(
    string UserName,
    string FirstName,
    string LastName,
    string? Email,
    string PhoneNumber,
    IReadOnlyList<Guid> RoleIds,
    bool IsActive = true);
