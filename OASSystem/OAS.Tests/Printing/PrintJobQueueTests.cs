using System.Text.Json;
using NUnit.Framework;
using OAS.API.Printing;
using OAS.Contracts.Printing;

namespace OAS.Tests.Printing;

[TestFixture]
public sealed class PrintJobQueueTests
{
    [Test]
    public void Lease_Returns_Queued_Job_Then_Hides_It_Until_Acknowledged()
    {
        var queue = new PrintJobQueue(TimeProvider.System);
        var job = CreateJob();
        var created = queue.Enqueue("DEFAULT", job);

        var leased = queue.TryLeaseNext("DEFAULT");
        var secondLease = queue.TryLeaseNext("DEFAULT");

        Assert.That(created.JobId, Is.EqualTo(job.JobId));
        Assert.That(leased?.JobId, Is.EqualTo(job.JobId));
        Assert.That(secondLease, Is.Null);
        Assert.That(queue.Complete(job.JobId), Is.True);
        Assert.That(queue.TryLeaseNext("DEFAULT"), Is.Null);
    }

    [Test]
    public void Lease_Does_Not_Cross_Workstations()
    {
        var queue = new PrintJobQueue(TimeProvider.System);
        var job = CreateJob();
        queue.Enqueue("DEFAULT", job);

        Assert.That(queue.TryLeaseNext("POS-02"), Is.Null);
        Assert.That(queue.TryLeaseNext("DEFAULT")?.JobId, Is.EqualTo(job.JobId));
    }

    private static DesktopPrintJobDto CreateJob() => new(
        Guid.NewGuid(),
        PrintDocumentTypes.ReceiptVoucher,
        null,
        null,
        1,
        false,
        "test-user",
        JsonSerializer.SerializeToElement(new { VoucherNumber = "RV-TEST" }));
}
