using OAS.Contracts.Database;

namespace OAS.Client.Database.Services;

public interface IDatabaseBootstrapClientService
{
    Task<DatabaseProfilesResponse> GetProfilesAsync(CancellationToken cancellationToken = default);
    Task<DatabaseUpdateStatusDto> GetStatusAsync(string profileKey, CancellationToken cancellationToken = default);
    Task<DatabaseUpdateStatusDto> UpdateAsync(string profileKey, CancellationToken cancellationToken = default);
}
