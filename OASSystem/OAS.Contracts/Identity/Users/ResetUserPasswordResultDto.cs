namespace OAS.Contracts.Identity.Users;

public sealed record ResetUserPasswordResultDto(
    string TemporaryPassword,
    string RowVersion);
