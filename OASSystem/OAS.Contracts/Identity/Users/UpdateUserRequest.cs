namespace OAS.Contracts.Identity.Users;

public sealed record UpdateUserRequest(
    string UserName,
    string FirstName,
    string LastName,
    string? Email,
    string RowVersion);
