using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchasingDomainWorkflowTests
{
    [Test]
    public void PurchaseRequest_SubmitApprove_RequiresLinesAndMovesLifecycle()
    {
        var request = PurchaseRequest.Create(
            Guid.NewGuid(),
            "PR-TEST-001",
            PurchaseRequestType.Replenishment,
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 10),
            "replenishment",
            null,
            "tester");

        Assert.Throws<DomainException>(() =>
            request.Submit(DateTimeOffset.UtcNow, "tester"));

        request.AddLine(PurchaseRequestLine.Create(
            Guid.NewGuid(),
            request.Id,
            1,
            Guid.NewGuid(),
            5m));

        request.Submit(DateTimeOffset.UtcNow, "tester");
        request.Approve(DateTimeOffset.UtcNow, "approver");

        Assert.That(request.Status, Is.EqualTo(PurchaseRequestStatus.Approved));
    }

    [Test]
    public void PurchaseReceipt_ConfirmAndPost_CapturesInventoryAndJournalReferences()
    {
        var receipt = PurchaseReceipt.Create(
            Guid.NewGuid(),
            "GRN-TEST-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 2),
            new DateOnly(2026, 10, 2),
            null,
            null);

        var line = PurchaseReceiptLine.Create(
            Guid.NewGuid(),
            receipt.Id,
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            10m,
            0m,
            10m,
            9m,
            1m,
            1m,
            20m,
            null,
            null,
            null);

        receipt.AddLine(line);
        receipt.Confirm(DateTimeOffset.UtcNow, "receiver");
        var inventoryTransactionId = Guid.NewGuid();
        var journalEntryId = Guid.NewGuid();
        receipt.MarkPosted(inventoryTransactionId, journalEntryId, DateTimeOffset.UtcNow, "poster");

        Assert.That(receipt.Status, Is.EqualTo(PurchaseReceiptStatus.Posted));
        Assert.That(receipt.InventoryTransactionId, Is.EqualTo(inventoryTransactionId));
        Assert.That(receipt.JournalEntryId, Is.EqualTo(journalEntryId));
        Assert.That(line.TotalAcceptedCost, Is.EqualTo(180m));
    }

    [Test]
    public void PurchaseReceiptLine_AcceptedPlusRejectedMustEqualReceived()
    {
        Assert.Throws<DomainException>(() => PurchaseReceiptLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            10m,
            0m,
            10m,
            8m,
            1m,
            1m,
            20m,
            null,
            null,
            null));
    }

    [Test]
    public void PurchaseInvoice_MatchApprovalLifecycle_ThenPost()
    {
        var invoice = PurchaseInvoice.Create(
            Guid.NewGuid(),
            "PI-TEST-001",
            "SUP-INV-1",
            Guid.NewGuid(),
            new DateOnly(2026, 10, 3),
            new DateOnly(2026, 10, 3),
            Guid.NewGuid(),
            1m,
            new DateOnly(2026, 10, 3),
            TaxCalculationMode.Exclusive,
            null);

        invoice.AddLine(PurchaseInvoiceLine.Create(
            Guid.NewGuid(),
            invoice.Id,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "VAR-001",
            "Variant 1",
            2m,
            50m,
            0m,
            0m,
            TaxCalculationMode.Exclusive,
            1m));

        invoice.Confirm(DateTimeOffset.UtcNow, "tester");
        invoice.MarkMatchApprovalRequired();
        Assert.That(invoice.Status, Is.EqualTo(PurchaseInvoiceStatus.PendingMatchApproval));

        invoice.MarkMatchApproved();
        var journalId = Guid.NewGuid();
        invoice.MarkPosted(journalId, DateTimeOffset.UtcNow, "poster");

        Assert.That(invoice.Status, Is.EqualTo(PurchaseInvoiceStatus.Posted));
        Assert.That(invoice.JournalEntryId, Is.EqualTo(journalId));
        Assert.That(invoice.TotalAmount, Is.EqualTo(100m));
    }

    [Test]
    public void PurchaseRequest_PartiallyConverted_Cancel_CancelsOnlyRemainingWorkflow()
    {
        var request = PurchaseRequest.Create(
            Guid.NewGuid(), "PR-TEST-PARTIAL", PurchaseRequestType.Replenishment, Guid.NewGuid(), null,
            new DateOnly(2026, 10, 5), null, null, null, "requester");
        request.AddLine(PurchaseRequestLine.Create(Guid.NewGuid(), request.Id, 1, Guid.NewGuid(), 10m));
        request.Submit(DateTimeOffset.UtcNow, "requester");
        request.Approve(DateTimeOffset.UtcNow, "approver");
        request.MarkConversion(4m, 10m);

        Assert.That(request.Status, Is.EqualTo(PurchaseRequestStatus.PartiallyConverted));

        request.Cancel(DateTimeOffset.UtcNow, "requester", "Cancel remaining quantity");

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(PurchaseRequestStatus.Cancelled));
            Assert.That(request.CancellationReason, Is.EqualTo("Cancel remaining quantity"));
        });
    }

    [Test]
    public void PurchaseOrder_FullyReceived_RemainsSeparateFromClosed()
    {
        var order = PurchaseOrder.Create(Guid.NewGuid(), "PO-TEST-FULL", Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 10, 5), null, Guid.NewGuid(), 1m, new DateOnly(2026, 10, 5),
            TaxCalculationMode.Exclusive, 0, null);
        order.AddLine(PurchaseOrderLine.Create(Guid.NewGuid(), order.Id, 1, Guid.NewGuid(), null, Guid.NewGuid(), 1m,
            "P-1", "Product", "Each", 1m, 10m, 0m, 0m, TaxCalculationMode.Exclusive, null, null));
        order.Submit(DateTimeOffset.UtcNow, "user");
        order.Approve(DateTimeOffset.UtcNow, "approver");
        order.Send(DateTimeOffset.UtcNow, "sender");
        order.MarkReceived(true);

        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.FullyReceived));
        Assert.That(order.Status, Is.Not.EqualTo(PurchaseOrderStatus.Closed));

        order.Close(DateTimeOffset.UtcNow, "closer");
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Closed));
    }
}
