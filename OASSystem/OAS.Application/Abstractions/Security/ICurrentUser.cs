namespace OAS.Application.Abstractions.Security;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}
