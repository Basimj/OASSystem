using Microsoft.AspNetCore.Components.Authorization;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Database.Services;
using OAS.Client.Database.State;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Features.Employees.Workspace;
using OAS.Client.Identity.Services;
using OAS.Client.Identity.State;
using OAS.Client.Identity.Users.Workspace;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.UiLib.Extensions;


namespace OAS.Client.Services;

public static class ClientServices
{
    public static IServiceCollection AddClientServices(this IServiceCollection services, string baseAddress)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddOasUiLib();
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseAddress) });
        services.AddScoped<DatabaseProfileSelectionState>();
        services.AddScoped<OasApiClient>();
        services.AddScoped<IApiFeedbackService, ApiFeedbackService>();
        services.AddScoped<IUiFeedbackSettingsService, UiFeedbackSettingsService>();
        services.AddScoped<IDatabaseBootstrapClientService, DatabaseBootstrapClientService>();
        services.AddScoped<IAuthClientService, AuthClientService>();
        services.AddScoped<IUserClientService, UserClientService>();
        services.AddScoped<IProfileClientService, ProfileClientService>();
        services.AddScoped<IUsersWorkspaceState, UsersWorkspaceState>();
        services.AddScoped<OasAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<OasAuthenticationStateProvider>());
        services.AddScoped<IEmployeeClientService, EmployeeClientService>();
        services.AddScoped<IEmployeesWorkspaceState, EmployeesWorkspaceState>();
        services.AddScoped<BrowserFileDownloadService>();

        return services;
    }
}
