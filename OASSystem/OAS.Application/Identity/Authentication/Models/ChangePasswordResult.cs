using OAS.Contracts.Identity.Authentication;

namespace OAS.Application.Identity.Authentication.Models;

public sealed record ChangePasswordResult
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();

    public CurrentUserDto? User { get; init; }
    public ChangePasswordFailureKind FailureKind { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = EmptyErrors;
    public bool Succeeded => User is not null && FailureKind == ChangePasswordFailureKind.None;

    public static ChangePasswordResult Success(CurrentUserDto user) => new() { User = user };

    public static ChangePasswordResult Validation(IReadOnlyDictionary<string, string[]> errors) => new()
    {
        FailureKind = ChangePasswordFailureKind.Validation,
        ErrorCode = "validation_failed",
        Errors = errors
    };

    public static ChangePasswordResult Failure(ChangePasswordFailureKind kind, string errorCode) => new()
    {
        FailureKind = kind,
        ErrorCode = errorCode
    };
}
