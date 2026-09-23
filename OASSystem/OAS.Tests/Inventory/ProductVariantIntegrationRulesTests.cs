using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Products.Variants;
using OAS.Application.Inventory.Products.Variants.Commands.UpdateProductVariant;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class ProductVariantIntegrationRulesTests
{
    [Test]
    public void LastActiveVariantOfStockProductCannotBeDeactivated()
    {
        var product = new Product("P-1", "منتج", Guid.NewGuid(), Guid.NewGuid(), isStockItem: true);
        var variant = new ProductVariant(product.Id, "SKU-1", 10m, 20m);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([variant]);
        var products = new FakeGenericRepository<Product, Guid>([product]);
        var units = new FakeGenericRepository<Unit, Guid>();
        var handler = new UpdateProductVariantCommandHandler(variants, products, units, new ProductVariantMapper());
        var request = new UpdateProductVariantRequest(variant.SKU, null, null, null, null, null, 10m, 20m, false);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductVariantCommand(variant.Id, request), CancellationToken.None));
    }

    [Test]
    public async Task OneVariantCanBeDeactivatedWhenAnotherActiveVariantExists()
    {
        var product = new Product("P-2", "منتج", Guid.NewGuid(), Guid.NewGuid(), isStockItem: true);
        var first = new ProductVariant(product.Id, "SKU-2-A", 10m, 20m);
        var second = new ProductVariant(product.Id, "SKU-2-B", 10m, 20m);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([first, second]);
        var products = new FakeGenericRepository<Product, Guid>([product]);
        var units = new FakeGenericRepository<Unit, Guid>();
        var handler = new UpdateProductVariantCommandHandler(variants, products, units, new ProductVariantMapper());
        var request = new UpdateProductVariantRequest(first.SKU, null, null, null, null, null, 10m, 20m, false);

        var result = await handler.Handle(new UpdateProductVariantCommand(first.Id, request), CancellationToken.None);

        Assert.That(result.IsActive, Is.False);
        Assert.That(second.IsActive, Is.True);
    }
}
