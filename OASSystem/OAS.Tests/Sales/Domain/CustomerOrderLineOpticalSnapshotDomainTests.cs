using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Domain;

[TestFixture]
public sealed class CustomerOrderLineOpticalSnapshotDomainTests
{
    [Test]
    public void ManualMeasurements_DoNotRequirePrescriptionRevision()
    {
        var snapshot = CustomerOrderLineOpticalSnapshot.CreateManual(
            Guid.NewGuid(), Guid.NewGuid(), EyeSide.RightOD,
            sph: -2.00m, cyl: -0.75m, axis: 90, add: 1.50m, pd: 31.5m);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.MeasurementSource, Is.EqualTo(OpticalMeasurementSource.Manual));
            Assert.That(snapshot.PrescriptionRevisionId, Is.Null);
            Assert.That(snapshot.Eye, Is.EqualTo(EyeSide.RightOD));
            Assert.That(snapshot.SPH, Is.EqualTo(-2.00m));
        });
    }

    [Test]
    public void StoredPrescription_RequiresRevision()
    {
        Assert.Throws<DomainException>(() => CustomerOrderLineOpticalSnapshot.CreateStoredPrescription(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, EyeSide.LeftOS, -1m, -0.5m));
    }

    [TestCase(-1)]
    [TestCase(181)]
    public void AxisOutsideRange_IsRejected(short axis)
    {
        Assert.Throws<DomainException>(() => CustomerOrderLineOpticalSnapshot.CreateManual(
            Guid.NewGuid(), Guid.NewGuid(), EyeSide.RightOD, -1m, -0.5m, axis));
    }

    [Test]
    public void Snapshot_CannotBeChangedAfterOrderLeavesDraft()
    {
        var snapshot = CustomerOrderLineOpticalSnapshot.CreateManual(
            Guid.NewGuid(), Guid.NewGuid(), EyeSide.RightOD, -1m, -0.5m, 90);

        Assert.Throws<DomainException>(() => snapshot.UpdateWhileDraft(
            CustomerOrderStatus.Confirmed,
            OpticalMeasurementSource.Manual,
            null,
            EyeSide.RightOD,
            -2m, -0.75m, 100, null, null, null, null, null, null, null,
            null, null, null, null));
    }

    [Test]
    public void Snapshot_IsHistoricalCopy_NotLivePrescriptionReferenceForManualSource()
    {
        var snapshot = CustomerOrderLineOpticalSnapshot.CreateManual(
            Guid.NewGuid(), Guid.NewGuid(), EyeSide.LeftOS, -3m, -1m, 80, 2m);

        var original = (snapshot.SPH, snapshot.CYL, snapshot.Axis, snapshot.ADD);
        Assert.That(original, Is.EqualTo((-3m, -1m, (short?)80, (decimal?)2m)));
    }
}
