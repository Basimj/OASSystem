namespace OAS.Contracts.Identity.Users;

public sealed record CreateUserResultDto(
    UserDetailsDto User,
    string TemporaryPassword);
