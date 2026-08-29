using System.Security.Claims;
using OAS.Application.Abstractions.Security;

namespace OAS.API.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
