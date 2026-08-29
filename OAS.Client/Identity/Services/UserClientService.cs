using OAS.Client.Services.Http;
using OAS.Contracts.Identity.Roles;
using OAS.Contracts.Identity.Users;

namespace OAS.Client.Identity.Services;

public sealed class UserClientService(OasApiClient apiClient) : IUserClientService
{
    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<UserDto[]>("api/identity/users", cancellationToken) ?? [];

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<RoleDto[]>("api/identity/roles", cancellationToken) ?? [];

    public Task<ApiCallResult<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<CreateUserRequest, UserDto>("api/identity/users", request, cancellationToken);
}
