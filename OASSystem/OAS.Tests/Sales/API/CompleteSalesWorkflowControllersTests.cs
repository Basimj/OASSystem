using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NUnit.Framework;
using OAS.API.Optical.Controllers;
using OAS.API.Purchasing.Controllers;
using OAS.API.Sales.Controllers;

namespace OAS.Tests.Sales.API;

[TestFixture]
public sealed class CompleteSalesWorkflowControllersTests
{
    private static readonly Type[] Controllers =
    [
        typeof(SalesCheckoutController),
        typeof(CustomerOrderOperationsController),
        typeof(CustomerDemandController),
        typeof(SupplierPurchaseHistoryController),
        typeof(OpticalJobsController)
    ];

    [Test]
    public void WorkflowControllers_AreApiControllersAuthorizedAndRateLimited()
    {
        foreach (var controller in Controllers)
        {
            Assert.That(controller.GetCustomAttribute<ApiControllerAttribute>(inherit: true), Is.Not.Null, controller.Name);
            Assert.That(controller.GetCustomAttribute<AuthorizeAttribute>(inherit: true), Is.Not.Null, controller.Name);
            Assert.That(controller.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true), Is.Not.Null, controller.Name);
            Assert.That(controller.GetCustomAttribute<RouteAttribute>(inherit: true), Is.Not.Null, controller.Name);
        }
    }

    [Test]
    public void WorkflowControllerActions_DeclareHttpVerbs()
    {
        foreach (var controller in Controllers)
        {
            var methods = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .ToArray();

            Assert.That(methods, Is.Not.Empty, controller.Name);

            foreach (var method in methods)
            {
                var hasVerb = method.GetCustomAttributes(inherit: true)
                    .Any(attribute => attribute is HttpGetAttribute
                        or HttpPostAttribute
                        or HttpPutAttribute
                        or HttpPatchAttribute
                        or HttpDeleteAttribute);

                Assert.That(hasVerb, Is.True, $"{controller.Name}.{method.Name}");
            }
        }
    }

    [Test]
    public void WorkflowControllerRoutePrefixes_MatchTaskContract()
    {
        Assert.That(GetRoute<SalesCheckoutController>(), Is.EqualTo("api/sales"));
        Assert.That(GetRoute<CustomerOrderOperationsController>(), Is.EqualTo("api/sales/order-operations"));
        Assert.That(GetRoute<CustomerDemandController>(), Is.EqualTo("api/purchasing/customer-demand"));
        Assert.That(GetRoute<SupplierPurchaseHistoryController>(), Is.EqualTo("api/purchasing/suppliers"));
        Assert.That(GetRoute<OpticalJobsController>(), Is.EqualTo("api/optical/jobs"));
    }

    [Test]
    public void SalesCheckoutController_ExposesCheckoutAndDeliveryRoutes()
    {
        Assert.That(GetHttpTemplate<SalesCheckoutController>(nameof(SalesCheckoutController.GetCustomerContext), typeof(HttpGetAttribute)),
            Is.EqualTo("checkout/customers/{customerId:guid}/context"));
        Assert.That(GetHttpTemplate<SalesCheckoutController>(nameof(SalesCheckoutController.GetCheckoutContext), typeof(HttpGetAttribute)),
            Is.EqualTo("orders/{orderId:guid}/checkout-context"));
        Assert.That(GetHttpTemplate<SalesCheckoutController>(nameof(SalesCheckoutController.Checkout), typeof(HttpPostAttribute)),
            Is.EqualTo("orders/{orderId:guid}/checkout"));
        Assert.That(GetHttpTemplate<SalesCheckoutController>(nameof(SalesCheckoutController.Deliver), typeof(HttpPostAttribute)),
            Is.EqualTo("orders/{orderId:guid}/delivery"));
    }

    [Test]
    public void CustomerDemandController_ExposesTrackingAndSourcingRoutes()
    {
        Assert.That(GetHttpTemplate<CustomerDemandController>(nameof(CustomerDemandController.GetSummary), typeof(HttpGetAttribute)),
            Is.EqualTo("summary"));
        Assert.That(GetHttpTemplate<CustomerDemandController>(nameof(CustomerDemandController.AssignSupplier), typeof(HttpPatchAttribute)),
            Is.EqualTo("{lineId:guid}/supplier"));
        Assert.That(GetHttpTemplate<CustomerDemandController>(nameof(CustomerDemandController.Schedule), typeof(HttpPatchAttribute)),
            Is.EqualTo("{lineId:guid}/schedule"));
        Assert.That(GetHttpTemplate<CustomerDemandController>(nameof(CustomerDemandController.CreatePurchaseOrder), typeof(HttpPostAttribute)),
            Is.EqualTo("{lineId:guid}/create-po"));
        Assert.That(GetHttpTemplate<CustomerDemandController>(nameof(CustomerDemandController.ResourceRemaining), typeof(HttpPostAttribute)),
            Is.EqualTo("{lineId:guid}/resource-remaining"));
    }

    [Test]
    public void OpticalJobsController_ExposesRequiredOperationalRoutes()
    {
        Assert.That(GetHttpTemplate<OpticalJobsController>(nameof(OpticalJobsController.GetWorkQueue), typeof(HttpGetAttribute)),
            Is.EqualTo("work-queue"));
        Assert.That(GetHttpTemplate<OpticalJobsController>(nameof(OpticalJobsController.Assign), typeof(HttpPostAttribute)),
            Is.EqualTo("{id:guid}/assign"));
        Assert.That(GetHttpTemplate<OpticalJobsController>(nameof(OpticalJobsController.Start), typeof(HttpPostAttribute)),
            Is.EqualTo("{id:guid}/start"));
        Assert.That(GetHttpTemplate<OpticalJobsController>(nameof(OpticalJobsController.SendToQualityControl), typeof(HttpPostAttribute)),
            Is.EqualTo("{id:guid}/send-to-quality-control"));
        Assert.That(GetHttpTemplate<OpticalJobsController>(nameof(OpticalJobsController.PassQualityControl), typeof(HttpPostAttribute)),
            Is.EqualTo("{id:guid}/pass-quality-control"));

        var readyRoutes = typeof(OpticalJobsController)
            .GetMethod(nameof(OpticalJobsController.MarkReadyForDelivery))!
            .GetCustomAttributes<HttpPostAttribute>(inherit: true)
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.That(readyRoutes, Does.Contain("{id:guid}/ready-for-delivery"));
        Assert.That(readyRoutes, Does.Contain("{id:guid}/ready"));
    }

    private static string? GetRoute<TController>() =>
        typeof(TController).GetCustomAttribute<RouteAttribute>(inherit: true)?.Template;

    private static string? GetHttpTemplate<TController>(string methodName, Type attributeType)
    {
        var method = typeof(TController).GetMethod(methodName)
            ?? throw new AssertionException($"Method {typeof(TController).Name}.{methodName} was not found.");

        return method.GetCustomAttributes(inherit: true)
            .First(attribute => attribute.GetType() == attributeType) switch
        {
            HttpGetAttribute get => get.Template,
            HttpPostAttribute post => post.Template,
            HttpPatchAttribute patch => patch.Template,
            HttpPutAttribute put => put.Template,
            HttpDeleteAttribute delete => delete.Template,
            _ => null
        };
    }
}
