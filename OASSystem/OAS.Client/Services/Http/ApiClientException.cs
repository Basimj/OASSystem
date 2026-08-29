using OAS.Contracts.Common.Errors;

namespace OAS.Client.Services.Http;

public sealed class ApiClientException : Exception
{
    public ApiClientException(ApiError error)
        : base(error.Message)
    {
        Error = error;
        StatusCode = error.Status;
    }

    public ApiError Error { get; }
    public int StatusCode { get; }
}
