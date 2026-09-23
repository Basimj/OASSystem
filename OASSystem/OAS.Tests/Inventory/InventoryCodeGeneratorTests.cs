using NUnit.Framework;
using OAS.Application.Inventory.Services;
using OAS.Contracts.Inventory;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Tests.Inventory.Fakes;
using InventoryUnit = OAS.Domain.Entities.Inventory.Unit;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class InventoryCodeGeneratorTests
{
    [Test]
    public async Task GeneratedCodesUseDifferentPrefixesPerInventoryTable()
    {
        var generator = CreateGenerator();

        Assert.That(await generator.NextAsync(InventoryCodeKinds.Product), Does.StartWith("P-"));
        Assert.That(await generator.NextAsync(InventoryCodeKinds.Brand), Does.StartWith("B-"));
        Assert.That(await generator.NextAsync(InventoryCodeKinds.ProductCategory), Does.StartWith("C-"));
        Assert.That(await generator.NextAsync(InventoryCodeKinds.ProductType), Does.StartWith("T-"));
        Assert.That(await generator.NextAsync(InventoryCodeKinds.Unit), Does.StartWith("U-"));
        Assert.That(await generator.NextAsync(InventoryCodeKinds.Warehouse), Does.StartWith("W-"));
        Assert.That(await generator.NextAsync(InventoryCodeKinds.ProductVariant), Does.StartWith("V-"));
    }

    [Test]
    public async Task GeneratorSkipsAnExistingCode()
    {
        var products = new FakeGenericRepository<Product, Guid>(
            [new Product("P-000001", "منتج موجود", Guid.NewGuid(), Guid.NewGuid())]);
        var generator = CreateGenerator(products: products);

        var code = await generator.NextAsync(InventoryCodeKinds.Product);

        Assert.That(code, Is.EqualTo("P-000002"));
    }

    [Test]
    public void EditingProductTypeCodeDoesNotChangeItsSystemBehaviorKey()
    {
        var type = new ProductType("SERVICE", "خدمة", systemKey: ProductTypeSystemKeys.Service);

        type.UpdateDetails("T-000123", "خدمة", "Service", true);

        Assert.That(type.Code, Is.EqualTo("T-000123"));
        Assert.That(type.SystemKey, Is.EqualTo(ProductTypeSystemKeys.Service));
    }

    private static InventoryCodeGenerator CreateGenerator(
        FakeGenericRepository<Product, Guid>? products = null) =>
        new(
            new FakeInventorySequenceNumberGenerator(),
            products ?? new FakeGenericRepository<Product, Guid>(),
            new FakeGenericRepository<Brand, Guid>(),
            new FakeGenericRepository<ProductCategory, Guid>(),
            new FakeGenericRepository<ProductType, Guid>(),
            new FakeGenericRepository<InventoryUnit, Guid>(),
            new FakeGenericRepository<Warehouse, Guid>(),
            new FakeGenericRepository<ProductVariant, Guid>());
}
