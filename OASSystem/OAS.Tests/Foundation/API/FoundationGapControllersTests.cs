using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NUnit.Framework;
using OAS.API.Accounting.Controllers;
using OAS.API.Optical.Controllers;
using OAS.API.Sales.Controllers;

namespace OAS.Tests.Foundation.API;

[TestFixture]
public sealed class FoundationGapControllersTests
{
    private static readonly Type[] Controllers =
    [
        typeof(CustomerPrescriptionContextController),
        typeof(CustomerOrderAvailabilityController),
        typeof(OpticalJobsController),
        typeof(CustomerAdvancesController)
    ];

    [Test]
    public void FoundationControllers_AreApiControllersAuthorizedAndRateLimited()
    {
        foreach (var controller in Controllers)
        {
            Assert.That(controller.GetCustomAttribute<ApiControllerAttribute>(true), Is.Not.Null, controller.Name);
            Assert.That(controller.GetCustomAttribute<AuthorizeAttribute>(true), Is.Not.Null, controller.Name);
            Assert.That(controller.GetCustomAttribute<EnableRateLimitingAttribute>(true), Is.Not.Null, controller.Name);
            Assert.That(controller.GetCustomAttribute<RouteAttribute>(true), Is.Not.Null, controller.Name);
        }
    }

    [Test]
    public void FoundationControllerActions_DeclareHttpVerbs()
    {
        foreach (var controller in Controllers)
        {
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(x => !x.IsSpecialName))
            {
                var hasVerb = method.GetCustomAttributes(true)
                    .Any(x => x is HttpGetAttribute or HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute);
                Assert.That(hasVerb, Is.True, $"{controller.Name}.{method.Name}");
            }
        }
    }

    [Test]
    public void RequiredRoutes_AreExposed()
    {
        Assert.That(typeof(CustomerPrescriptionContextController).GetCustomAttribute<RouteAttribute>()!.Template,
            Is.EqualTo("api/sales/customers/{customerId:guid}"));
        Assert.That(typeof(CustomerOrderAvailabilityController).GetCustomAttribute<RouteAttribute>()!.Template,
            Is.EqualTo("api/sales/orders"));
        Assert.That(typeof(OpticalJobsController).GetCustomAttribute<RouteAttribute>()!.Template,
            Is.EqualTo("api/optical/jobs"));
        Assert.That(typeof(CustomerAdvancesController).GetCustomAttribute<RouteAttribute>()!.Template,
            Is.EqualTo("api/accounting/customer-advances"));
    }
}
