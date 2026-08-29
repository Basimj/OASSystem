using Microsoft.Extensions.DependencyInjection;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;

namespace OAS.UiLib.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOasUiLib(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddScoped<IUiSnackbarService, UiSnackbarService>();
        services.AddScoped<IUiDialogService, UiDialogService>();
        return services;
    }
}
