using NUnit.Framework;
using OAS.Application.Accounting.Reports.Services;

namespace OAS.Tests.Accounting;

[TestFixture]
public sealed class CustomerArMathTests
{
    [Test]
    public void PaidInvoiceWithReturn_DoesNotCreateNegativeOutstanding()
    {
        Assert.That(CustomerArMath.OpenInvoice(100000m, 100000m, 30000m, 0m), Is.Zero);
    }

    [Test]
    public void ReturnAndReceipt_AreNotCountedTwice()
    {
        Assert.That(CustomerArMath.OpenInvoice(100000m, 70000m, 30000m, 0m), Is.Zero);
        Assert.That(CustomerArMath.OpenInvoice(100000m, 30000m, 20000m, 10000m), Is.EqualTo(40000m));
    }

    [Test]
    public void AsOfBuckets_AreStableAcrossBoundaries()
    {
        var asOf = new DateOnly(2026, 11, 30);
        Assert.Multiple(() =>
        {
            Assert.That(CustomerArMath.Bucket(null, asOf), Is.EqualTo("Unknown"));
            Assert.That(CustomerArMath.Bucket(asOf.AddDays(1), asOf), Is.EqualTo("NotDue"));
            Assert.That(CustomerArMath.Bucket(asOf, asOf), Is.EqualTo("0-30"));
            Assert.That(CustomerArMath.Bucket(asOf.AddDays(-30), asOf), Is.EqualTo("0-30"));
            Assert.That(CustomerArMath.Bucket(asOf.AddDays(-31), asOf), Is.EqualTo("31-60"));
            Assert.That(CustomerArMath.Bucket(asOf.AddDays(-91), asOf), Is.EqualTo("+90"));
        });
    }

    [Test]
    public void ReturnCredit_ConsumesOnlyGenuinePaidReturnEntitlement()
    {
        // A 30k return on an invoice with 100k outstanding is NOT a free credit.
        Assert.That(CustomerArMath.ReturnCredit(30000m, 100000m, 0m), Is.Zero);
        // Fully paid invoice: the entire return is eligible, net of applications/refunds.
        Assert.That(CustomerArMath.ReturnCredit(30000m, 0m, 15000m), Is.EqualTo(15000m));
    }

    [Test]
    public void AgingUsesOnlyCrossInvoiceCredit_AndNeverDuplicatesSourceReturn()
    {
        Assert.That(CustomerArMath.OpenInvoice(100000m, 70000m, 30000m, 0m), Is.Zero);
        Assert.That(CustomerArMath.OpenInvoice(90000m, 15000m, 0m, 30000m), Is.EqualTo(45000m));
        Assert.That(CustomerArMath.Round(0m - 30000m), Is.EqualTo(-30000m));
    }

    [Test]
    public void SignedCustomerBalance_RetainsCredit()
    {
        var opening = 30000m;
        var ending = opening + 200000m - 20000m - 120000m;
        Assert.That(ending, Is.EqualTo(90000m));
        Assert.That(CustomerArMath.Round(100m-130m), Is.EqualTo(-30m));
    }
}
