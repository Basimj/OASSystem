using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Tests.Inventory.Infrastructure;

// Opt in with OAS_INVENTORY_TEST_SQLSERVER. Each fixture owns a temporary database.
[TestFixture, Category("InventorySqlServer"), NonParallelizable]
public sealed class ProductOpeningSqlServerTests
{
    private string _connection = null!;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_INVENTORY_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_INVENTORY_TEST_SQLSERVER to run SQL Server integration tests.");
            return;
        }
        _connection = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_Inventory_Test_{Guid.NewGuid():N}"
        }.ConnectionString;
        await using var context = Context();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connection).Options);

    private async Task<CreateStockProductRequest> NewRequestAsync(bool withOpening = false)
    {
        await using var context = Context();
        var key = Guid.NewGuid().ToString("N")[..12];
        var type = new ProductType($"T-{key}", "منتج اختبار");
        var category = new ProductCategory($"C-{key}", "تصنيف اختبار", type.Id);
        var warehouse = new Warehouse($"W-{key}", "مخزن اختبار");
        context.AddRange(category, type, warehouse);
        await context.SaveChangesAsync();
        return new CreateStockProductRequest(
            new CreateProductRequest($"P-{key}", "منتج اختبار", category.Id, null, null, true),
            new InitialProductVariantRequest($"V-{key}", null, null, null, null, null, 100m, 150m),
            withOpening ? new OpeningInventoryRequest(warehouse.Id, 10m, 100m) : null);
    }

    private static CreateStockProductCommandHandler Handler(OasDbContext context) => new(
        new EfRepository<Product, Guid>(context), new EfRepository<ProductVariant, Guid>(context),
        new EfRepository<FrameDetails, Guid>(context), new EfRepository<LensDetails, Guid>(context),
        new EfRepository<ProductCategory, Guid>(context), new EfRepository<Brand, Guid>(context),
        new EfRepository<ProductType, Guid>(context), new EfRepository<Unit, Guid>(context));

    private static Task<CreateStockProductResult> CreateAsync(OasDbContext context, CreateStockProductRequest request) =>
        new EfUnitOfWork(context).ExecuteInTransactionAsync(ct =>
            Handler(context).Handle(new CreateStockProductCommand(request), ct));

    [Test]
    public async Task CatalogCreation_DoesNotGenerateBalancesTransactionsOrLedger()
    {
        var request = await NewRequestAsync();
        await using var context = Context();
        var result = await CreateAsync(context, request);
        await using var verify = Context();
        Assert.That(await verify.ProductVariants.AnyAsync(x => x.ProductId == result.ProductId), Is.True);
        Assert.That(await verify.InventoryBalances.AnyAsync(x => x.ProductVariantId == result.VariantId), Is.False);
        Assert.That(await verify.InventoryTransactions.AnyAsync(x => x.ReferenceId == result.ProductId), Is.False);
        Assert.That(await verify.InventoryLedger.AnyAsync(x => x.ProductVariantId == result.VariantId), Is.False);
    }

    [Test]
    public async Task OpeningPayload_IsRejectedWithoutPersistingCatalogOrStock()
    {
        var request = await NewRequestAsync(withOpening: true);
        await using var context = Context();
        Assert.ThrowsAsync<RequestValidationException>(async () => await CreateAsync(context, request));
        await using var verify = Context();
        Assert.That(await verify.Products.AnyAsync(x => x.ProductCode == request.Product.ProductCode), Is.False);
        Assert.That(await verify.ProductVariants.AnyAsync(x => x.SKU == request.Variant!.SKU), Is.False);
        Assert.That(await verify.InventoryBalances.AnyAsync(x => x.WarehouseId == request.OpeningInventory!.WarehouseId), Is.False);
        Assert.That(await verify.InventoryTransactions.AnyAsync(x => x.DestinationWarehouseId == request.OpeningInventory!.WarehouseId), Is.False);
    }

    [Test]
    public async Task DuplicateCatalogCreate_PersistsOneProductAndNoStock()
    {
        var request = await NewRequestAsync();
        async Task<bool> SubmitAsync()
        {
            await using var context = Context();
            try { await CreateAsync(context, request); return true; }
            catch (ConflictException) { return false; }
        }
        var outcomes = await Task.WhenAll(SubmitAsync(), SubmitAsync());
        Assert.That(outcomes.Count(x => x), Is.EqualTo(1));
        await using var verify = Context();
        Assert.That(await verify.Products.CountAsync(x => x.ProductCode == request.Product.ProductCode), Is.EqualTo(1));
        Assert.That(await verify.InventoryBalances.CountAsync(), Is.EqualTo(0));
    }
}
