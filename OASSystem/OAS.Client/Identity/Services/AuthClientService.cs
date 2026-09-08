using OAS.Client.Services.Http;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Client.Identity.Services;

public sealed class AuthClientService(OasApiClient apiClient) : IAuthClientService
{
    public Task<ApiCallResult<CurrentUserDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<LoginRequest, CurrentUserDto>("api/identity/auth/login", request, cancellationToken);

    public Task<ApiCallResult<CurrentUserDto>> CompletePasswordSetupAsync(CompletePasswordSetupRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<CompletePasswordSetupRequest, CurrentUserDto>("api/identity/auth/complete-password-setup", request, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        apiClient.PostAsync("api/identity/auth/logout", cancellationToken);

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try { return await apiClient.GetAsync<CurrentUserDto>("api/identity/auth/me", cancellationToken); }
        catch (ApiClientException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403) { return null; }
    }
}
