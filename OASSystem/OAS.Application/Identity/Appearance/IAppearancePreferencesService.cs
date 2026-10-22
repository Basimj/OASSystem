using OAS.Contracts.Identity.Appearance;

namespace OAS.Application.Identity.Appearance;

public interface IAppearancePreferencesService
{
    Task<AppearanceSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<AppearanceSettingsDto> SaveAsync(AppearanceSettingsDto settings, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}
