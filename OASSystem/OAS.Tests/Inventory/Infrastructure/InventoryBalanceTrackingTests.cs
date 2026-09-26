using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory.Infrastructure;

[TestFixture]
public sealed class InventoryBalanceTrackingTests
{
    private static OasDbContext CreateContext() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=tracking_only;Trusted_Connection=True;")
        .Options);

    [Test]
    public async Task PendingBalance_IsReusedAndRemainsAddedAcrossMultipleMovements()
    {
        // No database connection: the pending balance must be found in the change tracker.
        using var context = CreateContext();
        IInventoryBalanceRepository repository = new InventoryBalanceRepository(context);
        var balance = new InventoryBalance(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(balance);
        var ledger = new FakeGenericRepository<InventoryLedger, Guid>();
        var service = new InventoryPostingService(repository, ledger,
            new FakeInventorySequenceNumberGenerator(), TimeProvider.System);
        var now = DateTimeOffset.UtcNow;

        await service.PostMovementAsync(balance.WarehouseId, balance.ProductVariantId,
            InventoryMovementType.In, 10m, 100m, Guid.NewGuid(), Guid.NewGuid(), now, "tester");
        await service.PostMovementAsync(balance.WarehouseId, balance.ProductVariantId,
            InventoryMovementType.In, 10m, 200m, Guid.NewGuid(), Guid.NewGuid(), now, "tester");

        Assert.That(context.Entry(balance).State, Is.EqualTo(EntityState.Added));
        Assert.That(context.InventoryBalances.Local.Count, Is.EqualTo(1));
        Assert.That(balance.OnHandQuantity, Is.EqualTo(20m));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(150m));
        Assert.That(balance.InventoryValue, Is.EqualTo(3000m));
        Assert.That(ledger.Items.Select(x => x.BalanceAfter), Is.EqualTo(new[] { 10m, 20m }));
    }

    [Test]
    public void ExistingBalance_UpdateStillMarksItModified()
    {
        using var context = CreateContext();
        IInventoryBalanceRepository repository = new InventoryBalanceRepository(context);
        var balance = new InventoryBalance(Guid.NewGuid(), Guid.NewGuid());
        context.Attach(balance);
        balance.ApplyInbound(1m, 10m, DateTimeOffset.UtcNow);
        repository.Update(balance);
        Assert.That(context.Entry(balance).State, Is.EqualTo(EntityState.Modified));
    }
}
