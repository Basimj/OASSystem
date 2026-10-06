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
    public void V1Lifecycle_CanStartWhenMaterialsAreAvailable_AndFinishWithoutMandatoryQc()
    {
        var job = CreateJobWithLine();
        job.MarkMaterialsAvailable();
        job.Start(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
        job.MarkReadyForDelivery(new DateTimeOffset(2026, 10, 4, 13, 0, 0, TimeSpan.Zero));
        job.MarkDelivered();

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
    public void SameCustomerOrderLine_CannotBeAddedTwice()
    {
        var job = OpticalJob.Create(Guid.NewGuid(), "OJ-1", Guid.NewGuid(), null, Guid.NewGuid());
        var orderLineId = Guid.NewGuid();
        job.AddLine(OpticalJobLine.Create(Guid.NewGuid(), job.Id, orderLineId, Guid.NewGuid(), 1,
            SalesLineType.Lens, EyeSide.RightOD, "OD lens", 1m));

        Assert.Throws<DomainException>(() => job.AddLine(OpticalJobLine.Create(
            Guid.NewGuid(), job.Id, orderLineId, Guid.NewGuid(), 2,
            SalesLineType.Lens, EyeSide.RightOD, "duplicate", 1m)));
    }

    private static OpticalJob CreateJobWithLine()
    {
        var job = OpticalJob.Create(Guid.NewGuid(), "OJ-TEST", Guid.NewGuid(), null, Guid.NewGuid());
        job.AddLine(OpticalJobLine.Create(Guid.NewGuid(), job.Id, Guid.NewGuid(), Guid.NewGuid(), 1,
            SalesLineType.Lens, EyeSide.LeftOS, "OS lens", 1m));
        return job;
    }
}
