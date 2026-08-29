using OAS.Client.Services.Http;
using OAS.Contracts.Identity.Roles;
using OAS.Contracts.Identity.Users;

namespace OAS.Client.Identity.Services;

public interface IUserClientService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
