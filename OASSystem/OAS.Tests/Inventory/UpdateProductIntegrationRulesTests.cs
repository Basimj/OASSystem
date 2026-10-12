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
        var type = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        var category = new ProductCategory("CAT", "تصنيف", type.Id);
        var product = new Product("P-1", "منتج", category.Id, isStockItem: false);

        var handler = CreateHandler(product, category, [type]);
        var request = new UpdateProductRequest(product.ProductCode, product.Name, category.Id, null, null, true, true);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None));
    }

    [Test]
    public async Task StockProductWithActiveVariant_CanBeUpdated()
    {
        var type = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        var category = new ProductCategory("CAT", "تصنيف", type.Id);
        var product = new Product("P-2", "منتج", category.Id, isStockItem: true);
        var variant = new ProductVariant(product.Id, "SKU-2", 10m, 20m);

        var products = new FakeGenericRepository<Product, Guid>([product]);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([variant]);
        var categories = new FakeGenericRepository<ProductCategory, Guid>([category]);
        var types = new FakeGenericRepository<ProductType, Guid>([type]);
        var handler = new UpdateProductCommandHandler(
            products, variants,
            new FakeGenericRepository<InventoryBalance, Guid>(),
            new FakeGenericRepository<InventoryLedger, Guid>(),
            categories, new FakeGenericRepository<Brand, Guid>(), types, new ProductMapper());

        var request = new UpdateProductRequest(product.ProductCode, "منتج محدث", category.Id, null, null, true, true);
        var result = await handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None);

        Assert.That(result.IsStockItem, Is.True);
        Assert.That(result.Name, Is.EqualTo("منتج محدث"));
    }

    [Test]
    public void ServiceCannotBeChangedToStockItem()
    {
        var serviceType = new ProductType("SERVICE", "خدمة", systemKey: ProductTypeSystemKeys.Service);
        var category = new ProductCategory("CAT-SERVICE", "خدمات", serviceType.Id);
        var product = new Product("S-1", "خدمة", category.Id, isStockItem: false);

        var handler = CreateHandler(product, category, [serviceType]);
        var request = new UpdateProductRequest(product.ProductCode, product.Name, category.Id, null, null, true, true);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ProductWithInventoryHistory_CannotBecomeNonStock(bool changeToService)
    {
        var frameType = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        var serviceType = new ProductType("SERVICE", "خدمة", systemKey: ProductTypeSystemKeys.Service);
        var frameCategory = new ProductCategory("CAT-FRAME", "إطارات", frameType.Id);
        var serviceCategory = new ProductCategory("CAT-SERVICE", "خدمات", serviceType.Id);
        var product = new Product("P-HISTORY", "منتج", frameCategory.Id, isStockItem: true);
        var variant = new ProductVariant(product.Id, "SKU-HISTORY", 10m, 20m);
        var balance = new InventoryBalance(Guid.NewGuid(), variant.Id);

        var products = new FakeGenericRepository<Product, Guid>([product]);
        var variants = new FakeGenericRepository<ProductVariant, Guid>([variant]);
        var balances = new FakeGenericRepository<InventoryBalance, Guid>([balance]);
        var categories = new FakeGenericRepository<ProductCategory, Guid>([frameCategory, serviceCategory]);
        var types = new FakeGenericRepository<ProductType, Guid>([frameType, serviceType]);
        var handler = new UpdateProductCommandHandler(
            products, variants, balances,
            new FakeGenericRepository<InventoryLedger, Guid>(),
            categories, new FakeGenericRepository<Brand, Guid>(), types, new ProductMapper());

        var targetCategory = changeToService ? serviceCategory : frameCategory;
        var request = new UpdateProductRequest(product.ProductCode, product.Name, targetCategory.Id, null, null, false, true);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new UpdateProductCommand(product.Id, request), CancellationToken.None));
    }

    private static UpdateProductCommandHandler CreateHandler(Product product, ProductCategory category, IReadOnlyList<ProductType> types) => new(
        new FakeGenericRepository<Product, Guid>([product]),
        new FakeGenericRepository<ProductVariant, Guid>(),
        new FakeGenericRepository<InventoryBalance, Guid>(),
        new FakeGenericRepository<InventoryLedger, Guid>(),
        new FakeGenericRepository<ProductCategory, Guid>([category]),
        new FakeGenericRepository<Brand, Guid>(),
        new FakeGenericRepository<ProductType, Guid>(types.ToList()),
        new ProductMapper());
}
