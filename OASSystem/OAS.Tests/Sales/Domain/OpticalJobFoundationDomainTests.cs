using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Domain;

[TestFixture]
public sealed class OpticalJobFoundationDomainTests
{
    [Test]
    public void ValidProductionLifecycle_ReachesReadyForDelivery()
    {
        var job = CreateJobWithLine();
        job.MarkMaterialsAvailable();
        job.MarkMaterialsIssued();
        job.Start(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
        job.SendToQualityControl();
        job.PassQualityControl();
        job.MarkReadyForDelivery(new DateTimeOffset(2026, 10, 4, 13, 0, 0, TimeSpan.Zero));

        Assert.That(job.Status, Is.EqualTo(OpticalJobStatus.ReadyForDelivery));
    }

    [Test]
    public void ReadyForDelivery_RequiresIndependentQualityControlPass()
    {
        var job = CreateJobWithLine();
        job.MarkMaterialsAvailable();
        job.Start(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));

        Assert.Throws<DomainException>(() =>
            job.MarkReadyForDelivery(new DateTimeOffset(2026, 10, 4, 13, 0, 0, TimeSpan.Zero)));

        job.SendToQualityControl();
        Assert.That(job.Status, Is.EqualTo(OpticalJobStatus.AwaitingQC));
        job.PassQualityControl();
        Assert.That(job.Status, Is.EqualTo(OpticalJobStatus.QCPassed));
        job.MarkReadyForDelivery(new DateTimeOffset(2026, 10, 4, 13, 0, 0, TimeSpan.Zero));
        job.MarkDelivered(new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(job.Status, Is.EqualTo(OpticalJobStatus.Delivered));
            Assert.That(job.IsActive, Is.False);
        });
    }

    [Test]
    public void JobCannotStartBeforeMaterialsAreAvailable()
    {
        var job = CreateJobWithLine();
        Assert.Throws<DomainException>(() => job.Start(DateTimeOffset.UtcNow));
    }

    [Test]
    public void DeliverBeforeReady_IsRejected()
    {
        var job = CreateJobWithLine();
        job.MarkMaterialsAvailable();
        Assert.Throws<DomainException>(() => job.MarkDelivered(DateTimeOffset.UtcNow));
    }

    [Test]
    public void FailedQualityControl_ReturnsJobToProduction()
    {
        var job = CreateJobWithLine();
        job.MarkMaterialsAvailable();
        job.Start(DateTimeOffset.UtcNow);
        job.SendToQualityControl();

        job.FailQualityControl(OpticalQcFailureAction.Rework);

        Assert.That(job.Status, Is.EqualTo(OpticalJobStatus.InProduction));
    }

    [Test]
    public void QualityControl_CannotPassWhenAnyRequiredItemFailed()
    {
        var qc = OpticalQualityCheck.Create(Guid.NewGuid(), Guid.NewGuid(), 1);
        var item = OpticalQualityCheckItem.Create(Guid.NewGuid(), qc.Id, "POWER", "Power", 1);
        item.SetResult(OpticalQualityCheckItemResult.Fail, "Power mismatch");
        qc.AddItem(item);

        Assert.Throws<DomainException>(() => qc.CompletePassed(
            Guid.NewGuid(), DateTimeOffset.UtcNow, allItemsPassed: false));
    }

    [Test]
    public void Remake_FollowsReadyProductionQcCompletedLifecycle()
    {
        var remake = OpticalJobRemake.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, null, Guid.NewGuid(), Guid.NewGuid(),
            1m, "QC remake", Guid.NewGuid(), DateTimeOffset.UtcNow);

        remake.MarkReady();
        remake.Start(DateTimeOffset.UtcNow);
        remake.SendToQc();
        remake.Complete(DateTimeOffset.UtcNow);

        Assert.That(remake.Status, Is.EqualTo(OpticalRemakeStatus.Completed));
    }

    [Test]
    public void DeliveredJob_CannotBeCancelledOrReassigned()
    {
        var job = CreateJobWithLine();
        job.MarkMaterialsAvailable();
        job.Start(DateTimeOffset.UtcNow);
        job.SendToQualityControl();
        job.PassQualityControl();
        job.MarkReadyForDelivery(DateTimeOffset.UtcNow);
        job.MarkDelivered(DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.Throws<DomainException>(() => job.Cancel());
            Assert.Throws<DomainException>(() => job.AssignTechnician(Guid.NewGuid(), DateTimeOffset.UtcNow));
        });
    }

    [Test]
    public void SameCustomerOrderLine_CannotBeAddedTwice()
    {
        var job = OpticalJob.Create(Guid.NewGuid(), "OJ-1", Guid.NewGuid(), null, Guid.NewGuid());
        var orderLineId = Guid.NewGuid();
        job.AddLine(OpticalJobLine.Create(Guid.NewGuid(), job.Id, orderLineId, Guid.NewGuid(), 1,
            OpticalJobLineType.Lens, EyeSide.RightOD, null, "OD lens", 1m, true));

        Assert.Throws<DomainException>(() => job.AddLine(OpticalJobLine.Create(
            Guid.NewGuid(), job.Id, orderLineId, Guid.NewGuid(), 2,
            OpticalJobLineType.Lens, EyeSide.RightOD, null, "duplicate", 1m, true)));
    }

    private static OpticalJob CreateJobWithLine()
    {
        var job = OpticalJob.Create(Guid.NewGuid(), "OJ-TEST", Guid.NewGuid(), null, Guid.NewGuid());
        job.AddLine(OpticalJobLine.Create(Guid.NewGuid(), job.Id, Guid.NewGuid(), Guid.NewGuid(), 1,
            OpticalJobLineType.Lens, EyeSide.LeftOS, null, "OS lens", 1m, true));
        return job;
    }
}
