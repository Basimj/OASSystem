using NUnit.Framework;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Production;

namespace OAS.Tests.Sales;

[TestFixture]
public sealed class OpticalProductionDomainTests
{
    [Test]
    public void ProductionJob_PassedQualityControl_CanComplete()
    {
        var job = CreateJob();
        var material = OpticalProductionMaterial.Create(Guid.NewGuid(), job.Id, Guid.NewGuid(), 2m);
        material.SetIssueCost(12.5m);
        job.AddMaterial(material);

        job.Release(DateTimeOffset.UtcNow);
        job.Start(DateTimeOffset.UtcNow);
        job.SetMaterialIssue(Guid.NewGuid());
        job.SubmitQualityControl(DateTimeOffset.UtcNow, passed: true, notes: "OK");
        job.Complete(DateTimeOffset.UtcNow);

        Assert.That(job.Status, Is.EqualTo(OpticalProductionStatus.Completed));
        Assert.That(job.LastQcResult, Is.EqualTo(OpticalProductionQcResult.Passed));
        Assert.That(job.MaterialCostBase, Is.EqualTo(25m));
        Assert.That(job.IsActive, Is.False);
    }

    [Test]
    public void FailedQualityControl_WithBreakage_ClosesJobAndAllowsLinkedRemakeModel()
    {
        var job = CreateJob();
        var material = OpticalProductionMaterial.Create(Guid.NewGuid(), job.Id, Guid.NewGuid(), 1m);
        material.SetIssueCost(30m);
        job.AddMaterial(material);
        job.Release(DateTimeOffset.UtcNow);
        job.Start(DateTimeOffset.UtcNow);
        job.SetMaterialIssue(Guid.NewGuid());
        job.SubmitQualityControl(DateTimeOffset.UtcNow, passed: false, notes: "Lens broke", isBreakage: true);

        Assert.That(job.Status, Is.EqualTo(OpticalProductionStatus.QualityControlFailed));
        Assert.That(job.LastQcResult, Is.EqualTo(OpticalProductionQcResult.Failed));
        Assert.That(job.LastQcWasBreakage, Is.True);
        Assert.That(job.IsActive, Is.False);

        var remake = OpticalProductionJob.Create(
            Guid.NewGuid(), "OPJ-2026-000002", job.SalesInvoiceId, job.SalesInvoiceLineId,
            job.CustomerId, job.WarehouseId, new DateOnly(2026, 10, 4), null,
            "Remake", job.Id, job.RemakeNumber + 1);

        Assert.That(remake.IsRemake, Is.True);
        Assert.That(remake.RemakeOfJobId, Is.EqualTo(job.Id));
        Assert.That(remake.RemakeNumber, Is.EqualTo(1));
    }

    private static OpticalProductionJob CreateJob() => OpticalProductionJob.Create(
        Guid.NewGuid(),
        "OPJ-2026-000001",
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        new DateOnly(2026, 10, 3),
        new DateOnly(2026, 10, 5),
        "production test");
}
