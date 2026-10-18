using NUnit.Framework;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class CustomerCreditApplicationTests
{
    [Test]
    public void PaidInvoice100000_Return30000_CreditApply15000_Leaves15000()
    {
        var source=CustomerCreditApplication.Apply(Guid.NewGuid(),Guid.NewGuid(),
            Guid.NewGuid(),Guid.NewGuid(),15000m,15000m,15000m,"tester",DateTime.UtcNow,"apply-1");
        Assert.Multiple(()=>
        {
            Assert.That(source.SettlementKind,Is.EqualTo(CustomerCreditSettlementKind.ApplyToInvoice));
            Assert.That(source.AppliedSourceBaseAmount,Is.EqualTo(15000m));
            Assert.That(30000m-source.AppliedSourceBaseAmount,Is.EqualTo(15000m));
            Assert.That(source.TargetSalesInvoiceId,Is.Not.Null);
            Assert.That(source.RefundPaymentVoucherLineId,Is.Null);
        });
    }
    [Test]
    public void Refund_MustUseValidPostedPaymentVoucherLineReference()
    {
        var refund=CustomerCreditApplication.Refund(Guid.NewGuid(),Guid.NewGuid(),
            Guid.NewGuid(),Guid.NewGuid(),30000m,30000m,"tester",DateTime.UtcNow);
        Assert.Multiple(()=>
        {
            Assert.That(refund.SettlementKind,Is.EqualTo(CustomerCreditSettlementKind.RefundByPaymentVoucherLine));
            Assert.That(refund.TargetSalesInvoiceId,Is.Null);
            Assert.That(refund.RefundPaymentVoucherLineId,Is.Not.Null);
        });
    }
    [Test]
    public void RefundReversal_RequiresCompensatingReceipt()
    {
        var refund=CustomerCreditApplication.Refund(Guid.NewGuid(),Guid.NewGuid(),
            Guid.NewGuid(),Guid.NewGuid(),30000m,30000m,"tester",DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(()=>refund.Reverse(DateTime.UtcNow,"tester","reason"));
        var receipt=Guid.NewGuid();
        refund.Reverse(DateTime.UtcNow,"tester","compensated",receipt);
        Assert.That(refund.Status,Is.EqualTo(CustomerCreditApplicationStatus.Reversed));
        Assert.That(refund.ReversalReceiptVoucherLineId,Is.EqualTo(receipt));
    }
    [Test]
    public void CreditApplicationReversal_RestoresEntitlementButNotOriginalReturnGL()
    {
        var apply=CustomerCreditApplication.Apply(Guid.NewGuid(),Guid.NewGuid(),
            Guid.NewGuid(),Guid.NewGuid(),15000m,15000m,15000m,"tester",DateTime.UtcNow);
        apply.Reverse(DateTime.UtcNow,"tester","reassign");
        Assert.That(apply.Status,Is.EqualTo(CustomerCreditApplicationStatus.Reversed));
        Assert.That(apply.AppliedSourceBaseAmount,Is.EqualTo(15000m));
        Assert.Throws<InvalidOperationException>(()=>apply.Reverse(DateTime.UtcNow,"tester","repeated"));
    }
    [Test]
    public void CreditApplications_RejectNonPositiveAndIncompleteAmounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>CustomerCreditApplication.Apply(
            Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),0m,100m,100m,"tester",DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CustomerCreditApplication.Refund(
            Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),10m,-10m,"tester",DateTime.UtcNow));
    }
}
