using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Inventory.Services;
using OAS.Application.Purchasing.PurchaseReturns.Services;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;
using OAS.Infrastructure.Persistence.Repositories.Inventory;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseReturnInventoryPostingEfTrackingTests
{
    [Test]
    public async Task PostAsync_KeepsNewInventoryTransactionAndLinesInAddedState_UntilUnitOfWorkSaves()
    {
        // Only test EF's change tracker: no SQL Server connection or SaveChanges is required.
        // Use the production entity configurations and repositories to catch the FK regression
        // caused by calling Set.Update on a newly Added inventory transaction.
        var options = new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=localhost;Database=OAS_Tracking_Test_Not_Used;TrustServerCertificate=True;Integrated Security=True")
            .Options;
        await using var context = new OasDbContext(options);
        var transactions = new InventoryTransactionRepository(context);
        var transactionLines = new EfRepository<InventoryTransactionLine, Guid>(context);
        var inventory = new RecordingPostingService();
        var service = new PurchaseReturnInventoryPostingService(
            transactions, transactionLines, inventory, new SequenceGenerator());

        var postingDate = new DateOnly(2026, 11, 8);
        var postedAt = new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var purchaseReturn = PurchaseReturn.Create(
            Guid.NewGuid(), "PRT-FK-REGRESSION", Guid.NewGuid(), null,
            Guid.NewGuid(), Guid.NewGuid(), postingDate, postingDate, "Regression test");
        purchaseReturn.AddLine(PurchaseReturnLine.Create(
            Guid.NewGuid(), purchaseReturn.Id, 1, Guid.NewGuid(), null, Guid.NewGuid(),
            2m, 2m, 50m, 100m, 0m));
        purchaseReturn.AddLine(PurchaseReturnLine.Create(
            Guid.NewGuid(), purchaseReturn.Id, 2, Guid.NewGuid(), null, Guid.NewGuid(),
            1m, 1m, 60m, 60m, 0m));
        purchaseReturn.Confirm(postedAt, "tester");

        var result = await service.PostAsync(purchaseReturn, "tester", postedAt);
        var transaction = context.ChangeTracker.Entries<InventoryTransaction>().Single().Entity;
        var lines = context.ChangeTracker.Entries<InventoryTransactionLine>().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.InventoryTransactionIds, Is.EqualTo(new[] { transaction.Id }));
            Assert.That(context.Entry(transaction).State, Is.EqualTo(EntityState.Added),
                "The new parent must be INSERTed; Update would turn it into an UPDATE and break the FK.");
            Assert.That(transaction.Status, Is.EqualTo(InventoryTransactionStatus.Posted));
            Assert.That(lines, Has.Length.EqualTo(2));
            Assert.That(lines.All(x => x.State == EntityState.Added), Is.True);
            Assert.That(lines.All(x => x.Entity.TransactionId == transaction.Id), Is.True);
            Assert.That(inventory.PostedMovements, Is.EqualTo(2));
            Assert.That(purchaseReturn.InventoryCostBaseAmount, Is.EqualTo(90m));
        });
    }

    private sealed class SequenceGenerator : ISequenceNumberGenerator
    {
        public Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default) =>
            Task.FromResult(1L);
    }

    private sealed class RecordingPostingService : IInventoryPostingService
    {
        public int PostedMovements { get; private set; }

        public Task<decimal> GetAvailableOutboundUnitCostAsync(
            Guid warehouseId, Guid productVariantId, decimal quantity,
            CancellationToken cancellationToken = default) => Task.FromResult(30m);

        public Task<decimal> GetOutboundUnitCostAsync(
            Guid warehouseId, Guid productVariantId, decimal quantity,
            CancellationToken cancellationToken = default) => Task.FromResult(30m);

        public Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostMovementAsync(
            Guid warehouseId, Guid productVariantId, InventoryMovementType movementType,
            decimal quantity, decimal unitCost, Guid transactionId, Guid transactionLineId,
            DateTimeOffset movementDate, string createdBy, CancellationToken cancellationToken = default)
        {
            PostedMovements++;
            return Task.FromResult<(InventoryBalance Balance, InventoryLedger Ledger)>((null!, null!));
        }

        public Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostReservedOutboundAsync(
            Guid warehouseId, Guid productVariantId, decimal quantity,
            Guid transactionId, Guid transactionLineId, DateTimeOffset movementDate,
            string createdBy, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Purchase returns do not use reserved outbound posting.");
    }
}
