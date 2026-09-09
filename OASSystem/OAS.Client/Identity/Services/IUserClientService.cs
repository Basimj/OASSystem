using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Roles;
using OAS.Contracts.Identity.Users;

namespace OAS.Client.Identity.Services;

public interface IUserClientService
{
    Task<PagedResult<UserSummaryDto>> GetUsersAsync(
        PageRequest request,
        bool? isActive = null,
        Guid? roleId = null,
        CancellationToken cancellationToken = default);
    Task<UserDetailsDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<CreateUserResultDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDetailsDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDetailsDto>> SetUserStatusAsync(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDetailsDto>> SetUserRolesAsync(Guid id, SetUserRolesRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<ResetUserPasswordResultDto>> ResetUserPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<UserDetailsDto>> UnlockUserAsync(Guid id, UnlockUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> UploadProfileImageAsync(Guid id, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> RemoveProfileImageAsync(Guid id, CancellationToken cancellationToken = default);
}
