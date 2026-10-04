using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NUnit.Framework;
using OAS.API.Purchasing.Controllers;
using OAS.API.Sales.Controllers;

namespace OAS.Tests.Sales.API;

[TestFixture]
public sealed class CommercialExtensionsControllersTests
{
    private static readonly (Type Controller, string RoutePrefix)[] Controllers =
    [
        (typeof(SalesReturnsController), "api/sales/"),
        (typeof(CommissionsController), "api/sales/"),
        (typeof(OpticalProductionController), "api/sales/"),
        (typeof(PurchaseReturnsController), "api/purchasing/")
    ];

    [Test]
    public void NewCommercialControllers_AreSecuredRateLimitedAndRouted()
    {
        foreach (var (controller, prefix) in Controllers)
        {
            Assert.That(controller.GetCustomAttribute<ApiControllerAttribute>(inherit: true), Is.Not.Null,
                $"{controller.Name} must use [ApiController].");
            Assert.That(controller.GetCustomAttribute<AuthorizeAttribute>(inherit: true), Is.Not.Null,
                $"{controller.Name} must require authorization.");
            Assert.That(controller.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true), Is.Not.Null,
                $"{controller.Name} must participate in API rate limiting.");

            var route = controller.GetCustomAttribute<RouteAttribute>(inherit: true);
            Assert.That(route, Is.Not.Null);
            Assert.That(route!.Template, Does.StartWith(prefix),
                $"{controller.Name} route must start with '{prefix}'.");
        }
    }

    [Test]
    public void NewCommercialControllerActions_DeclareHttpVerbs()
    {
        foreach (var (controller, _) in Controllers)
        {
            var actionMethods = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .ToArray();

            Assert.That(actionMethods, Is.Not.Empty, $"{controller.Name} must expose at least one action.");

            foreach (var method in actionMethods)
            {
                var hasVerb = method.GetCustomAttributes(inherit: true)
                    .Any(attribute => attribute is HttpGetAttribute or HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute);

                Assert.That(hasVerb, Is.True,
                    $"{controller.Name}.{method.Name} must declare an HTTP verb attribute.");
            }
        }
    }
}
