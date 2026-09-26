using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Application.Inventory.Services;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;
using OAS.Infrastructure.Persistence.Repositories.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory.Infrastructure;

// Opt in with OAS_INVENTORY_TEST_SQLSERVER. Each fixture owns a uniquely named
// temporary database; the supplied connection's database is never modified.
[TestFixture, Category("InventorySqlServer"), NonParallelizable]
public sealed class ProductOpeningSqlServerTests
{
    private string _connection = null!;
    private readonly FakeInventorySequenceNumberGenerator _sequence = new();

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_INVENTORY_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_INVENTORY_TEST_SQLSERVER to run SQL Server atomicity/concurrency tests.");
            return;
        }
        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_Inventory_Test_{Guid.NewGuid():N}"
        };
        _connection = builder.ConnectionString;
        await using var context = Context();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrEmpty(_connection)) return;
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connection).Options);

    private async Task<CreateStockProductRequest> RequestAsync(bool opening = true)
    {
        await using var context = Context();
        var key = Guid.NewGuid().ToString("N")[..12];
        var category = new ProductCategory($"C-{key}", "تصنيف اختبار");
        var type = new ProductType($"T-{key}", "منتج اختبار");
        var warehouse = new Warehouse($"W-{key}", "مخزن اختبار");
        context.AddRange(category, type, warehouse);
        await context.SaveChangesAsync();
        return new CreateStockProductRequest(
            new CreateProductRequest($"P-{key}", "منتج اختبار", null, category.Id, null, type.Id, null, true),
            new InitialProductVariantRequest($"V-{key}", null, null, null, null, null, 100m, 150m),
            opening ? new OpeningInventoryRequest(warehouse.Id, 10m, 100m) : null);
    }

    private InventoryPostingService Posting(OasDbContext context) => new(
        new InventoryBalanceRepository(context), new EfRepository<InventoryLedger, Guid>(context),
        _sequence, TimeProvider.System);

    private CreateStockProductCommandHandler Handler(OasDbContext context, IInventoryPostingService? posting = null) => new(
        new EfRepository<Product, Guid>(context), new EfRepository<ProductVariant, Guid>(context),
        new EfRepository<FrameDetails, Guid>(context), new EfRepository<LensDetails, Guid>(context),
        new EfRepository<ProductCategory, Guid>(context), new EfRepository<Brand, Guid>(context),
        new EfRepository<ProductType, Guid>(context), new EfRepository<Unit, Guid>(context),
        new EfRepository<Warehouse, Guid>(context), new InventoryTransactionRepository(context),
        new EfRepository<InventoryTransactionLine, Guid>(context), posting ?? Posting(context),
        _sequence, new FakeCurrentUser("inventory-test"), TimeProvider.System);

    private Task<CreateStockProductResult> CreateAsync(OasDbContext context, CreateStockProductRequest request,
        IInventoryPostingService? posting = null) => new EfUnitOfWork(context).ExecuteInTransactionAsync(
            ct => Handler(context, posting).Handle(new CreateStockProductCommand(request), ct));

    [Test]
    public async Task Opening_CommitsCompleteGraphAndWeightedAverage()
    {
        var request = await RequestAsync();
        await using var context = Context();
        var result = await CreateAsync(context, request);
        await using var verify = Context();
        var balance = await verify.InventoryBalances.SingleAsync(x => x.ProductVariantId == result.VariantId);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(10m));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(100m));
        Assert.That(balance.InventoryValue, Is.EqualTo(1000m));
        Assert.That(await verify.InventoryTransactions.CountAsync(x => x.ReferenceId == result.ProductId &&
            x.Status == InventoryTransactionStatus.Posted), Is.EqualTo(1));
        Assert.That(await verify.InventoryTransactionLines.CountAsync(x => x.ProductVariantId == result.VariantId), Is.EqualTo(1));
        Assert.That(await verify.InventoryLedger.CountAsync(x => x.ProductVariantId == result.VariantId), Is.EqualTo(1));

        // A second receipt exercises the same posting service used by manual movements.
        await new EfUnitOfWork(context).ExecuteInTransactionAsync(async ct =>
        {
            var transaction = new InventoryTransaction($"R-{Guid.NewGuid():N}"[..30], InventoryTransactionType.Receipt,
                DateTimeOffset.UtcNow, destinationWarehouseId: balance.WarehouseId);
            var line = new InventoryTransactionLine(transaction.Id, balance.ProductVariantId, 10m, 200m);
            context.AddRange(transaction, line);
            await Posting(context).PostMovementAsync(balance.WarehouseId, balance.ProductVariantId,
                InventoryMovementType.In, 10m, 200m, transaction.Id, line.Id, DateTimeOffset.UtcNow, "tester", ct);
            transaction.Post(DateTimeOffset.UtcNow, "tester");
            return true;
        });
        await verify.Entry(balance).ReloadAsync();
        Assert.That(balance.OnHandQuantity, Is.EqualTo(20m));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(150m));
        Assert.That(balance.InventoryValue, Is.EqualTo(3000m));
    }

    [Test]
    public async Task WithoutOpening_DoesNotInsertArtificialZeroBalance()
    {
        var request = await RequestAsync(false);
        await using var context = Context();
        var result = await CreateAsync(context, request);
        await using var verify = Context();
        Assert.That(await verify.ProductVariants.AnyAsync(x => x.ProductId == result.ProductId), Is.True);
        Assert.That(await verify.InventoryBalances.AnyAsync(x => x.ProductVariantId == result.VariantId), Is.False);
        Assert.That(await verify.InventoryTransactions.AnyAsync(x => x.ReferenceId == result.ProductId), Is.False);
    }

    [Test]
    public async Task FailureAfterPosting_RollsBackEvenFlushedChanges()
    {
        var request = await RequestAsync();
        await using var context = Context();
        var failing = new FlushThenFailPosting(Posting(context), context);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await CreateAsync(context, request, failing));
        await using var verify = Context();
        Assert.That(await verify.Products.AnyAsync(x => x.ProductCode == request.Product.ProductCode), Is.False);
        Assert.That(await verify.ProductVariants.AnyAsync(x => x.SKU == request.Variant!.SKU), Is.False);
        Assert.That(await verify.InventoryBalances.AnyAsync(x => x.WarehouseId == request.OpeningInventory!.WarehouseId), Is.False);
        Assert.That(await verify.InventoryTransactions.AnyAsync(x => x.DestinationWarehouseId == request.OpeningInventory!.WarehouseId), Is.False);
        Assert.That(await verify.InventoryLedger.AnyAsync(x => x.WarehouseId == request.OpeningInventory!.WarehouseId), Is.False);
    }

    [Test]
    public async Task DuplicateCreate_RollsBackSecondOpening()
    {
        var request = await RequestAsync();
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
        Assert.That(await verify.InventoryTransactions.CountAsync(x => x.DestinationWarehouseId == request.OpeningInventory!.WarehouseId), Is.EqualTo(1));
        var balance = await verify.InventoryBalances.SingleAsync(x => x.WarehouseId == request.OpeningInventory!.WarehouseId);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(10m));
    }

    [Test]
    public async Task ConcurrentBalanceUpdates_RejectStaleWriter()
    {
        var request = await RequestAsync();
        await using var first = Context();
        var result = await CreateAsync(first, request);
        await using var second = Context();
        var stale = await new InventoryBalanceRepository(second).GetByWarehouseAndVariantAsync(
            request.OpeningInventory!.WarehouseId, result.VariantId!.Value);
        var current = await new InventoryBalanceRepository(first).GetByWarehouseAndVariantAsync(
            request.OpeningInventory.WarehouseId, result.VariantId.Value);
        current!.ApplyInbound(1m, 100m, DateTimeOffset.UtcNow);
        await new EfUnitOfWork(first).SaveChangesAsync();
        stale!.ApplyInbound(2m, 100m, DateTimeOffset.UtcNow);
        Assert.ThrowsAsync<ConcurrencyException>(async () => await new EfUnitOfWork(second).SaveChangesAsync());
        await using var verify = Context();
        var balance = await verify.InventoryBalances.SingleAsync(x => x.ProductVariantId == result.VariantId);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(11m));
    }

    private sealed class FlushThenFailPosting(IInventoryPostingService inner, OasDbContext context) : IInventoryPostingService
    {
        public async Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostMovementAsync(
            Guid warehouseId, Guid productVariantId, InventoryMovementType movementType, decimal quantity,
            decimal unitCost, Guid transactionId, Guid transactionLineId, DateTimeOffset movementDate,
            string createdBy, CancellationToken cancellationToken = default)
        {
            await inner.PostMovementAsync(warehouseId, productVariantId, movementType, quantity, unitCost,
                transactionId, transactionLineId, movementDate, createdBy, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Injected failure after writing opening inventory.");
        }
    }
}
