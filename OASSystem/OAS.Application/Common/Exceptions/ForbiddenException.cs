namespace OAS.Application.Common.Exceptions;

public sealed class ForbiddenException(
    string message = "You are not allowed to perform this operation.",
    string code = "forbidden") : Exception(message)
{
    public string Code { get; } = code;
}
