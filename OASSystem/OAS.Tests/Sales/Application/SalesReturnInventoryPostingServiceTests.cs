using NUnit.Framework;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Sales.Returns.Services;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesReturnInventoryPostingServiceTests
{
    [Test]
    public async Task MultiWarehouseReturn_CreatesOnePostedTransactionPerWarehouse_AndPreservesSnapshotCost()
    {
        var warehouseA = Guid.NewGuid();
        var warehouseB = Guid.NewGuid();
        var salesReturn = CreateReturn(
            CreateInventoryLine(1, warehouseA, quantity: 1m, unitCost: 40m),
            CreateInventoryLine(2, warehouseB, quantity: 2m, unitCost: 55m));

        var transactions = new TransactionRepository();
        var transactionLines = new TransactionLineRepository();
        var posting = new RecordingInventoryPostingService();
        var service = new SalesReturnInventoryPostingService(
            transactions,
            transactionLines,
            posting,
            new SequenceGenerator());

        var result = await service.PostAsync(
            salesReturn,
            "tester",
            new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(result.InventoryTransactionIds, Has.Count.EqualTo(2));
            Assert.That(transactions.Items, Has.Count.EqualTo(2));
            Assert.That(transactions.Items.All(x => x.Status == InventoryTransactionStatus.Posted), Is.True);
            Assert.That(
                transactions.Items.Select(x => x.DestinationWarehouseId).OrderBy(x => x).ToArray(),
                Is.EqualTo(new Guid?[] { warehouseA, warehouseB }.OrderBy(x => x).ToArray()));
            Assert.That(transactionLines.Items, Has.Count.EqualTo(2));
            Assert.That(posting.Movements, Has.Count.EqualTo(2));
            Assert.That(posting.Movements.Single(x => x.WarehouseId == warehouseA).UnitCost, Is.EqualTo(40m));
            Assert.That(posting.Movements.Single(x => x.WarehouseId == warehouseB).UnitCost, Is.EqualTo(55m));
            Assert.That(posting.Movements.Single(x => x.WarehouseId == warehouseB).Quantity, Is.EqualTo(2m));
        });
    }

    [Test]
    public async Task RetryAfterCompleteInventoryPosting_IsIdempotent_AndDoesNotPostMovementsTwice()
    {
        var warehouseA = Guid.NewGuid();
        var warehouseB = Guid.NewGuid();
        var salesReturn = CreateReturn(
            CreateInventoryLine(1, warehouseA, 1m, 40m),
            CreateInventoryLine(2, warehouseB, 1m, 55m));

        var transactions = new TransactionRepository();
        var transactionLines = new TransactionLineRepository();
        var posting = new RecordingInventoryPostingService();
        var service = new SalesReturnInventoryPostingService(
            transactions,
            transactionLines,
            posting,
            new SequenceGenerator());

        var at = new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);
        var first = await service.PostAsync(salesReturn, "tester", at);
        var movementCount = posting.Movements.Count;

        var second = await service.PostAsync(salesReturn, "tester", at.AddMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(second.InventoryTransactionIds.OrderBy(x => x), Is.EqualTo(first.InventoryTransactionIds.OrderBy(x => x)));
            Assert.That(transactions.Items, Has.Count.EqualTo(2));
            Assert.That(posting.Movements, Has.Count.EqualTo(movementCount));
        });
    }

    private static SalesReturn CreateReturn(params SalesReturnLine[] lines)
    {
        var date = new DateOnly(2026, 10, 17);
        var currencyId = Guid.NewGuid();
        var entity = SalesReturn.Create(
            Guid.NewGuid(),
            "SR-SA01-INVENTORY",
            Guid.NewGuid(),
            Guid.NewGuid(),
            date,
            date,
            currencyId,
            "YER",
            2,
            1m,
            date,
            ExchangeRateType.Accounting,
            ExchangeRateSource.System,
            currencyId,
            "YER",
            2,
            "SA-01 inventory posting test");

        foreach (var line in lines)
        {
            var rebound = SalesReturnLine.Create(
                line.Id,
                entity.Id,
                line.LineNumber,
                line.SalesInvoiceLineId,
                line.LineType,
                line.ProductVariantId,
                line.WarehouseId,
                line.ProductCodeSnapshot,
                line.ProductNameSnapshot,
                line.Quantity,
                line.NetAmount,
                line.TaxAmount,
                line.FinalAmount,
                line.BaseNetAmount,
                line.BaseTaxAmount,
                line.BaseFinalAmount,
                line.UnitCostSnapshot,
                line.TotalCostSnapshot,
                line.ReturnCondition,
                line.StockDisposition);
            entity.AddLine(rebound);
        }

        return entity;
    }

    private static SalesReturnLine CreateInventoryLine(int lineNumber, Guid warehouseId, decimal quantity, decimal unitCost)
    {
        var quantityCost = quantity * unitCost;
        return SalesReturnLine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            lineNumber,
            Guid.NewGuid(),
            SalesLineType.Frame,
            Guid.NewGuid(),
            warehouseId,
            $"FR-{lineNumber:000}",
            $"Frame {lineNumber}",
            quantity,
            100m * quantity,
            0m,
            100m * quantity,
            100m * quantity,
            0m,
            100m * quantity,
            unitCost,
            quantityCost,
            SalesReturnCondition.Resellable,
            SalesReturnStockDisposition.Restock);
    }

    private sealed class SequenceGenerator : ISequenceNumberGenerator
    {
        private long _value;

        public Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Interlocked.Increment(ref _value));
    }

    private sealed class RecordingInventoryPostingService : IInventoryPostingService
    {
        public List<Movement> Movements { get; } = [];

        public Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostMovementAsync(
            Guid warehouseId,
            Guid productVariantId,
            InventoryMovementType movementType,
            decimal quantity,
            decimal unitCost,
            Guid transactionId,
            Guid transactionLineId,
            DateTimeOffset movementDate,
            string createdBy,
            CancellationToken cancellationToken = default)
        {
            Movements.Add(new Movement(
                warehouseId,
                productVariantId,
                movementType,
                quantity,
                unitCost,
                transactionId,
                transactionLineId));
            return Task.FromResult<(InventoryBalance Balance, InventoryLedger Ledger)>((null!, null!));
        }

        public Task<decimal> GetAvailableOutboundUnitCostAsync(
            Guid warehouseId,
            Guid productVariantId,
            decimal quantity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<decimal> GetOutboundUnitCostAsync(
            Guid warehouseId,
            Guid productVariantId,
            decimal quantity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostReservedOutboundAsync(
            Guid warehouseId,
            Guid productVariantId,
            decimal quantity,
            Guid transactionId,
            Guid transactionLineId,
            DateTimeOffset movementDate,
            string createdBy,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(InventoryBalance Balance, InventoryLedger Ledger)>((null!, null!));
    }

    private sealed record Movement(
        Guid WarehouseId,
        Guid ProductVariantId,
        InventoryMovementType MovementType,
        decimal Quantity,
        decimal UnitCost,
        Guid TransactionId,
        Guid TransactionLineId);

    private sealed class TransactionRepository : IInventoryTransactionRepository
    {
        public List<InventoryTransaction> Items { get; } = [];

        public Task<InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<InventoryTransaction?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<InventoryTransaction>> ListAsync(
            ISpecification<InventoryTransaction>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransaction>>(Items.ToArray());

        public Task<PagedData<InventoryTransaction>> GetPageAsync(
            ISpecification<InventoryTransaction> specification,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<long> CountAsync(
            ISpecification<InventoryTransaction>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((long)Items.Count);

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(x => x.Id == id));

        public Task AddAsync(InventoryTransaction entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<InventoryTransaction> entities, CancellationToken cancellationToken = default)
        {
            Items.AddRange(entities);
            return Task.CompletedTask;
        }

        public void Update(InventoryTransaction entity)
        {
        }

        public void Delete(InventoryTransaction entity) => Items.Remove(entity);

        public void DeleteRange(IEnumerable<InventoryTransaction> entities)
        {
            foreach (var entity in entities.ToArray())
                Items.Remove(entity);
        }

        public Task<IReadOnlyList<InventoryTransactionLine>> GetLinesAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransactionLine>>([]);
    }

    private sealed class TransactionLineRepository : IRepository<InventoryTransactionLine, Guid>
    {
        public List<InventoryTransactionLine> Items { get; } = [];

        public Task<InventoryTransactionLine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<InventoryTransactionLine?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<InventoryTransactionLine>> ListAsync(
            ISpecification<InventoryTransactionLine>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryTransactionLine>>(Items.ToArray());

        public Task<PagedData<InventoryTransactionLine>> GetPageAsync(
            ISpecification<InventoryTransactionLine> specification,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<long> CountAsync(
            ISpecification<InventoryTransactionLine>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((long)Items.Count);

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(x => x.Id == id));

        public Task AddAsync(InventoryTransactionLine entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<InventoryTransactionLine> entities, CancellationToken cancellationToken = default)
        {
            Items.AddRange(entities);
            return Task.CompletedTask;
        }

        public void Update(InventoryTransactionLine entity)
        {
        }

        public void Delete(InventoryTransactionLine entity) => Items.Remove(entity);

        public void DeleteRange(IEnumerable<InventoryTransactionLine> entities)
        {
            foreach (var entity in entities.ToArray())
                Items.Remove(entity);
        }
    }
}
