using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using OAS.API.Accounting.Controllers;

namespace OAS.Tests.Accounting.API;

[TestFixture]
public class AccountingControllersRoutingAndSecurityTests
{
    private static readonly Type[] ControllerTypes =
    [
        typeof(AccountsController),
        typeof(BankAccountsController),
        typeof(CashAccountsController),
        typeof(CashShiftsController),
        typeof(CostCentersController),
        typeof(CustomerAccountsController),
        typeof(ExpensesController),
        typeof(FiscalPeriodsController),
        typeof(FiscalYearsController),
        typeof(JournalEntriesController),
        typeof(PaymentVouchersController),
        typeof(PostingProfilesController),
        typeof(ReceiptVouchersController),
        typeof(SupplierAccountsController)
    ];

    [Test]
    public void AllAccountingControllers_HaveApiControllerAndRouteAttributes()
    {
        foreach (var controller in ControllerTypes)
        {
            var apiAttr = controller.GetCustomAttribute<ApiControllerAttribute>(inherit: true);
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>(inherit: true);

            Assert.That(apiAttr, Is.Not.Null, $"{controller.Name} must have [ApiController] attribute");
            Assert.That(routeAttr, Is.Not.Null, $"{controller.Name} must have [Route] attribute");
            Assert.That(routeAttr!.Template, Does.StartWith("api/accounting/"),
                $"{controller.Name} route must start with 'api/accounting/'");
        }
    }

    [Test]
    public void AllAccountingControllers_HaveAuthorizeAttribute()
    {
        foreach (var controller in ControllerTypes)
        {
            var authAttr = controller.GetCustomAttribute<AuthorizeAttribute>(inherit: true);
            Assert.That(authAttr, Is.Not.Null, $"{controller.Name} must have [Authorize] attribute");
        }
    }

    [Test]
    public void AllAccountingControllers_ActionMethods_HaveHttpVerbAttributes()
    {
        foreach (var controller in ControllerTypes)
        {
            var publicMethods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);

            foreach (var method in publicMethods)
            {
                var httpAttr = method.GetCustomAttributes()
                    .Any(a => a is HttpGetAttribute or HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute or HttpPatchAttribute);

                Assert.That(httpAttr, Is.True,
                    $"Action method {controller.Name}.{method.Name} must have an HTTP verb attribute ([HttpGet], [HttpPost], etc.)");
            }
        }
    }
}
