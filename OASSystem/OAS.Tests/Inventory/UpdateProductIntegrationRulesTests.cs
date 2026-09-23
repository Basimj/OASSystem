using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Products.Products;
using OAS.Application.Inventory.Products.Products.Commands.UpdateProduct;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class UpdateProductIntegrationRulesTests
{
    [Test]
    public void CannotTurnProductIntoStockItemWithoutActiveVariant()
    {
        var category = new ProductCategory("CAT", "تصنيف");
        var type = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        var product = new Product("P-1", "منتج", category.Id, type.Id, isStockItem: false);

        var products = new FakeGenericRepository<Product, Guid>([product]);
        var variants = new FakeGenericRepository<ProductVariant, Guid>();
        var categories = new FakeGenericRepository<ProductCategory, Guid>([category]);
        var brands = new FakeGenericRepository<Brand, Guid>();
        var types = new FakeGenericRepository<ProductType, Guid>([type]);
        var handler = new UpdateProductCommandHandler(
            products,
            variants,
            new FakeGenericRepository<InventoryBalance, Guid>(),
            new FakeGenericRepository<InventoryLedger, Guid>(),
            categories,
            brands,
            types,
            new ProductMapper());

        var request = new UpdateProductRequest(product.ProductCode, product.NameAr, null, category.Id, null, type.Id, null, true, true);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None));
    }

    [Test]
    public async Task StockProductWithActiveVariant_CanBeUpdated()
    {
        var category = new ProductCategory("CAT", "تصنيف");
        var type = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        var product = new Product("P-2", "منتج", category.Id, type.Id, isStockItem: true);
        var variant = new ProductVariant(product.Id, "SKU-2", 10m, 20m);

        var products = new FakeGenericRepository<Product, Guid>([product]);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([variant]);
        var categories = new FakeGenericRepository<ProductCategory, Guid>([category]);
        var brands = new FakeGenericRepository<Brand, Guid>();
        var types = new FakeGenericRepository<ProductType, Guid>([type]);
        var handler = new UpdateProductCommandHandler(
            products,
            variants,
            new FakeGenericRepository<InventoryBalance, Guid>(),
            new FakeGenericRepository<InventoryLedger, Guid>(),
            categories,
            brands,
            types,
            new ProductMapper());

        var request = new UpdateProductRequest(product.ProductCode, "منتج محدث", null, category.Id, null, type.Id, null, true, true);
        var result = await handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None);

        Assert.That(result.IsStockItem, Is.True);
        Assert.That(result.NameAr, Is.EqualTo("منتج محدث"));
    }

    [Test]
    public void ServiceCannotBeChangedToStockItem()
    {
        var category = new ProductCategory("CAT", "تصنيف");
        var serviceType = new ProductType("SERVICE", "خدمة", systemKey: ProductTypeSystemKeys.Service);
        var product = new Product("S-1", "خدمة", category.Id, serviceType.Id, isStockItem: false);

        var products = new FakeGenericRepository<Product, Guid>([product]);
        var variants = new FakeGenericRepository<ProductVariant, Guid>();
        var categories = new FakeGenericRepository<ProductCategory, Guid>([category]);
        var brands = new FakeGenericRepository<Brand, Guid>();
        var types = new FakeGenericRepository<ProductType, Guid>([serviceType]);
        var handler = new UpdateProductCommandHandler(
            products,
            variants,
            new FakeGenericRepository<InventoryBalance, Guid>(),
            new FakeGenericRepository<InventoryLedger, Guid>(),
            categories,
            brands,
            types,
            new ProductMapper());

        var request = new UpdateProductRequest(product.ProductCode, product.NameAr, null, category.Id, null, serviceType.Id, null, true, true);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None));
    }
    [Test]
    public void ProductWithInventoryHistory_CannotBeChangedToService()
    {
        var category = new ProductCategory("CAT", "تصنيف");
        var frameType = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        var serviceType = new ProductType("SERVICE", "خدمة", systemKey: ProductTypeSystemKeys.Service);
        var product = new Product("P-HISTORY", "منتج", category.Id, frameType.Id, isStockItem: true);
        var variant = new ProductVariant(product.Id, "SKU-HISTORY", 10m, 20m);
        var balance = new InventoryBalance(Guid.NewGuid(), variant.Id);

        var products = new FakeGenericRepository<Product, Guid>([product]);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([variant]);
        var balances = new FakeGenericRepository<InventoryBalance, Guid>([balance]);
        var categories = new FakeGenericRepository<ProductCategory, Guid>([category]);
        var types = new FakeGenericRepository<ProductType, Guid>([frameType, serviceType]);
        var handler = new UpdateProductCommandHandler(
            products,
            variants,
            balances,
            new FakeGenericRepository<InventoryLedger, Guid>(),
            categories,
            new FakeGenericRepository<Brand, Guid>(),
            types,
            new ProductMapper());

        var request = new UpdateProductRequest(product.ProductCode, product.NameAr, null, category.Id, null, serviceType.Id, null, false, true);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None));
    }

}
