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
        var product = new Product("P-1", "منتج", Guid.NewGuid(), isStockItem: true);
        var variant = new ProductVariant(product.Id, "SKU-1", 10m, 20m);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([variant]);
        var products = new FakeGenericRepository<Product, Guid>([product]);
        var units = new FakeGenericRepository<Unit, Guid>();
        var handler = new UpdateProductVariantCommandHandler(variants, products, units, new ProductVariantMapper());
        var request = new UpdateProductVariantRequest(variant.SKU, null, null, null, null, null, 10m, 20m, false);

        var exception = Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductVariantCommand(variant.Id, request), CancellationToken.None));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Errors.ContainsKey("is_active"), Is.True);
        Assert.That(exception.Errors["is_active"].Any(message =>
            message.Contains("stock_product_requires_active_variant", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public async Task LastVariantOfNonStockProductCanBeDeactivated()
    {
        var product = new Product("P-3", "منتج غير مخزني", Guid.NewGuid(), isStockItem: false);
        var variant = new ProductVariant(product.Id, "SKU-3", 10m, 20m);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([variant]);
        var products = new FakeGenericRepository<Product, Guid>([product]);
        var units = new FakeGenericRepository<Unit, Guid>();
        var handler = new UpdateProductVariantCommandHandler(variants, products, units, new ProductVariantMapper());
        var request = new UpdateProductVariantRequest(variant.SKU, null, null, null, null, null, 10m, 20m, false);

        var result = await handler.Handle(new UpdateProductVariantCommand(variant.Id, request), CancellationToken.None);

        Assert.That(result.IsActive, Is.False);
    }

    [Test]
    public async Task OneVariantCanBeDeactivatedWhenAnotherActiveVariantExists()
    {
        var product = new Product("P-2", "منتج", Guid.NewGuid(), isStockItem: true);
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
