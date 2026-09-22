using OAS.Application.Abstractions.Security;

namespace OAS.API.Security;

public sealed class HttpCurrentRequestInfo(
    IHttpContextAccessor httpContextAccessor) : ICurrentRequestInfo
{
    public string? Device
    {
        get
        {
            var context = httpContextAccessor.HttpContext;

            if (context is null)
                return null;

            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ip = context.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrWhiteSpace(userAgent) &&
                string.IsNullOrWhiteSpace(ip))
            {
                return null;
            }

            return $"IP={ip ?? "unknown"}; UserAgent={userAgent}";
        }
    }
}