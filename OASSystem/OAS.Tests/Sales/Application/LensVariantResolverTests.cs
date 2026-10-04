using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Domain.Entities.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class LensVariantResolverTests
{
    [Test]
    public async Task ResolveAsync_ReturnsExactVariantForSameProduct()
    {
        var productId = Guid.NewGuid();
        var seed = new ProductVariant(productId, "SEED", 10m, 20m, variantName: "Any name");
        var exact = new ProductVariant(productId, "L-200-075", 10m, 20m, variantName: "Name is not used");
        var other = new ProductVariant(productId, "L-225-075", 10m, 20m);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([seed, exact, other]);
        var details = new FakeGenericRepository<LensVariantDetail, Guid>([
            LensVariantDetail.Create(Guid.NewGuid(), exact.Id, -2.00m, -0.75m, 1.50m),
            LensVariantDetail.Create(Guid.NewGuid(), other.Id, -2.25m, -0.75m, 1.50m)
        ]);
        var resolver = new LensVariantResolver(variants, details);

        var result = await resolver.ResolveAsync(new LensVariantMatchRequest(seed.Id, -2.00m, -0.75m, 1.50m));

        Assert.That(result.ProductVariantId, Is.EqualTo(exact.Id));
    }

    [Test]
    public void ResolveAsync_DifferentOpticalIdentity_IsNotTreatedAsAvailable()
    {
        var productId = Guid.NewGuid();
        var seed = new ProductVariant(productId, "SEED", 10m, 20m);
        var candidate = new ProductVariant(productId, "CANDIDATE", 10m, 20m);
        var resolver = new LensVariantResolver(
            new FakeGenericRepository<ProductVariant, Guid>([seed, candidate]),
            new FakeGenericRepository<LensVariantDetail, Guid>([
                LensVariantDetail.Create(Guid.NewGuid(), candidate.Id, -2.25m, -0.75m, 1.50m)
            ]));

        Assert.ThrowsAsync<ConflictException>(async () =>
            await resolver.ResolveAsync(new LensVariantMatchRequest(seed.Id, -2.00m, -0.75m, 1.50m)));
    }

    [Test]
    public void ResolveAsync_DuplicateExactIdentity_IsRejectedAsAmbiguous()
    {
        var productId = Guid.NewGuid();
        var seed = new ProductVariant(productId, "SEED", 10m, 20m);
        var first = new ProductVariant(productId, "A", 10m, 20m);
        var second = new ProductVariant(productId, "B", 10m, 20m);
        var resolver = new LensVariantResolver(
            new FakeGenericRepository<ProductVariant, Guid>([seed, first, second]),
            new FakeGenericRepository<LensVariantDetail, Guid>([
                LensVariantDetail.Create(Guid.NewGuid(), first.Id, -2m, -0.75m, null),
                LensVariantDetail.Create(Guid.NewGuid(), second.Id, -2m, -0.75m, null)
            ]));

        Assert.ThrowsAsync<ConflictException>(async () =>
            await resolver.ResolveAsync(new LensVariantMatchRequest(seed.Id, -2m, -0.75m, null)));
    }
}
