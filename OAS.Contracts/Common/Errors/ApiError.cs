namespace OAS.Contracts.Common.Errors;

public sealed record ApiError
{
    public string Code { get; init; } = "error";
    public string Message { get; init; } = "An unexpected error occurred.";
    public int Status { get; init; } = 500;
    public string? TraceId { get; init; }
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}
