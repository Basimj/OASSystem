namespace OAS.Infrastructure.Database.Configuration;

public sealed class DatabaseProfilesOptions
{
    public const string SectionName = "Database";
    public string DefaultProfile { get; set; } = "Default";
    public List<DatabaseProfileOptions> Profiles { get; set; } = [];
}

public sealed class DatabaseProfileOptions
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ConnectionStringName { get; set; } = string.Empty;
}
