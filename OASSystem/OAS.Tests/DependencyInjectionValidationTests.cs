using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OAS.Application;
using OAS.Infrastructure;
using OAS.Application.Abstractions.Security;
using OAS.Application.Database.Abstractions;
using OAS.Application.Purchasing.Abstractions;

namespace OAS.Tests;

[TestFixture]
public sealed class DependencyInjectionValidationTests
{
    [Test]
    public void ServiceProvider_ValidateOnBuild_SucceedsWithoutUnresolvedDependencies()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=OAS_Test;Trusted_Connection=True;",
                ["Database:DefaultProfile"] = "Default",
                ["Database:Profiles:0:Key"] = "Default",
                ["Database:Profiles:0:DisplayName"] = "Default",
                ["Database:Profiles:0:ConnectionStringName"] = "DefaultConnection",
                ["Authentication:SessionHours"] = "8"
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ICurrentRequestInfo>(new TestCurrentRequestInfo());
        services.AddSingleton<IDatabaseProfileSelection>(new TestDatabaseProfileSelection());
        services.AddApplication();
        services.AddInfrastructure(configuration);

        Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(ICustomerDemandSourcingService)), Is.True,
            "Customer-demand sourcing service must be registered before Purchasing MediatR handlers are activated.");
        Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(IPurchasingCommercialTermsPort)), Is.True,
            "Purchasing commercial-terms resolver must be registered for customer-demand PO sourcing.");

        // This enforces validation of all registered services and their constructor dependencies
        var options = new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        };

        Assert.DoesNotThrow(() =>
        {
            using var provider = services.BuildServiceProvider(options);
        });
    }
    [Test]
    public void AddApplication_DefersPurchasingHandlersUntilPurchasingInfrastructureIsRegistered()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var prematurePurchasingHandlers = services
            .Where(descriptor =>
                descriptor.ImplementationType?.Namespace?.StartsWith("OAS.Application.Purchasing", StringComparison.Ordinal) == true
                && descriptor.ServiceType.Namespace == "MediatR"
                && descriptor.ServiceType.Name.StartsWith("IRequestHandler", StringComparison.Ordinal))
            .ToArray();

        Assert.That(prematurePurchasingHandlers, Is.Empty,
            "Purchasing MediatR handlers must not be activated until Purchasing Infrastructure repositories/ports are registered.");

        Assert.That(services.Any(descriptor => descriptor.ImplementationType?.FullName == "OAS.Application.Purchasing.Mapping.PurchasingMapper"), Is.False);
        Assert.That(services.Any(descriptor => descriptor.ImplementationType?.FullName == "OAS.Application.Purchasing.Matching.PurchaseMatchingService"), Is.False);
    }

    private sealed class TestCurrentRequestInfo : ICurrentRequestInfo
    {
        public string? Device => "tests";
    }

    private sealed class TestDatabaseProfileSelection : IDatabaseProfileSelection
    {
        public string? ProfileKey => "Default";
    }

}
