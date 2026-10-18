using OAS.API.Purchasing.Services;
using OAS.Application.Purchasing.Abstractions;
using OAS.Infrastructure.Purchasing.Persistence;
using OAS.Infrastructure.Purchasing.Services;

namespace OAS.API.Purchasing;

public static class ReceiptFollowUpRegistration
{
    public static IServiceCollection AddReceiptIntegrationFollowUps(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseReceiptFollowUpJobs, ReceiptFollowUpJobStore>();
        services.AddScoped<ReceiptFollowUpProcessor>();
        services.AddHostedService<ReceiptFollowUpWorker>();
        return services;
    }
}
