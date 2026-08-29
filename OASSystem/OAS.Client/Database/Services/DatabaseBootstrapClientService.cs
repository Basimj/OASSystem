using OAS.Client.Database.State;
using OAS.Client.Services.Http;
using OAS.Contracts.Database;

namespace OAS.Client.Database.Services;

public sealed class DatabaseBootstrapClientService(OasApiClient apiClient, DatabaseProfileSelectionState selection) : IDatabaseBootstrapClientService
{
    public async Task<DatabaseProfilesResponse> GetProfilesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<DatabaseProfilesResponse>("api/database/bootstrap/profiles", cancellationToken)
        ?? new DatabaseProfilesResponse("Default", []);

    public async Task<DatabaseUpdateStatusDto> GetStatusAsync(string profileKey, CancellationToken cancellationToken = default)
    {
        selection.SelectedProfileKey = profileKey;
        return await apiClient.GetAsync<DatabaseUpdateStatusDto>($"api/database/bootstrap/status?profileKey={Uri.EscapeDataString(profileKey)}", cancellationToken)
            ?? new DatabaseUpdateStatusDto(profileKey, false, false, 0, "database_unavailable");
    }

    public async Task<DatabaseUpdateStatusDto> UpdateAsync(string profileKey, CancellationToken cancellationToken = default)
    {
        selection.SelectedProfileKey = profileKey;
        return await apiClient.PostAsync<DatabaseUpdateRequest, DatabaseUpdateStatusDto>(
            "api/database/bootstrap/update", new DatabaseUpdateRequest(profileKey), cancellationToken)
            ?? new DatabaseUpdateStatusDto(profileKey, false, false, 0, "database_update_failed");
    }
}
