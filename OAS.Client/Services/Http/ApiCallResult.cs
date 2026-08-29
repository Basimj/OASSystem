using OAS.Contracts.Common.Errors;

namespace OAS.Client.Services.Http;

public sealed record ApiCallResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public ApiError? Error { get; init; }

    public static ApiCallResult<T> Success(T? value) => new() { Succeeded = true, Value = value };
    public static ApiCallResult<T> Failure(ApiError error) => new() { Error = error };
}
