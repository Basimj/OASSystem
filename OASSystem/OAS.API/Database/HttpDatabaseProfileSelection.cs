using OAS.Application.Database.Abstractions;
using OAS.Infrastructure.Database.Services;

namespace OAS.API.Database;

public sealed class HttpDatabaseProfileSelection(IHttpContextAccessor httpContextAccessor, DatabaseProfileCatalog catalog) : IDatabaseProfileSelection
{
    public const string HeaderName = "X-OAS-Database-Profile";
    public const string ClaimName = "oas_database_profile";

    public string? ProfileKey
    {
        get
        {
            var context = httpContextAccessor.HttpContext;

            // Before authentication (login/bootstrap), the caller may select a database profile.
            // After authentication, the database profile is bound to the authenticated session and
            // cannot be overridden by a client-controlled request header.
            var raw = context?.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(ClaimName)?.Value
                : context?.Request.Headers[HeaderName].FirstOrDefault();

            try
            {
                return catalog.ResolveProfile(raw).Key;
            }
            catch
            {
                return catalog.DefaultProfileKey;
            }
        }
    }
}
