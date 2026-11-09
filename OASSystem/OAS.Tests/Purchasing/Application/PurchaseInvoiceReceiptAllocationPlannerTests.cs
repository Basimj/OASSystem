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

    [Test]
    public void Exceeded_available_quantity_identifies_the_exact_invoice_line()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(
            [new(InvoiceLine,Po,Variant,2m,1),new(Guid.NewGuid(),Po,Variant,5m,2)],
            [new(ReceiptLine,Po,Variant,4m,0m)],out _,out var issue,out _);
        Assert.That(ok,Is.False);
        Assert.That(issue,Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(issue!.LineSequence,Is.EqualTo(2));
            Assert.That(issue.AvailableQuantity,Is.EqualTo(2m));
            Assert.That(issue.RequestedQuantity,Is.EqualTo(5m));
            Assert.That(issue.Message,Does.Contain("5"));
            Assert.That(issue.Message,Does.Contain("2"));
        });
    }

    [Test]
    public void Externally_matched_quantity_is_not_available_to_new_invoice()
    {
        // Only 3 of the 8 accepted units remain after other invoices and returns.
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(
            [new(InvoiceLine,Po,Variant,8m,1)],
            [new(ReceiptLine,Po,Variant,3m,0m)],out _,out var issue,out _);
        Assert.That(ok,Is.False);
        Assert.That(issue!.AvailableQuantity,Is.EqualTo(3m));
    }

    [Test]
    public void Available_quantity_after_other_invoices_can_be_invoiced_without_error()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(
            [new(InvoiceLine,Po,Variant,3m,1)],
            [new(ReceiptLine,Po,Variant,3m,0m)],out var plan,out var issue,out _);
        Assert.That(ok,Is.True);
        Assert.That(issue,Is.Null);
        Assert.That(plan.Single().Quantity,Is.EqualTo(3m));
    }
    [Test]
    public void Two_similarly_named_order_lines_are_matched_independently()
    {
        var secondPo=Guid.NewGuid();
        var secondReceipt=Guid.NewGuid();
        var secondInvoiceLine=Guid.NewGuid();
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(
            [new(InvoiceLine,Po,Variant,10m,1,ReceiptLine),
             new(secondInvoiceLine,secondPo,Variant,5m,2,secondReceipt)],
            [new(ReceiptLine,Po,Variant,10m,0m),
             new(secondReceipt,secondPo,Variant,5m,0m)],
            out var plan,out var issue,out var error);
        Assert.That(ok,Is.True,error);
        Assert.That(issue,Is.Null);
        Assert.That(plan.Select(x=>x.Quantity).ToArray(),Is.EqualTo(new[]{10m,5m}));
        Assert.That(plan.Select(x=>x.ReceiptLineId).ToArray(),Is.EqualTo(new[]{ReceiptLine,secondReceipt}));
    }

    [Test]
    public void First_invoice_line_with_zero_remaining_does_not_borrow_from_second_line()
    {
        var secondPo=Guid.NewGuid();
        var secondReceipt=Guid.NewGuid();
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(
            [new(InvoiceLine,Po,Variant,10m,1,ReceiptLine),
             new(Guid.NewGuid(),secondPo,Variant,5m,2,secondReceipt)],
            [new(ReceiptLine,Po,Variant,0m,0m),
             new(secondReceipt,secondPo,Variant,5m,0m)],
            out _,out var issue,out _);
        Assert.That(ok,Is.False);
        Assert.That(issue!.LineSequence,Is.EqualTo(1));
        Assert.That(issue.AvailableQuantity,Is.EqualTo(0m));
        Assert.That(issue.Message,Does.Contain("صفر"));
    }

    [Test]
    public void Missing_order_line_association_has_explanatory_error_not_generic_zero()
    {
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(
            [new(InvoiceLine,Po,Variant,10m,1,ReceiptLine)],
            [new(ReceiptLine,Guid.NewGuid(),Variant,10m,0m)],
            out _,out var issue,out _);
        Assert.That(ok,Is.False);
        Assert.That(issue!.LineSequence,Is.EqualTo(1));
        Assert.That(issue.Message,Does.Contain("بند الاستلام"));
    }

    [Test]
    public void Pinned_receipt_line_cannot_spend_another_receipt_line_for_the_same_po_line()
    {
        var secondReceipt=Guid.NewGuid();
        var ok=PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(
            [new(InvoiceLine,Po,Variant,5m,1,ReceiptLine)],
            [new(ReceiptLine,Po,Variant,0m,0m),
             new(secondReceipt,Po,Variant,7m,0m)],
            out _,out var issue,out _);
        Assert.That(ok,Is.False);
        Assert.That(issue!.AvailableQuantity,Is.EqualTo(0m));
    }

}
