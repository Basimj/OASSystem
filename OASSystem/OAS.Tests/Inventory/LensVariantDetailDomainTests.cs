using NUnit.Framework;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Exceptions;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class LensVariantDetailDomainTests
{
    [Test]
    public void ExactOpticalIdentity_MatchesOnlySameSphCylAdd()
    {
        var detail = LensVariantDetail.Create(Guid.NewGuid(), Guid.NewGuid(), -2.00m, -0.75m, 1.50m, 8.60m, 14.20m);

        Assert.Multiple(() =>
        {
            Assert.That(detail.Matches(-2.00m, -0.75m, 1.50m), Is.True);
            Assert.That(detail.Matches(-2.25m, -0.75m, 1.50m), Is.False);
            Assert.That(detail.Matches(-2.00m, -0.50m, 1.50m), Is.False);
            Assert.That(detail.Matches(-2.00m, -0.75m, 2.00m), Is.False);
        });
    }

    [Test]
    public void InactiveVariantDetail_DoesNotMatch()
    {
        var detail = LensVariantDetail.Create(Guid.NewGuid(), Guid.NewGuid(), -1m, -0.5m, null);
        detail.SetActive(false);
        Assert.That(detail.Matches(-1m, -0.5m, null), Is.False);
    }

    [TestCase(-0.25, null, null)]
    [TestCase(null, 0, null)]
    [TestCase(null, null, 0)]
    public void InvalidOpticalIdentity_IsRejected(double? add, double? baseCurve, double? diameter)
    {
        Assert.Throws<DomainException>(() => LensVariantDetail.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            add.HasValue ? (decimal?)add.Value : null,
            baseCurve.HasValue ? (decimal?)baseCurve.Value : null,
            diameter.HasValue ? (decimal?)diameter.Value : null));
    }

    [Test]
    public void InventoryLensIdentity_DoesNotContainEyeSide()
    {
        var propertyNames = typeof(LensVariantDetail).GetProperties().Select(x => x.Name).ToArray();
        Assert.That(propertyNames, Does.Not.Contain("Eye"));
        Assert.That(propertyNames, Does.Not.Contain("RightLeft"));
    }
}
