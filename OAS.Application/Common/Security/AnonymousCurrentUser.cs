using OAS.Application.Abstractions.Security;

namespace OAS.Application.Common.Security;

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public string? UserId => null;
    public bool IsAuthenticated => false;
}
