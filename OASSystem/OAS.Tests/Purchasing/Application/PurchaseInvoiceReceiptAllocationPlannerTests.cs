using NUnit.Framework;
using OAS.Client.Purchasing.Common;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseInvoiceReceiptAllocationPlannerTests
{
    private static readonly Guid Po=Guid.NewGuid();
    private static readonly Guid Variant=Guid.NewGuid();
    private static readonly Guid InvoiceLine=Guid.NewGuid();
    private static readonly Guid ReceiptLine=Guid.NewGuid();

    [Test]
    public void Fully_received_line_is_allocated_to_the_selected_receipt()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlan(
            [new(InvoiceLine,Po,Variant,8m,1)],
            [new(ReceiptLine,Po,Variant,8m,0m)],out var allocations,out var error);
        Assert.That(ok,Is.True,error);
        Assert.That(allocations,Has.Count.EqualTo(1));
        Assert.Multiple(()=>
        {
            Assert.That(allocations[0].InvoiceLineId,Is.EqualTo(InvoiceLine));
            Assert.That(allocations[0].ReceiptLineId,Is.EqualTo(ReceiptLine));
            Assert.That(allocations[0].Quantity,Is.EqualTo(8m));
        });
    }

    [Test]
    public void Returned_quantities_are_not_available_for_invoice_matching()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlan(
            [new(InvoiceLine,Po,Variant,8m,1)],
            [new(ReceiptLine,Po,Variant,8m,2m)],out var allocations,out var error);
        Assert.That(ok,Is.False);
        Assert.That(allocations,Is.Empty);
        Assert.That(error,Does.Contain("أكبر"));
    }

    [Test]
    public void Wrong_product_or_order_line_cannot_be_matched()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlan(
            [new(InvoiceLine,Po,Variant,4m,1)],
            [new(ReceiptLine,Guid.NewGuid(),Variant,10m,0m)],out _,out _);
        Assert.That(ok,Is.False);
    }

    [Test]
    public void Multiple_invoice_lines_cannot_reuse_the_same_receipt_quantity()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlan(
            [new(InvoiceLine,Po,Variant,6m,1),new(Guid.NewGuid(),Po,Variant,5m,2)],
            [new(ReceiptLine,Po,Variant,10m,0m)],out _,out _);
        Assert.That(ok,Is.False);
    }

    [Test]
    public void Partial_invoice_may_use_less_than_accepted_quantity()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlan(
            [new(InvoiceLine,Po,Variant,3m,1)],
            [new(ReceiptLine,Po,Variant,10m,0m)],out var plan,out _);
        Assert.That(ok,Is.True);
        Assert.That(plan.Single().Quantity,Is.EqualTo(3m));
    }
}
