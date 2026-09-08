namespace OAS.Contracts.Identity.Users;

public sealed record CreateUserRequest(
    string UserName,
    string FirstName,
    string LastName,
    string? Email,
    IReadOnlyList<Guid> RoleIds,
    bool IsActive = true);
