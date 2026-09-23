using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Application.Inventory.Services;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class CreateStockProductTests
{
    private FakeGenericRepository<Product, Guid> _products = null!;
    private FakeGenericRepository<ProductVariant, Guid> _variants = null!;
    private FakeGenericRepository<FrameDetails, Guid> _frameDetails = null!;
    private FakeGenericRepository<LensDetails, Guid> _lensDetails = null!;
    private FakeGenericRepository<ProductCategory, Guid> _categories = null!;
    private FakeGenericRepository<Brand, Guid> _brands = null!;
    private FakeGenericRepository<ProductType, Guid> _types = null!;
    private FakeGenericRepository<Unit, Guid> _units = null!;
    private FakeGenericRepository<Warehouse, Guid> _warehouses = null!;
    private FakeInventoryTransactionRepository _transactions = null!;
    private FakeGenericRepository<InventoryTransactionLine, Guid> _lines = null!;
    private FakeInventoryBalanceRepository _balances = null!;
    private FakeGenericRepository<InventoryLedger, Guid> _ledger = null!;
    private FakeInventorySequenceNumberGenerator _sequence = null!;
    private InventoryPostingService _posting = null!;
    private ProductCategory _category = null!;
    private ProductType _frameType = null!;
    private ProductType _serviceType = null!;
    private Warehouse _warehouse = null!;

    [SetUp]
    public void SetUp()
    {
        _products = new();
        _variants = new();
        _frameDetails = new();
        _lensDetails = new();
        _categories = new();
        _brands = new();
        _types = new();
        _units = new();
        _warehouses = new();
        _transactions = new([], []);
        _lines = new(_transactions.Lines);
        _balances = new();
        _ledger = new();
        _sequence = new();
        _posting = new InventoryPostingService(_balances, _ledger, _sequence, TimeProvider.System);

        _category = new ProductCategory("CAT", "تصنيف");
        _frameType = new ProductType("FRAME", "إطار", systemKey: ProductTypeSystemKeys.Frame);
        _serviceType = new ProductType("SERVICE", "خدمة", systemKey: ProductTypeSystemKeys.Service);
        _warehouse = new Warehouse("MAIN", "المخزن الرئيسي", isDefault: true);
        _categories.Items.Add(_category);
        _types.Items.AddRange([_frameType, _serviceType]);
        _warehouses.Items.Add(_warehouse);
    }

    [Test]
    public async Task CreateWithOpeningInventory_CreatesAndPostsCompleteFlow()
    {
        var handler = CreateHandler();
        var request = new CreateStockProductRequest(
            new CreateProductRequest("P-001", "إطار تجريبي", null, _category.Id, null, _frameType.Id, null, true),
            new InitialProductVariantRequest("SKU-001", "111", "أسود", "Black", "52", null, 100m, 180m),
            new OpeningInventoryRequest(_warehouse.Id, 10m, 100m),
            new InitialFrameDetailsRequest("RB-001", "Acetate", "Full Rim", null, "Square", 140m, 18m, 52m));

        var result = await handler.Handle(new CreateStockProductCommand(request), CancellationToken.None);

        Assert.That(_products.Items.Count, Is.EqualTo(1));
        Assert.That(_variants.Items.Count, Is.EqualTo(1));
        Assert.That(_frameDetails.Items.Count, Is.EqualTo(1));
        Assert.That(_frameDetails.Items[0].ProductId, Is.EqualTo(_products.Items[0].Id));
        Assert.That(_transactions.Items.Count, Is.EqualTo(1));
        Assert.That(_lines.Items.Count, Is.EqualTo(1));
        Assert.That(_balances.Items.Count, Is.EqualTo(1));
        Assert.That(_ledger.Items.Count, Is.EqualTo(1));
        Assert.That(_transactions.Items[0].TransactionType, Is.EqualTo(InventoryTransactionType.Opening));
        Assert.That(_transactions.Items[0].Status, Is.EqualTo(InventoryTransactionStatus.Posted));
        Assert.That(_balances.Items[0].OnHandQuantity, Is.EqualTo(10m));
        Assert.That(_balances.Items[0].AverageUnitCost, Is.EqualTo(100m));
        Assert.That(_balances.Items[0].InventoryValue, Is.EqualTo(1000m));
        Assert.That(result.OpeningInventoryCreated, Is.True);
    }

    [Test]
    public async Task CreateStockProductWithoutOpening_CreatesProductAndVariantOnly()
    {
        var handler = CreateHandler();
        var request = new CreateStockProductRequest(
            new CreateProductRequest("P-002", "منتج بدون رصيد", null, _category.Id, null, _frameType.Id, null, true),
            new InitialProductVariantRequest("SKU-002", null, null, null, null, null, 10m, 20m),
            null);

        var result = await handler.Handle(new CreateStockProductCommand(request), CancellationToken.None);

        Assert.That(_products.Items.Count, Is.EqualTo(1));
        Assert.That(_variants.Items.Count, Is.EqualTo(1));
        Assert.That(_transactions.Items, Is.Empty);
        Assert.That(_balances.Items, Is.Empty);
        Assert.That(result.OpeningInventoryCreated, Is.False);
    }

    [Test]
    public void ServiceCannotBeStockItem()
    {
        var handler = CreateHandler();
        var request = new CreateStockProductRequest(
            new CreateProductRequest("S-001", "خدمة", null, _category.Id, null, _serviceType.Id, null, true),
            new InitialProductVariantRequest("SERVICE-SKU", null, null, null, null, null, 0m, 0m),
            null);

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new CreateStockProductCommand(request), CancellationToken.None));
    }

    [Test]
    public void InactiveWarehouseRejectsOpeningInventory()
    {
        _warehouse.UpdateDetails(_warehouse.Code, _warehouse.NameAr, _warehouse.NameEn, _warehouse.Description, _warehouse.IsDefault, false);
        var handler = CreateHandler();
        var request = new CreateStockProductRequest(
            new CreateProductRequest("P-003", "منتج", null, _category.Id, null, _frameType.Id, null, true),
            new InitialProductVariantRequest("SKU-003", null, null, null, null, null, 10m, 20m),
            new OpeningInventoryRequest(_warehouse.Id, 1m, 10m));

        Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.Handle(new CreateStockProductCommand(request), CancellationToken.None));
    }

    private CreateStockProductCommandHandler CreateHandler() => new(
        _products,
        _variants,
        _frameDetails,
        _lensDetails,
        _categories,
        _brands,
        _types,
        _units,
        _warehouses,
        _transactions,
        _lines,
        _posting,
        _sequence,
        new FakeCurrentUser("tester"),
        TimeProvider.System);
}
