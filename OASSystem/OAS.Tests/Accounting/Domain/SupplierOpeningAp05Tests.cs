using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class SupplierOpeningAp05Tests
{
    private readonly DateOnly _asOf = new(2026, 1, 1);

    [Test]
    public void Unknown_Historical_DueDate_DoesNotInvent_Date()
    {
        var item = NewItem(Guid.NewGuid(), "OLD-001", null);

        Assert.Multiple(() =>
        {
            Assert.That(item.DueDate, Is.Null);
            Assert.That(item.DueDateProvenance, Is.EqualTo(SupplierOpeningDueDateProvenance.Unknown));
            Assert.That(item.BaseOpenAmount, Is.EqualTo(15000m));
        });
    }

    [Test]
    public void Explicit_DueDate_Is_Preserved_Independently_Of_Supplier_Terms()
    {
        var dueDate = _asOf.AddDays(45);
        var item = NewItem(Guid.NewGuid(), "OLD-002", dueDate);
        Assert.That(item.DueDate, Is.EqualTo(dueDate));
        Assert.That(item.DueDateProvenance, Is.EqualTo(SupplierOpeningDueDateProvenance.Explicit));
    }

    [Test]
    public void Duplicate_Supplier_Legacy_Reference_Is_Rejected_Case_Insensitively()
    {
        var batch = NewBatch();
        var supplierId = Guid.NewGuid();
        batch.AddItem(NewItem(supplierId, " INV-100 ", null, batch.Id));

        Assert.Throws<InvalidOperationException>(() => batch.AddItem(NewItem(supplierId, "inv-100", null, batch.Id)));
    }

    [Test]
    public void Opening_Journal_Is_Linked_Only_Once()
    {
        var batch = NewBatch();
        batch.AddItem(NewItem(Guid.NewGuid(), "OLD-003", null, batch.Id));
        var journalId = Guid.NewGuid();
        batch.LinkExistingJournal(journalId, "accountant", DateTime.UtcNow);

        Assert.That(batch.Status, Is.EqualTo(SupplierOpeningBatchStatus.LinkedExistingJournal));
        Assert.That(batch.JournalEntryId, Is.EqualTo(journalId));
        Assert.Throws<InvalidOperationException>(() => batch.MarkPosted(Guid.NewGuid(), "accountant", DateTime.UtcNow));
    }

    [Test]
    public void Reversed_Opening_Application_Preserves_Audit_Trail()
    {
        var id = Guid.NewGuid();
        var amount = 3000m;
        var application = SupplierOpeningApplication.Create(
            id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount, amount,
            DateTime.UtcNow, "accountant", "AP05-RETRY-001");
        application.Reverse(DateTime.UtcNow, "manager", "Incorrect voucher link");

        Assert.Multiple(() =>
        {
            Assert.That(application.Status, Is.EqualTo(SupplierOpeningApplicationStatus.Reversed));
            Assert.That(application.AppliedTargetBaseAmount, Is.EqualTo(amount));
            Assert.That(application.ReversedAtUtc, Is.Not.Null);
            Assert.That(application.ReversalReason, Is.EqualTo("Incorrect voucher link"));
        });
        Assert.Throws<InvalidOperationException>(() => application.Reverse(DateTime.UtcNow, "manager", "duplicate"));
    }

    private SupplierOpeningBatch NewBatch() => SupplierOpeningBatch.Create(
        Guid.NewGuid(), "AP-OPEN-2026-000001", _asOf, Guid.NewGuid());

    private SupplierOpeningItem NewItem(Guid supplierId, string reference, DateOnly? dueDate, Guid? batchId = null) =>
        SupplierOpeningItem.Create(
            Guid.NewGuid(), batchId ?? Guid.NewGuid(), supplierId, reference, _asOf, dueDate,
            dueDate.HasValue ? SupplierOpeningDueDateProvenance.Explicit : SupplierOpeningDueDateProvenance.Unknown,
            Guid.NewGuid(), "YER", 2, 10m, 1500m, 15000m);
}
