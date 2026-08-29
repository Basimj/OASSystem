using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Models;

public sealed record CreateUserResult
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();

    public UserDto? User { get; init; }
    public CreateUserFailureKind FailureKind { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = EmptyErrors;
    public bool Succeeded => User is not null && FailureKind == CreateUserFailureKind.None;

    public static CreateUserResult Success(UserDto user) => new() { User = user };
    public static CreateUserResult Validation(IReadOnlyDictionary<string, string[]> errors) => new()
    {
        FailureKind = CreateUserFailureKind.Validation,
        ErrorCode = "validation_failed",
        Errors = errors
    };
    public static CreateUserResult Conflict(string errorCode) => new() { FailureKind = CreateUserFailureKind.Conflict, ErrorCode = errorCode };
    public static CreateUserResult Forbidden(string errorCode = "forbidden") => new() { FailureKind = CreateUserFailureKind.Forbidden, ErrorCode = errorCode };
}
