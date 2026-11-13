using System.Reflection;
using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseOrderPricingGateTests
{
    [Test]
    public void Unpersisted_draft_may_contain_zero_price_but_cannot_be_submitted()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 0m);
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
        Assert.That(order.Lines.Single().UnitPrice, Is.Zero);

        var ex = Assert.Throws<DomainException>(() => order.Submit(DateTimeOffset.UtcNow, "user"));
        Assert.That(ex!.Message, Does.Contain("البند 1"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
    }

    [Test]
    public void Save_rejects_a_zero_price_line_before_the_order_is_persisted()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 20m);
        AddLine(order, 2, price: 0m);

        var ex = Assert.Throws<DomainException>(() => order.ValidatePricingForSave());
        Assert.That(ex!.Message, Does.Contain("البند 2"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
    }

    [Test]
    public void Save_rejects_a_full_discount_even_with_a_positive_price()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 100m, discount: 100m);

        var ex = Assert.Throws<DomainException>(() => order.ValidatePricingForSave());
        Assert.That(ex!.Message, Does.Contain("صافي"));
    }

    [Test]
    public void Save_accepts_an_order_with_positive_base_inventory_cost()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 40m, discount: 5m);
        Assert.DoesNotThrow(() => order.ValidatePricingForSave());
    }

    [Test]
    public void Submission_checks_every_line_including_second_line()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 50m);
        AddLine(order, 2, price: 0m);

        var ex = Assert.Throws<DomainException>(() => order.Submit(DateTimeOffset.UtcNow, "user"));
        Assert.That(ex!.Message, Does.Contain("البند 2"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
    }

    [Test]
    public void Submission_rejects_positive_price_when_full_discount_makes_net_zero()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 100m, discount: 100m);
        Assert.That(order.Lines.Single().NetAmount, Is.Zero);

        var ex = Assert.Throws<DomainException>(() => order.Submit(DateTimeOffset.UtcNow, "user"));
        Assert.That(ex!.Message, Does.Contain("صافي"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
    }

    [Test]
    public void Submission_rejects_tiny_price_that_rounds_to_zero_in_base_currency()
    {
        var order = CreateOrder(exchangeRate: 0.1m);
        AddLine(order, 1, price: 0.0001m);
        Assert.That(order.Lines.Single().NetAmount, Is.GreaterThan(0m));

        var ex = Assert.Throws<DomainException>(() => order.Submit(DateTimeOffset.UtcNow, "user"));
        Assert.That(ex!.Message, Does.Contain("التقريب"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
    }

    [Test]
    public void Submission_rejects_quantity_rounded_to_zero_in_base_inventory_units()
    {
        var order = CreateOrder();
        order.AddLine(PurchaseOrderLine.Create(Guid.NewGuid(), order.Id, 1, Guid.NewGuid(), null,
            Guid.NewGuid(), 0.0001m, "PROD-SMALL", "Fractional product", "Each", 1m,
            100m, 0m, 0m, TaxCalculationMode.Exclusive, null, null));
        Assert.That(order.Lines.Single().BaseQuantity, Is.Zero);

        var ex = Assert.Throws<DomainException>(() => order.Submit(DateTimeOffset.UtcNow, "user"));
        Assert.That(ex!.Message, Does.Contain("الوحدة الأساسية"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
    }

    [Test]
    public void Priced_purchase_order_can_be_submitted_approved_and_sent()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 10m, discount: 1m);
        AddLine(order, 2, price: 40m);

        order.Submit(DateTimeOffset.UtcNow, "requester");
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.PendingApproval));
        order.Approve(DateTimeOffset.UtcNow, "approver");
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Approved));
        order.Send(DateTimeOffset.UtcNow, "sender");
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Sent));
    }

    [Test]
    public void Approval_and_send_recheck_legacy_unpriced_lines()
    {
        var order = CreateOrder();
        AddLine(order, 1, price: 10m);
        order.Submit(DateTimeOffset.UtcNow, "requester");
        var line = order.Lines.Single();

        // Simulate an old pending approval row imported before the pricing gate.
        ChangeUnitPrice(line, 0m);
        Assert.Throws<DomainException>(() => order.Approve(DateTimeOffset.UtcNow, "approver"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.PendingApproval));

        ChangeUnitPrice(line, 10m);
        order.Approve(DateTimeOffset.UtcNow, "approver");
        ChangeUnitPrice(line, 0m);
        Assert.Throws<DomainException>(() => order.Send(DateTimeOffset.UtcNow, "sender"));
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Approved));
    }

    private static PurchaseOrder CreateOrder(decimal exchangeRate = 1m) =>
        PurchaseOrder.Create(Guid.NewGuid(), "PO-PRICE-TEST", Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 11, 11), null, Guid.NewGuid(), exchangeRate,
            new DateOnly(2026, 11, 11), TaxCalculationMode.Exclusive, 0, null);

    private static void AddLine(PurchaseOrder order, int sequence, decimal price, decimal discount = 0m)
    {
        order.AddLine(PurchaseOrderLine.Create(Guid.NewGuid(), order.Id, sequence, Guid.NewGuid(), null,
            Guid.NewGuid(), 1m, $"PROD-{sequence}", $"Product {sequence}", "Each", 1m,
            price, discount, 0m, TaxCalculationMode.Exclusive, null, null));
    }

    private static void ChangeUnitPrice(PurchaseOrderLine line, decimal price) =>
        typeof(PurchaseOrderLine).GetProperty(nameof(PurchaseOrderLine.UnitPrice), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(line, price);
}
