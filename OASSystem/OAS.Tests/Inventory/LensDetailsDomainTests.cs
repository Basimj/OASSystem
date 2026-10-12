using NUnit.Framework;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Exceptions;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class LensDetailsDomainTests
{
    [Test]
    public void Constructor_StoresExactOpticalMeasurementsForVariant()
    {
        var variantId = Guid.NewGuid();
        var lens = new LensDetails(
            variantId,
            "Single Vision",
            isPrescriptionLens: true,
            refractiveIndex: 1.56m,
            sph: -2.00m,
            cyl: -0.75m,
            add: 1.50m,
            baseCurve: 8.60m,
            diameter: 70m);

        Assert.Multiple(() =>
        {
            Assert.That(lens.ProductVariantId, Is.EqualTo(variantId));
            Assert.That(lens.SPH, Is.EqualTo(-2.00m));
            Assert.That(lens.CYL, Is.EqualTo(-0.75m));
            Assert.That(lens.ADD, Is.EqualTo(1.50m));
            Assert.That(lens.BaseCurve, Is.EqualTo(8.60m));
            Assert.That(lens.Diameter, Is.EqualTo(70m));
            Assert.That(lens.Matches(-2.00m, -0.75m, 1.50m), Is.True);
        });
    }

    [Test]
    public void Constructor_RejectsZeroRefractiveIndex() =>
        Assert.Throws<DomainException>(() => new LensDetails(Guid.NewGuid(), "Single Vision", refractiveIndex: 0m));

    [Test]
    public void UpdateDetails_RejectsNegativeAdd()
    {
        var lens = new LensDetails(Guid.NewGuid(), "Progressive", isPrescriptionLens: true);
        Assert.Throws<DomainException>(() => lens.UpdateDetails("Progressive", isPrescriptionLens: true, add: -0.25m));
    }

    [Test]
    public void UpdateDetails_ReplacesExactMeasurementsWithoutRangeNormalization()
    {
        var lens = new LensDetails(Guid.NewGuid(), "Single Vision", isPrescriptionLens: true, sph: -1m, cyl: 0m);
        lens.UpdateDetails("Single Vision", isPrescriptionLens: true, sph: -6.25m, cyl: -1.50m, add: null);
        Assert.Multiple(() =>
        {
            Assert.That(lens.SPH, Is.EqualTo(-6.25m));
            Assert.That(lens.CYL, Is.EqualTo(-1.50m));
            Assert.That(lens.ADD, Is.Null);
        });
    }
}
