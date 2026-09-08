namespace OAS.Application.Common.Exceptions;

public sealed class ConflictException(
    string code,
    string message)
    : Exception(message)
{
    public string Code { get; } = code;
}