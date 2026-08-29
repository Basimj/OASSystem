namespace OAS.Application.Identity.Exceptions;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException() : base("Authentication failed.") { }
}
