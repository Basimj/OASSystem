using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Mapping;
using OAS.Application.Purchasing.Matching;
using OAS.Application.Purchasing.Services;

namespace OAS.Application.Purchasing;

/// <summary>
/// Activates the Purchasing application layer after the Purchasing infrastructure
/// repositories and ports have been registered. This is intentionally separate
/// from AddApplication so intermediate layer-by-layer project snapshots remain
/// runnable and DI-valid.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPurchasingApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.TypeEvaluator = IsPurchasingApplicationType;
            configuration.RegisterServicesFromAssembly(assembly);
        });

        services.TryAddScoped<PurchasingMapper>();
        services.TryAddScoped<IPurchasingCodeService, PurchasingCodeService>();
        services.TryAddScoped<IPurchaseMatchingService, PurchaseMatchingService>();

        return services;
    }

    private static bool IsPurchasingApplicationType(Type type) =>
        type.Namespace?.StartsWith("OAS.Application.Purchasing", StringComparison.Ordinal) == true;
}
