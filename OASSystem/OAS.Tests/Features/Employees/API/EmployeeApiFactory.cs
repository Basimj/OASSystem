using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAS.Tests.Features.Employees.Integration;

namespace OAS.Tests.Features.Employees.API;

public sealed class EmployeeApiFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        TestDatabase.ConnectionString,

                    ["Database:DefaultProfile"] =
                        "Default",

                    ["Database:Profiles:0:Key"] =
                        "Default",

                    ["Database:Profiles:0:DisplayName"] =
                        "Default",

                    ["Database:Profiles:0:ConnectionStringName"] =
                        "DefaultConnection"
                };

                config.AddInMemoryCollection(settings);
            });
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                "Test",
                _ => { });
        });
    }
}