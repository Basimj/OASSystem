using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OAS.Contracts.Database;
using OAS.Infrastructure.Database.Configuration;

namespace OAS.Infrastructure.Database.Services;

public sealed class DatabaseProfileCatalog(IOptions<DatabaseProfilesOptions> options, IConfiguration configuration)
{
    private readonly DatabaseProfilesOptions _options = options.Value;

    public string DefaultProfileKey => ResolveConfiguredDefault().Key;

    public DatabaseProfilesResponse GetPublicProfiles()
    {
        var defaultKey = DefaultProfileKey;
        var profiles = GetProfiles()
            .Select(x => new DatabaseProfileDto(x.Key, x.DisplayName, string.Equals(x.Key, defaultKey, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        return new DatabaseProfilesResponse(defaultKey, profiles);
    }

    public string ResolveConnectionString(string? profileKey)
    {
        var profile = ResolveProfile(profileKey);
        return configuration.GetConnectionString(profile.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{profile.ConnectionStringName}' for database profile '{profile.Key}' is not configured.");
    }

    public DatabaseProfileOptions ResolveProfile(string? profileKey)
    {
        var requested = string.IsNullOrWhiteSpace(profileKey) ? DefaultProfileKey : profileKey.Trim();
        return GetProfiles().FirstOrDefault(x => string.Equals(x.Key, requested, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Database profile '{requested}' is not configured.");
    }

    private DatabaseProfileOptions ResolveConfiguredDefault()
    {
        var profiles = GetProfiles();
        var configured = profiles.FirstOrDefault(x => string.Equals(x.Key, _options.DefaultProfile, StringComparison.OrdinalIgnoreCase));
        return configured ?? profiles[0];
    }

    private IReadOnlyList<DatabaseProfileOptions> GetProfiles()
    {
        if (_options.Profiles.Count > 0) return _options.Profiles;
        return [new DatabaseProfileOptions { Key = "Default", DisplayName = "Default", ConnectionStringName = "DefaultConnection" }];
    }
}
