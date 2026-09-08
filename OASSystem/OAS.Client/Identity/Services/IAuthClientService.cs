using OAS.Client.Services.Http;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Client.Identity.Services;

public interface IAuthClientService
{
    Task<ApiCallResult<CurrentUserDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<CurrentUserDto>> CompletePasswordSetupAsync(CompletePasswordSetupRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
