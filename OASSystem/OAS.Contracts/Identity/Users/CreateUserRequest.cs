namespace OAS.Contracts.Identity.Users;

public sealed record CreateUserRequest(
    string UserName,
    string FirstName,
    string LastName,
    string? Email,
    string Password,
    string ConfirmPassword,
    Guid RoleId,
    bool IsActive = true);
