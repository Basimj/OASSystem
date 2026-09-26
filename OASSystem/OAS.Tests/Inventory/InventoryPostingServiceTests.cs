using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Services;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class InventoryPostingServiceTests
{
    private FakeInventoryBalanceRepository _balanceRepository = null!;
    private FakeGenericRepository<InventoryLedger, Guid> _ledgerRepository = null!;
    private FakeInventorySequenceNumberGenerator _seqGenerator = null!;
    private InventoryPostingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _balanceRepository = new FakeInventoryBalanceRepository();
        _ledgerRepository = new FakeGenericRepository<InventoryLedger, Guid>();
        _seqGenerator = new FakeInventorySequenceNumberGenerator();
        _service = new InventoryPostingService(
            _balanceRepository,
            _ledgerRepository,
            _seqGenerator,
            TimeProvider.System);
    }

    [Test]
    public async Task PostInbound_CreatesBalanceAndLedgerRecord()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var txnId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var (balance, ledger) = await _service.PostMovementAsync(
            warehouseId,
            variantId,
            InventoryMovementType.In,
            quantity: 10,
            unitCost: 15.00m,
            transactionId: txnId,
            transactionLineId: lineId,
            movementDate: now,
            createdBy: "user1");

        Assert.That(balance.OnHandQuantity, Is.EqualTo(10));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(15.00m));
        Assert.That(balance.InventoryValue, Is.EqualTo(150.00m));

        Assert.That(_ledgerRepository.Items.Count, Is.EqualTo(1));
        Assert.That(ledger.SequenceNumber, Is.EqualTo(1));
        Assert.That(ledger.MovementType, Is.EqualTo(InventoryMovementType.In));
        Assert.That(ledger.QuantityIn, Is.EqualTo(10));
        Assert.That(ledger.QuantityOut, Is.EqualTo(0));
        Assert.That(ledger.BalanceAfter, Is.EqualTo(10));
        Assert.That(ledger.UnitCost, Is.EqualTo(15.00m));
        Assert.That(ledger.AverageCostAfter, Is.EqualTo(15.00m));
    }

    [Test]
    public async Task PostOutbound_ReducesBalanceAndCreatesLedgerRecord()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var txnId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Inbound 20 units @ $10.00
        await _service.PostMovementAsync(warehouseId, variantId, InventoryMovementType.In, 20, 10.00m, txnId, lineId, now, "user1");

        // Outbound 5 units
        var (balance, ledger) = await _service.PostMovementAsync(
            warehouseId,
            variantId,
            InventoryMovementType.Out,
            quantity: 5,
            unitCost: 10.00m,
            transactionId: txnId,
            transactionLineId: Guid.NewGuid(),
            movementDate: now,
            createdBy: "user1");

        Assert.That(balance.OnHandQuantity, Is.EqualTo(15));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(10.00m));
        Assert.That(balance.InventoryValue, Is.EqualTo(150.00m));

        Assert.That(_ledgerRepository.Items.Count, Is.EqualTo(2));
        Assert.That(ledger.MovementType, Is.EqualTo(InventoryMovementType.Out));
        Assert.That(ledger.QuantityIn, Is.EqualTo(0));
        Assert.That(ledger.QuantityOut, Is.EqualTo(5));
        Assert.That(ledger.BalanceAfter, Is.EqualTo(15));
    }

    [Test]
    public void PostOutbound_InsufficientStock_ThrowsConflictException()
    {
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Assert.ThrowsAsync<ConflictException>(async () =>
        {
            await _service.PostMovementAsync(
                warehouseId,
                variantId,
                InventoryMovementType.Out,
                quantity: 5,
                unitCost: 10.00m,
                transactionId: Guid.NewGuid(),
                transactionLineId: Guid.NewGuid(),
                movementDate: now,
                createdBy: "user1");
        });
    }
    [Test]
    public void PostMovement_NegativeUnitCost_IsRejected()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await _service.PostMovementAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                InventoryMovementType.In,
                quantity: 1m,
                unitCost: -1m,
                transactionId: Guid.NewGuid(),
                transactionLineId: Guid.NewGuid(),
                movementDate: DateTimeOffset.UtcNow,
                createdBy: "user1");
        });
    }

    [Test]
    public async Task ZeroCostOpening_OutboundLedgerPreservesZeroCost()
    {
        var warehouse = Guid.NewGuid();
        var variant = Guid.NewGuid();
        var date = DateTimeOffset.UtcNow;
        await _service.PostMovementAsync(warehouse, variant, InventoryMovementType.In,
            10m, 0m, Guid.NewGuid(), Guid.NewGuid(), date, "tester");
        var (balance, ledger) = await _service.PostMovementAsync(warehouse, variant, InventoryMovementType.Out,
            2m, 999m, Guid.NewGuid(), Guid.NewGuid(), date, "tester");
        Assert.That(ledger.UnitCost, Is.Zero);
        Assert.That(balance.InventoryValue, Is.Zero);
        Assert.That(balance.OnHandQuantity, Is.EqualTo(8m));
    }
}
