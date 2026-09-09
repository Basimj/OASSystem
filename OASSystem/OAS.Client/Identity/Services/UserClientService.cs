using OAS.Client.Services.Http;
using OAS.Contracts.Common.Errors;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Roles;
using OAS.Contracts.Identity.Users;

namespace OAS.Client.Identity.Services;

public sealed class UserClientService(OasApiClient apiClient) : IUserClientService
{
    private const string Endpoint = "api/identity/users";

    public async Task<PagedResult<UserSummaryDto>> GetUsersAsync(
        PageRequest request,
        bool? isActive = null,
        Guid? roleId = null,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<UserSummaryDto>>(
            $"{Endpoint}{BuildQuery(request, isActive, roleId)}",
            cancellationToken) ?? new PagedResult<UserSummaryDto>();

    public async Task<UserDetailsDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<UserDetailsDto>($"{Endpoint}/{id}", cancellationToken)
        ?? throw new ApiClientException(new ApiError { Status = 404, Code = "not_found", Message = "User was not found." });

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<RoleDto[]>("api/identity/roles", cancellationToken) ?? [];

    public Task<ApiCallResult<CreateUserResultDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<CreateUserRequest, CreateUserResultDto>(Endpoint, request, cancellationToken);

    public Task<ApiCallResult<UserDetailsDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutResultAsync<UpdateUserRequest, UserDetailsDto>($"{Endpoint}/{id}", request, cancellationToken);

    public Task<ApiCallResult<UserDetailsDto>> SetUserStatusAsync(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<SetUserStatusRequest, UserDetailsDto>($"{Endpoint}/{id}/status", request, cancellationToken);

    public Task<ApiCallResult<UserDetailsDto>> SetUserRolesAsync(Guid id, SetUserRolesRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutResultAsync<SetUserRolesRequest, UserDetailsDto>($"{Endpoint}/{id}/roles", request, cancellationToken);

    public Task<ApiCallResult<ResetUserPasswordResultDto>> ResetUserPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<ResetUserPasswordRequest, ResetUserPasswordResultDto>($"{Endpoint}/{id}/reset-password", request, cancellationToken);

    public Task<ApiCallResult<UserDetailsDto>> UnlockUserAsync(Guid id, UnlockUserRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<UnlockUserRequest, UserDetailsDto>($"{Endpoint}/{id}/unlock", request, cancellationToken);

    public Task<ApiCallResult<bool>> UploadProfileImageAsync(Guid id, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default) =>
        apiClient.UploadFilePutResultAsync($"{Endpoint}/{id}/profile-image", stream, fileName, contentType, cancellationToken);

    public Task<ApiCallResult<bool>> RemoveProfileImageAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.DeleteResultAsync($"{Endpoint}/{id}/profile-image", cancellationToken);

    private static string BuildQuery(PageRequest request, bool? isActive, Guid? roleId)
    {
        var normalized = request.Normalize();
        var parts = new List<string>
        {
            $"pageNumber={normalized.PageNumber}",
            $"pageSize={normalized.PageSize}",
            $"sortDirection={normalized.SortDirection}"
        };
        if (!string.IsNullOrWhiteSpace(normalized.Search)) parts.Add($"search={Uri.EscapeDataString(normalized.Search)}");
        if (!string.IsNullOrWhiteSpace(normalized.SortBy)) parts.Add($"sortBy={Uri.EscapeDataString(normalized.SortBy)}");
        if (isActive.HasValue) parts.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        if (roleId.HasValue) parts.Add($"roleId={roleId.Value:D}");
        return "?" + string.Join("&", parts);
    }
}
