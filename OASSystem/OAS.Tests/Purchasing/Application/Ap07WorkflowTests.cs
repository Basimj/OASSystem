using NUnit.Framework;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.PurchaseInvoices.Queries;
using OAS.Application.Purchasing.PurchaseOrders.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseOrders;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class Ap07WorkflowTests
{
    [Test]
    public void ReceiptLinkedInvoiceLookup_RequiresBothReceiptAndInvoicePermissions()
    {
        var request = new GetMatchedPurchaseInvoicesForReceiptQuery(Guid.NewGuid(),
            new PageRequest { PageNumber = 502, PageSize = 100_000 });
        Assert.Multiple(() =>
        {
            Assert.That(request.RequiredPermissions, Does.Contain(PurchasingPermissions.Receipts.View));
            Assert.That(request.RequiredPermissions, Does.Contain(PurchasingPermissions.Invoices.View));
            Assert.That(request.Request.Normalize().PageSize, Is.EqualTo(PageRequest.MaximumPageSize));
            Assert.That(request.Request.Normalize().PageNumber, Is.EqualTo(502));
        });
    }

    [Test]
    public void PurchaseOrderProgress_RequiresReceivingAndInvoiceVisibility()
    {
        var request = new GetPurchaseOrderProgressQuery(Guid.NewGuid());
        Assert.Multiple(() =>
        {
            Assert.That(request.RequiredPermissions, Does.Contain(PurchasingPermissions.Orders.View));
            Assert.That(request.RequiredPermissions, Does.Contain(PurchasingPermissions.Receipts.View));
            Assert.That(request.RequiredPermissions, Does.Contain(PurchasingPermissions.Invoices.View));
        });
    }

    [Test]
    public void ProgressContract_KeepsPostedInvoiceAndReturnQuantitiesSeparate()
    {
        var line = new PurchaseOrderLineProgressDto(Guid.NewGuid(), 1, "عدسات",
            Ordered: 10m, Received: 8m, Invoiced: 6m, Returned: 1m,
            RemainingToReceive: 2m, RemainingToInvoice: 1m, RemainingToReturn: 7m);
        Assert.That(line.Received - line.Invoiced - line.Returned, Is.EqualTo(line.RemainingToInvoice));
        Assert.That(line.Ordered - line.Received, Is.EqualTo(line.RemainingToReceive));
    }
}
