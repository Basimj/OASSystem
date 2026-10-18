using System.Security.Claims;
using OAS.Application.Abstractions.Security;
using OAS.API.Purchasing.Services;

namespace OAS.API.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub") ?? (Principal is null && ReceiptWorkerAmbient.CurrentProfile is not null ? "system:receipt-followup" : null);
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true || (Principal is null && ReceiptWorkerAmbient.CurrentProfile is not null);
}
