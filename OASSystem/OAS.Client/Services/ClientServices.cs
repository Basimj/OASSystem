using Microsoft.AspNetCore.Components.Authorization;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Database.Services;
using OAS.Client.Database.State;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Features.Employees.Workspace;
using OAS.Client.Identity.Services;
using OAS.Client.Identity.State;
using OAS.Client.Identity.Users.Workspace;
using OAS.Client.Inventory.Services;
using OAS.Client.Printing.Services;
using OAS.Client.Sales.Services;
using OAS.Client.Sales.Workspace;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.UiLib.Extensions;

namespace OAS.Client.Services;

public static class ClientServices
{
    public static IServiceCollection AddClientServices(
        this IServiceCollection services,
        string baseAddress)
    {
        services.AddLocalization(options =>
            options.ResourcesPath = "Resources");

        services.AddOasUiLib();

        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();

        services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        });

        // Database
        services.AddScoped<DatabaseProfileSelectionState>();
        services.AddScoped<OasApiClient>();
        services.AddScoped<IDatabaseBootstrapClientService, DatabaseBootstrapClientService>();

        // Feedback
        services.AddScoped<IApiFeedbackService, ApiFeedbackService>();
        services.AddScoped<IUiFeedbackSettingsService, UiFeedbackSettingsService>();

        // Identity / Authentication
        services.AddScoped<IAuthClientService, AuthClientService>();
        services.AddScoped<IUserClientService, UserClientService>();
        services.AddScoped<IProfileClientService, ProfileClientService>();

        services.AddScoped<IUsersWorkspaceState, UsersWorkspaceState>();

        services.AddScoped<OasAuthenticationStateProvider>();

        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<OasAuthenticationStateProvider>());

        // Employees
        services.AddScoped<IEmployeeClientService, EmployeeClientService>();
        services.AddScoped<IEmployeesWorkspaceState, EmployeesWorkspaceState>();

        // Inventory
        services.AddScoped<IInventoryClientService, InventoryClientService>();
        services.AddScoped<InventorySpreadsheetClient>();

        // Browser / File Download
        services.AddScoped<BrowserFileDownloadService>();

        // Printing
        services.AddScoped<IPrintingClientService, PrintingClientService>();

        // Accounting
        services.AddScoped<
            OAS.Client.Accounting.Services.IAccountingClientService,
            OAS.Client.Accounting.Services.AccountingClientService>();

        services.AddScoped<
            OAS.Client.Accounting.Workspace.IAccountingWorkspaceState,
            OAS.Client.Accounting.Workspace.AccountingWorkspaceState>();

        services.AddScoped<
            OAS.Client.Accounting.Services.AccountingSpreadsheetClient>();

        // Sales
        services.AddScoped<ISalesClientService, SalesClientService>();
        services.AddScoped<ISalesWorkspaceState, SalesWorkspaceState>();

        // Purchasing
        services.AddScoped<
            OAS.Client.Purchasing.Services.IPurchasingClientService,
            OAS.Client.Purchasing.Services.PurchasingClientService>();

        services.AddScoped<
            OAS.Client.Purchasing.Workspace.IPurchasingWorkspaceState,
            OAS.Client.Purchasing.Workspace.PurchasingWorkspaceState>();

        return services;
    }
}