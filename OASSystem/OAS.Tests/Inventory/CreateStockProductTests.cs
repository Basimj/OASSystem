using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class CreateStockProductTests
{
    private FakeGenericRepository<Product, Guid> _products = null!;
    private FakeGenericRepository<ProductVariant, Guid> _variants = null!;
    private FakeGenericRepository<FrameDetails, Guid> _frames = null!;
    private FakeGenericRepository<LensDetails, Guid> _lenses = null!;
    private FakeGenericRepository<ProductCategory, Guid> _categories = null!;
    private FakeGenericRepository<Brand, Guid> _brands = null!;
    private FakeGenericRepository<ProductType, Guid> _types = null!;
    private FakeGenericRepository<Unit, Guid> _units = null!;
    private ProductCategory _category = null!;
    private ProductCategory _serviceCategory = null!;

    [SetUp]
    public void SetUp()
    {
        _products = new();
        _variants = new();
        _frames = new();
        _lenses = new();
        _categories = new();
        _brands = new();
        _types = new();
        _units = new();
        var frameType = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        var serviceType = new ProductType("SERVICE", "خدمة", systemKey: ProductTypeSystemKeys.Service);
        _category = new ProductCategory("CAT-FRAME", "تصنيف الإطارات", frameType.Id);
        _serviceCategory = new ProductCategory("CAT-SERVICE", "تصنيف الخدمات", serviceType.Id);
        _categories.Items.AddRange([_category, _serviceCategory]);
        _types.Items.AddRange([frameType, serviceType]);
    }

    [Test]
    public async Task CreateStockProduct_CreatesCatalogAndVariantWithoutStock()
    {
        var request = new CreateStockProductRequest(
            new CreateProductRequest("P-001", "إطار تجريبي", _category.Id, null, null, true),
            new InitialProductVariantRequest("SKU-001", "111", "أسود", "Black", "52", null, 100m, 180m),
            null,
            new InitialFrameDetailsRequest("RB-001", "Acetate", "Full Rim", null, "Square", 140m, 18m, 52m));

        var result = await Handler().Handle(new CreateStockProductCommand(request), CancellationToken.None);

        Assert.That(_products.Items.Count, Is.EqualTo(1));
        Assert.That(_variants.Items.Count, Is.EqualTo(1));
        Assert.That(_frames.Items.Count, Is.EqualTo(1));
        Assert.That(_frames.Items[0].ProductId, Is.EqualTo(result.ProductId));
        Assert.That(result.VariantId, Is.EqualTo(_variants.Items[0].Id));
        Assert.That(result.OpeningInventoryCreated, Is.False);
        Assert.That(result.InventoryTransactionId, Is.Null);
        Assert.That(result.WarehouseId, Is.Null);
        Assert.That(result.OpeningQuantity, Is.Null);
    }

    [Test]
    public void CreateStockProduct_OpeningIsRejectedBeforeAnyCatalogWrite()
    {
        var request = new CreateStockProductRequest(
            new CreateProductRequest("P-BLOCK", "منتج", _category.Id, null, null, true),
            new InitialProductVariantRequest("SKU-BLOCK", null, null, null, null, null, 10m, 20m),
            new OpeningInventoryRequest(Guid.NewGuid(), 1m, 10m));

        Assert.ThrowsAsync<RequestValidationException>(() =>
            Handler().Handle(new CreateStockProductCommand(request), CancellationToken.None));
        Assert.That(_products.Items, Is.Empty);
        Assert.That(_variants.Items, Is.Empty);
    }

    [Test]
    public void ServiceCannotBeStockItem()
    {
        var request = new CreateStockProductRequest(
            new CreateProductRequest("S-001", "خدمة", _serviceCategory.Id, null, null, true),
            new InitialProductVariantRequest("SERVICE-SKU", null, null, null, null, null, 0m, 0m));
        Assert.ThrowsAsync<RequestValidationException>(() =>
            Handler().Handle(new CreateStockProductCommand(request), CancellationToken.None));
    }

    private CreateStockProductCommandHandler Handler() => new(
        _products, _variants, _frames, _lenses, _categories, _brands, _types, _units);
}
