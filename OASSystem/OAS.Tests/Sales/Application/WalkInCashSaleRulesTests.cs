using NUnit.Framework;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class WalkInCashSaleRulesTests
{
    [TestCase(SalesPaymentPlan.FullNow, false)]
    [TestCase(SalesPaymentPlan.PartialNow, true)]
    [TestCase(SalesPaymentPlan.AccountCredit, true)]
    public void WalkInCustomer_RequiresFullPayment(SalesPaymentPlan plan, bool expected)
    {
        var customerId = Guid.NewGuid();
        Assert.That(WalkInCashSaleRules.RequiresFullPayment(customerId, customerId, plan), Is.EqualTo(expected));
    }

    [Test]
    public void OtherCustomers_RetainExistingPaymentPlanRules()
    {
        var customerId = Guid.NewGuid();
        Assert.That(WalkInCashSaleRules.RequiresFullPayment(customerId, Guid.NewGuid(), SalesPaymentPlan.AccountCredit), Is.False);
        Assert.That(WalkInCashSaleRules.RequiresFullPayment(customerId, null, SalesPaymentPlan.PartialNow), Is.False);
    }
}
