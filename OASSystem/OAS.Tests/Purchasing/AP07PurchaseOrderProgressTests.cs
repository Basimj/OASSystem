using NUnit.Framework;
using OAS.Application.Purchasing.PurchaseOrders.Queries;
using OAS.Application.Purchasing.PurchaseInvoices.Queries;
using OAS.Contracts.Common.Pagination;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class AP07PurchaseOrderProgressTests
{
    private static readonly Guid Line = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public void PartialReceipt_DisplaysRemainingToReceiveAndReturn()
    {
        var line = PurchaseOrderProgressCalculator.Calculate(Line, 1, "منتج", 100m, 30m, 0m, 0m);
        Assert.Multiple(() =>
        {
            Assert.That(line.RemainingToReceive, Is.EqualTo(70m));
            Assert.That(line.RemainingToInvoice, Is.EqualTo(30m));
            Assert.That(line.RemainingToReturn, Is.EqualTo(30m));
        });
    }

    [Test]
    public void PartialInvoiceAndReturn_DoNotCountReturnedGoodsAsOpen()
    {
        var line = PurchaseOrderProgressCalculator.Calculate(Line, 1, "منتج", 100m, 70m, 35m, 10m);
        Assert.Multiple(() =>
        {
            Assert.That(line.Invoiced, Is.EqualTo(35m));
            Assert.That(line.Returned, Is.EqualTo(10m));
            Assert.That(line.RemainingToReceive, Is.EqualTo(30m));
            Assert.That(line.RemainingToInvoice, Is.EqualTo(25m));
            Assert.That(line.RemainingToReturn, Is.EqualTo(60m));
        });
    }

    [Test]
    public void OverInvoiceClampsRemainingToZero_ButNeverChangesPostedLedger()
    {
        var line = PurchaseOrderProgressCalculator.Calculate(Line, 1, "منتج", 25m, 25m, 25m, 5m);
        Assert.That(line.RemainingToInvoice, Is.Zero);
        Assert.That(line.RemainingToReturn, Is.EqualTo(20m));
    }

    [Test]
    public void NoReceiptNoMatch_LeavesAllOrderedQuantityPending()
    {
        var line = PurchaseOrderProgressCalculator.Calculate(Line, 1, "منتج", 50m, 0m, 0m, 0m);
        Assert.Multiple(() =>
        {
            Assert.That(line.RemainingToReceive, Is.EqualTo(50m));
            Assert.That(line.RemainingToInvoice, Is.Zero);
            Assert.That(line.RemainingToReturn, Is.Zero);
        });
    }

    [Test]
    public void MatchingLookup_RequiresInvoicesAndReceiptsViewPermissions()
    {
        var request = new GetMatchedPurchaseInvoicesForReceiptQuery(Guid.NewGuid(), new PageRequest());
        Assert.That(request.RequiredPermissions, Does.Contain("purchasing.invoices.view"));
        Assert.That(request.RequiredPermissions, Does.Contain("purchasing.receipts.view"));
    }

    [Test]
    public void OrderProgress_RequiresAllRelatedDocumentViewPermissions()
    {
        var request = new GetPurchaseOrderProgressQuery(Guid.NewGuid());
        Assert.That(request.RequiredPermissions, Does.Contain("purchasing.orders.view"));
        Assert.That(request.RequiredPermissions, Does.Contain("purchasing.receipts.view"));
        Assert.That(request.RequiredPermissions, Does.Contain("purchasing.invoices.view"));
    }
}
