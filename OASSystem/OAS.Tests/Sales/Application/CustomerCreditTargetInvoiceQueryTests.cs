using NUnit.Framework;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Returns.Queries;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class CustomerCreditTargetInvoiceQueryTests
{
    [Test]
    public void RequiresCreditPermissionAndInvoiceViewPermission()
    {
        var query = new GetCustomerCreditTargetInvoicesQuery(Guid.NewGuid());
        Assert.That(query.RequiredPermissions, Does.Contain(SalesPermissions.Credit.Use));
        Assert.That(query.RequiredPermissions, Does.Contain(SalesPermissions.Invoices.View));
    }

    [Test]
    public void MissingCustomerId_IsRejectedBeforeRepositoryAccess()
    {
        var handler = new GetCustomerCreditTargetInvoicesQueryHandler(null!, null!);
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await handler.Handle(new GetCustomerCreditTargetInvoicesQuery(Guid.Empty), CancellationToken.None);
        });
    }
}
