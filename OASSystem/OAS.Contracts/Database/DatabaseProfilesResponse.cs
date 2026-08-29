namespace OAS.Contracts.Database;

public sealed record DatabaseProfilesResponse(string DefaultProfileKey, IReadOnlyList<DatabaseProfileDto> Profiles);
