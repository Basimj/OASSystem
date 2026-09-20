using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OAS.Application;
using OAS.Infrastructure;

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
        services.AddApplication();
        services.AddInfrastructure(configuration);

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
}
