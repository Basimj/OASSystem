using NUnit.Framework;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Tests.Sales.Domain;

[TestFixture]
public sealed class SalesPaymentPlanAndOrderDomainTests
{
    [TestCase(SalesPaymentPlan.FullNow, SalesPaymentTermType.Immediate)]
    [TestCase(SalesPaymentPlan.PartialNow, SalesPaymentTermType.Immediate)]
    [TestCase(SalesPaymentPlan.PayOnPickup, SalesPaymentTermType.Immediate)]
    [TestCase(SalesPaymentPlan.AccountCredit, SalesPaymentTermType.Credit)]
    public void PaymentPlan_MapsToExpectedPaymentTerm(
        SalesPaymentPlan paymentPlan,
        SalesPaymentTermType expectedTerm) =>
        Assert.That(SalesPaymentPlanPolicy.ToPaymentTermType(paymentPlan), Is.EqualTo(expectedTerm));

    [Test]
    public void AccountCredit_OrderUsesCreditTermAndKeepsTermDays()
    {
        var order = CreateOrder(SalesPaymentPlan.AccountCredit, paymentTermDays: 30);

        Assert.Multiple(() =>
        {
            Assert.That(order.PaymentPlan, Is.EqualTo(SalesPaymentPlan.AccountCredit));
            Assert.That(order.PaymentTermType, Is.EqualTo(SalesPaymentTermType.Credit));
            Assert.That(order.PaymentTermDaysSnapshot, Is.EqualTo(30));
        });
    }

    [TestCase(SalesPaymentPlan.FullNow)]
    [TestCase(SalesPaymentPlan.PartialNow)]
    [TestCase(SalesPaymentPlan.PayOnPickup)]
    public void NonCreditPaymentPlans_DoNotCreateCreditTerms(SalesPaymentPlan paymentPlan)
    {
        var order = CreateOrder(paymentPlan, paymentTermDays: 30);

        Assert.Multiple(() =>
        {
            Assert.That(order.PaymentTermType, Is.EqualTo(SalesPaymentTermType.Immediate));
            Assert.That(order.PaymentTermDaysSnapshot, Is.Zero);
        });
    }

    [Test]
    public void PaymentPlan_CannotBeChangedAfterConfirmation()
    {
        var order = CreateOrder(SalesPaymentPlan.FullNow);
        order.AddLine(CreateServiceLine(order.Id, requiresProduction: false));
        order.Confirm(CustomerOrderStatus.Confirmed, DateTimeOffset.UtcNow, "tester");

        Assert.Throws<DomainException>(() =>
            order.ChangePaymentPlan(SalesPaymentPlan.PayOnPickup));
    }

    [Test]
    public void ProductionOrder_UsesExplicitLifecycle()
    {
        var order = CreateOrder(SalesPaymentPlan.PartialNow);
        order.AddLine(CreateLensLine(order.Id));
        order.Confirm(CustomerOrderStatus.ReadyForProduction, DateTimeOffset.UtcNow, "tester");

        order.MarkInProduction();
        order.MarkReadyForDelivery();
        order.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(CustomerOrderStatus.Completed));
            Assert.That(order.IsActive, Is.False);
        });
    }

    [Test]
    public void NonProductionOrder_CanMoveDirectlyFromConfirmedToReadyForDelivery()
    {
        var order = CreateOrder(SalesPaymentPlan.PayOnPickup);
        order.AddLine(CreateServiceLine(order.Id, requiresProduction: false));
        order.Confirm(CustomerOrderStatus.Confirmed, DateTimeOffset.UtcNow, "tester");

        order.MarkReadyForDelivery();

        Assert.That(order.Status, Is.EqualTo(CustomerOrderStatus.ReadyForDelivery));
    }

    [Test]
    public void Complete_BeforeReadyForDelivery_IsRejected()
    {
        var order = CreateOrder(SalesPaymentPlan.FullNow);
        order.AddLine(CreateServiceLine(order.Id, requiresProduction: false));
        order.Confirm(CustomerOrderStatus.Confirmed, DateTimeOffset.UtcNow, "tester");

        Assert.Throws<DomainException>(order.Complete);
    }

    [Test]
    public void LegacyCreditTerm_BackfillsToAccountCreditSemantics()
    {
        var order = CreateLegacyOrder(SalesPaymentTermType.Credit, paymentTermDays: 15);

        Assert.Multiple(() =>
        {
            Assert.That(order.PaymentPlan, Is.EqualTo(SalesPaymentPlan.AccountCredit));
            Assert.That(order.PaymentTermType, Is.EqualTo(SalesPaymentTermType.Credit));
            Assert.That(order.PaymentTermDaysSnapshot, Is.EqualTo(15));
        });
    }

    [Test]
    public void SalesInvoice_PreservesPaymentPlanSnapshotAndDueDate()
    {
        var currencyId = Guid.NewGuid();
        var invoice = SalesInvoice.Create(
            Guid.NewGuid(), "SI-PAYMENT-001", Guid.NewGuid(), Guid.NewGuid(), null,
            new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 4),
            currencyId, "YER", "ر.ي", 2, 1m, new DateOnly(2026, 10, 4),
            ExchangeRateType.Accounting, ExchangeRateSource.System, TaxCalculationMode.Exclusive,
            SalesPaymentPlan.AccountCredit, 30, currencyId, "YER", 2, "credit invoice");

        Assert.Multiple(() =>
        {
            Assert.That(invoice.PaymentPlan, Is.EqualTo(SalesPaymentPlan.AccountCredit));
            Assert.That(invoice.PaymentTermType, Is.EqualTo(SalesPaymentTermType.Credit));
            Assert.That(invoice.DueDate, Is.EqualTo(new DateOnly(2026, 11, 3)));
        });
    }

    private static CustomerOrder CreateOrder(SalesPaymentPlan paymentPlan, int paymentTermDays = 0)
    {
        var currencyId = Guid.NewGuid();
        return CustomerOrder.Create(
            Guid.NewGuid(), "CO-PAYMENT-001", Guid.NewGuid(), null,
            new DateOnly(2026, 10, 4), null, currencyId, "YER", "ر.ي", 2,
            1m, new DateOnly(2026, 10, 4), ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, paymentPlan, paymentTermDays, null);
    }

    private static CustomerOrder CreateLegacyOrder(SalesPaymentTermType paymentTermType, int paymentTermDays)
    {
        var currencyId = Guid.NewGuid();
        return CustomerOrder.Create(
            Guid.NewGuid(), "CO-LEGACY-001", Guid.NewGuid(), null,
            new DateOnly(2026, 10, 4), null, currencyId, "YER", "ر.ي", 2,
            1m, new DateOnly(2026, 10, 4), ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, paymentTermType, paymentTermDays, null);
    }

    private static CustomerOrderLine CreateServiceLine(Guid orderId, bool requiresProduction) =>
        CustomerOrderLine.Create(
            Guid.NewGuid(), orderId, 1, null, SalesLineType.Service,
            null, null, "Service", 1m, 10m, 10m,
            SalesDiscountType.None, null, null, null, null, requiresProduction, null,
            TaxCalculationMode.Exclusive, 2);

    private static CustomerOrderLine CreateLensLine(Guid orderId) =>
        CustomerOrderLine.Create(
            Guid.NewGuid(), orderId, 1, Guid.NewGuid(), SalesLineType.Lens,
            Guid.NewGuid(), Guid.NewGuid(), "OD Lens", 1m, 100m, 100m,
            SalesDiscountType.None, null, null, null, null, true, null,
            TaxCalculationMode.Exclusive, 2);
}
