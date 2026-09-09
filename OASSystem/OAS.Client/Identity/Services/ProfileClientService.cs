using OAS.Client.Services.Http;
using OAS.Contracts.Identity.Authentication;
using OAS.Contracts.Identity.Profile;

namespace OAS.Client.Identity.Services;

public sealed class ProfileClientService(OasApiClient apiClient) : IProfileClientService
{
    public Task<ApiCallResult<MyProfileDto>> GetAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetResultAsync<MyProfileDto>("api/identity/profile", cancellationToken);

    public Task<ApiCallResult<MyProfileDto>> UpdateAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutResultAsync<UpdateMyProfileRequest, MyProfileDto>("api/identity/profile", request, cancellationToken);

    public Task<ApiCallResult<CurrentUserDto>> ChangePasswordAsync(ChangeMyPasswordRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<ChangeMyPasswordRequest, CurrentUserDto>("api/identity/profile/change-password", request, cancellationToken);

    public Task<ApiCallResult<bool>> UploadImageAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default) =>
        apiClient.UploadFilePutResultAsync("api/identity/profile/image", stream, fileName, contentType, cancellationToken);

    public Task<ApiCallResult<bool>> RemoveImageAsync(CancellationToken cancellationToken = default) =>
        apiClient.DeleteResultAsync("api/identity/profile/image", cancellationToken);
}
