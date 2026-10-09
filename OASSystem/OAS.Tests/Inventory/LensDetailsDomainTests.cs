using NUnit.Framework;
using OAS.Domain.Entities.Inventory;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class LensDetailsDomainTests
{
    [Test]
    public void Constructor_NormalizesReversedOpticalRanges()
    {
        var lens = new LensDetails(
            Guid.NewGuid(),
            "Single Vision",
            isPrescriptionLens: true,
            sphereMin: -0.25m,
            sphereMax: -6.00m,
            cylinderMin: 0m,
            cylinderMax: -4.00m,
            addMin: 3.50m,
            addMax: 0m);

        Assert.Multiple(() =>
        {
            Assert.That(lens.SphereMin, Is.EqualTo(-6.00m));
            Assert.That(lens.SphereMax, Is.EqualTo(-0.25m));
            Assert.That(lens.CylinderMin, Is.EqualTo(-4.00m));
            Assert.That(lens.CylinderMax, Is.EqualTo(0m));
            Assert.That(lens.AddMin, Is.EqualTo(0m));
            Assert.That(lens.AddMax, Is.EqualTo(3.50m));
        });
    }

    [Test]
    public void UpdateDetails_NormalizesReversedOpticalRanges()
    {
        var lens = new LensDetails(Guid.NewGuid(), "Single Vision", isPrescriptionLens: true);

        lens.UpdateDetails(
            "Single Vision",
            isPrescriptionLens: true,
            sphereMin: 4.00m,
            sphereMax: -8.00m,
            cylinderMin: 0m,
            cylinderMax: -6.00m,
            addMin: 4.00m,
            addMax: 0m);

        Assert.Multiple(() =>
        {
            Assert.That(lens.SphereMin, Is.EqualTo(-8.00m));
            Assert.That(lens.SphereMax, Is.EqualTo(4.00m));
            Assert.That(lens.CylinderMin, Is.EqualTo(-6.00m));
            Assert.That(lens.CylinderMax, Is.EqualTo(0m));
            Assert.That(lens.AddMin, Is.EqualTo(0m));
            Assert.That(lens.AddMax, Is.EqualTo(4.00m));
        });
    }
}
