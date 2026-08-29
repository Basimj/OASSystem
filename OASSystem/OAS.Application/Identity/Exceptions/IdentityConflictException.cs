namespace OAS.Application.Identity.Exceptions;

public sealed class IdentityConflictException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
