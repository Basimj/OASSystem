using OAS.Client.Services.Http;
using OAS.Contracts.Identity.Authentication;
using OAS.Contracts.Identity.Profile;

namespace OAS.Client.Identity.Services;

public interface IProfileClientService
{
    Task<ApiCallResult<MyProfileDto>> GetAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<MyProfileDto>> UpdateAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<CurrentUserDto>> ChangePasswordAsync(ChangeMyPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> UploadImageAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> RemoveImageAsync(CancellationToken cancellationToken = default);
}
