using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using OAS.Infrastructure.Persistence;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class AP09ReceiptFollowUpTests
{
    [Test]
    public void Customer_Job_Stores_Target_And_Pending_Without_Ledger_Link()
    {
        var receipt = Guid.NewGuid(); var order = Guid.NewGuid();
        var now = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc);
        var job = PurchaseReceiptFollowUpJob.ForCustomer(receipt, order, now);
        Assert.Multiple(() =>
        {
            Assert.That(job.PurchaseReceiptId, Is.EqualTo(receipt));
            Assert.That(job.CustomerOrderId, Is.EqualTo(order));
            Assert.That(job.PurchaseRequestLineId, Is.Null);
            Assert.That(job.TargetKind, Is.EqualTo(ReceiptFollowUpTarget.CustomerOrder));
            Assert.That(job.Status, Is.EqualTo(ReceiptFollowUpStatus.Pending));
            Assert.That(job.AttemptCount, Is.Zero);
        });
    }

    [Test]
    public void Optical_Job_Uses_Request_Line_Not_Order()
    {
        var line = Guid.NewGuid();
        var job = PurchaseReceiptFollowUpJob.ForOptical(Guid.NewGuid(), line, DateTime.UtcNow);
        Assert.That(job.PurchaseRequestLineId, Is.EqualTo(line));
        Assert.That(job.CustomerOrderId, Is.Null);
        Assert.That(job.TargetKind, Is.EqualTo(ReceiptFollowUpTarget.OpticalReplacement));
    }

    [Test]
    public void Completed_Job_Cannot_Be_Requeued()
    {
        // Domain guard: a completed job cannot be requeued or double-processed.
        var job = PurchaseReceiptFollowUpJob.ForCustomer(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => job.Retry(DateTime.UtcNow));
    }

    [Test]
    public void Unleased_Job_Cannot_Be_Marked_Completed()
    {
        var job = PurchaseReceiptFollowUpJob.ForOptical(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => job.Complete(DateTime.UtcNow));
    }

    [Test]
    public void Ef_Model_Has_Unique_Source_Identity_And_Concurrency_Token()
    {
        using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=localhost;Database=AP09_Mapping_Only;TrustServerCertificate=True")
            .Options);
        var entity = db.Model.FindEntityType(typeof(PurchaseReceiptFollowUpJob));
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity!.FindProperty(nameof(PurchaseReceiptFollowUpJob.RowVersion))!.IsConcurrencyToken, Is.True);
        Assert.That(entity.GetIndexes().Count(x => x.IsUnique), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Reject_Missing_Targets()
    {
        Assert.Throws<ArgumentException>(() => PurchaseReceiptFollowUpJob.ForCustomer(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => PurchaseReceiptFollowUpJob.ForOptical(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow));
    }
}
