using OAS.Contracts.Identity.Authentication;

namespace OAS.Application.Identity.Authentication.Models;

public sealed record LoginResult
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();

    public CurrentUserDto? User { get; init; }
    public LoginFailureKind FailureKind { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = EmptyErrors;
    public bool Succeeded => User is not null && FailureKind == LoginFailureKind.None;

    public static LoginResult Success(CurrentUserDto user) => new() { User = user };
    public static LoginResult InvalidCredentials() => new() { FailureKind = LoginFailureKind.InvalidCredentials, ErrorCode = "identity_invalid_credentials" };
    public static LoginResult Validation(IReadOnlyDictionary<string, string[]> errors) => new()
    {
        FailureKind = LoginFailureKind.Validation,
        ErrorCode = "validation_failed",
        Errors = errors
    };
}
