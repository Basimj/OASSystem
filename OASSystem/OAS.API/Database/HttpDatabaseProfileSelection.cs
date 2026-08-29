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
            var raw = context?.Request.Headers[HeaderName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw)) raw = context?.User.FindFirst(ClaimName)?.Value;
            try { return catalog.ResolveProfile(raw).Key; }
            catch { return catalog.DefaultProfileKey; }
        }
    }
}
