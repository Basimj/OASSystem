using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Services;
using OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;
using OAS.Application.Inventory.Transactions.Commands.PostInventoryTransaction;
using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Tests.Inventory.Fakes;
using ContractType = OAS.Contracts.Enums.Inventory.InventoryTransactionType;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class InventoryTransactionTests
{
    private List<InventoryTransaction> _transactionItems = null!;
    private List<InventoryTransactionLine> _lineItems = null!;
    private FakeInventoryTransactionRepository _transactionRepository = null!;
    private FakeGenericRepository<InventoryTransactionLine, Guid> _lineRepository = null!;
    private FakeGenericRepository<Warehouse, Guid> _warehouseRepository = null!;
    private FakeGenericRepository<ProductVariant, Guid> _variantRepository = null!;
    private FakeInventoryBalanceRepository _balanceRepository = null!;
    private FakeGenericRepository<InventoryLedger, Guid> _ledgerRepository = null!;
    private FakeInventorySequenceNumberGenerator _sequenceGenerator = null!;
    private FakeCurrentUser _currentUser = null!;
    private InventoryPostingService _postingService = null!;

    private Guid _warehouseAId;
    private Guid _warehouseBId;
    private Guid _variantId;

    [SetUp]
    public void SetUp()
    {
        _transactionItems = [];
        _lineItems = [];
        _transactionRepository = new FakeInventoryTransactionRepository(_transactionItems, _lineItems);
        _lineRepository = new FakeGenericRepository<InventoryTransactionLine, Guid>(_lineItems);
        _warehouseRepository = new FakeGenericRepository<Warehouse, Guid>();
        _variantRepository = new FakeGenericRepository<ProductVariant, Guid>();
        _balanceRepository = new FakeInventoryBalanceRepository();
        _ledgerRepository = new FakeGenericRepository<InventoryLedger, Guid>();
        _sequenceGenerator = new FakeInventorySequenceNumberGenerator();
        _currentUser = new FakeCurrentUser("test-user-123");
        _postingService = new InventoryPostingService(
            _balanceRepository,
            _ledgerRepository,
            _sequenceGenerator,
            TimeProvider.System);

        var warehouseA = new Warehouse("WH-01", "Main Warehouse");
        _warehouseAId = warehouseA.Id;
        _warehouseRepository.Items.Add(warehouseA);

        var warehouseB = new Warehouse("WH-02", "Secondary Warehouse");
        _warehouseBId = warehouseB.Id;
        _warehouseRepository.Items.Add(warehouseB);

        var variant = new ProductVariant(Guid.NewGuid(), "VAR-01", 20m, 50m, variantName: "Variant 01");
        _variantId = variant.Id;
        _variantRepository.Items.Add(variant);
    }

    [Test]
    public async Task CreateTransaction_GeneratesTransactionAndLines()
    {
        var handler = new CreateInventoryTransactionCommandHandler(
            _transactionRepository,
            _lineRepository,
            _warehouseRepository,
            _variantRepository,
            _sequenceGenerator);

        var request = new CreateInventoryTransactionRequest(
            TransactionNumber: "TXN-TEST-01",
            TransactionType: ContractType.Receipt,
            SourceWarehouseId: null,
            DestinationWarehouseId: _warehouseAId,
            TransactionDate: DateTimeOffset.UtcNow,
            ReferenceType: null,
            ReferenceId: null,
            Reason: null,
            Notes: null,
            Lines:
            [
                new CreateInventoryTransactionLineRequest(
                    ProductVariantId: _variantId,
                    Quantity: 20,
                    UnitCost: 25m,
                    Notes: "Initial batch")
            ]);

        var txnId = await handler.Handle(new CreateInventoryTransactionCommand(request), CancellationToken.None);

        var txn = await _transactionRepository.GetByIdAsync(txnId);
        Assert.That(txn, Is.Not.Null);
        Assert.That(txn!.Status, Is.EqualTo(InventoryTransactionStatus.Draft));

        var lines = await _transactionRepository.GetLinesAsync(txnId);
        Assert.That(lines.Count, Is.EqualTo(1));
        Assert.That(lines[0].ProductVariantId, Is.EqualTo(_variantId));
        Assert.That(lines[0].Quantity, Is.EqualTo(20));
        Assert.That(lines[0].UnitCost, Is.EqualTo(25m));
    }

    [Test]
    public async Task PostTransaction_InboundReceipt_IncreasesBalanceAndCreatesLedger()
    {
        var txn = new InventoryTransaction(
            "TXN-001",
            InventoryTransactionType.Receipt,
            DateTimeOffset.UtcNow,
            sourceWarehouseId: null,
            destinationWarehouseId: _warehouseAId);
        await _transactionRepository.AddAsync(txn);

        var line = new InventoryTransactionLine(txn.Id, _variantId, 15, 30m);
        _lineRepository.Items.Add(line);

        var postHandler = new PostInventoryTransactionCommandHandler(
            _transactionRepository,
            _postingService,
            _currentUser,
            TimeProvider.System);

        await postHandler.Handle(new PostInventoryTransactionCommand(txn.Id), CancellationToken.None);

        Assert.That(txn.Status, Is.EqualTo(InventoryTransactionStatus.Posted));
        Assert.That(txn.PostedBy, Is.EqualTo("test-user-123"));
        Assert.That(txn.PostedAtUtc, Is.Not.Null);

        var balance = await _balanceRepository.GetByWarehouseAndVariantAsync(_warehouseAId, _variantId);
        Assert.That(balance, Is.Not.Null);
        Assert.That(balance!.OnHandQuantity, Is.EqualTo(15));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(30m));
        Assert.That(balance.InventoryValue, Is.EqualTo(450m));

        Assert.That(_ledgerRepository.Items.Count, Is.EqualTo(1));
        var ledger = _ledgerRepository.Items[0];
        Assert.That(ledger.MovementType, Is.EqualTo(InventoryMovementType.In));
        Assert.That(ledger.QuantityIn, Is.EqualTo(15));
        Assert.That(ledger.BalanceAfter, Is.EqualTo(15));
    }

    [Test]
    public async Task PostTransaction_OutboundIssue_DecreasesBalanceAndCreatesLedger()
    {
        // Pre-populate balance with 30 units at 40m
        var balance = new InventoryBalance(_warehouseAId, _variantId);
        balance.ApplyInbound(30, 40m, DateTimeOffset.UtcNow);
        await _balanceRepository.AddAsync(balance);

        var txn = new InventoryTransaction(
            "TXN-002",
            InventoryTransactionType.Issue,
            DateTimeOffset.UtcNow,
            sourceWarehouseId: _warehouseAId,
            destinationWarehouseId: null);
        await _transactionRepository.AddAsync(txn);

        var line = new InventoryTransactionLine(txn.Id, _variantId, 10, 40m);
        _lineRepository.Items.Add(line);

        var postHandler = new PostInventoryTransactionCommandHandler(
            _transactionRepository,
            _postingService,
            _currentUser,
            TimeProvider.System);

        await postHandler.Handle(new PostInventoryTransactionCommand(txn.Id), CancellationToken.None);

        Assert.That(txn.Status, Is.EqualTo(InventoryTransactionStatus.Posted));
        Assert.That(balance.OnHandQuantity, Is.EqualTo(20));
        Assert.That(balance.AverageUnitCost, Is.EqualTo(40m));
        Assert.That(balance.InventoryValue, Is.EqualTo(800m));

        Assert.That(_ledgerRepository.Items.Count, Is.EqualTo(1));
        var ledger = _ledgerRepository.Items[0];
        Assert.That(ledger.MovementType, Is.EqualTo(InventoryMovementType.Out));
        Assert.That(ledger.QuantityOut, Is.EqualTo(10));
        Assert.That(ledger.BalanceAfter, Is.EqualTo(20));
    }

    [Test]
    public async Task PostTransaction_Transfer_MovesQuantityBetweenWarehouses()
    {
        // Source warehouse has 50 units @ 20m
        var sourceBal = new InventoryBalance(_warehouseAId, _variantId);
        sourceBal.ApplyInbound(50, 20m, DateTimeOffset.UtcNow);
        await _balanceRepository.AddAsync(sourceBal);

        var txn = new InventoryTransaction(
            "TXN-003",
            InventoryTransactionType.Transfer,
            DateTimeOffset.UtcNow,
            sourceWarehouseId: _warehouseAId,
            destinationWarehouseId: _warehouseBId);
        await _transactionRepository.AddAsync(txn);

        var line = new InventoryTransactionLine(txn.Id, _variantId, 15, 20m);
        _lineRepository.Items.Add(line);

        var postHandler = new PostInventoryTransactionCommandHandler(
            _transactionRepository,
            _postingService,
            _currentUser,
            TimeProvider.System);

        await postHandler.Handle(new PostInventoryTransactionCommand(txn.Id), CancellationToken.None);

        Assert.That(sourceBal.OnHandQuantity, Is.EqualTo(35));
        var destBal = await _balanceRepository.GetByWarehouseAndVariantAsync(_warehouseBId, _variantId);
        Assert.That(destBal, Is.Not.Null);
        Assert.That(destBal!.OnHandQuantity, Is.EqualTo(15));
        Assert.That(destBal.AverageUnitCost, Is.EqualTo(20m));

        // Ledger should have two entries: 1 Out from WH A, 1 In to WH B
        Assert.That(_ledgerRepository.Items.Count, Is.EqualTo(2));
        var outEntry = _ledgerRepository.Items.First(x => x.WarehouseId == _warehouseAId);
        var inEntry = _ledgerRepository.Items.First(x => x.WarehouseId == _warehouseBId);
        Assert.That(outEntry.MovementType, Is.EqualTo(InventoryMovementType.Out));
        Assert.That(inEntry.MovementType, Is.EqualTo(InventoryMovementType.In));
    }

    [Test]
    public async Task PostTransaction_AlreadyPosted_ThrowsConflictException()
    {
        var txn = new InventoryTransaction(
            "TXN-004",
            InventoryTransactionType.Receipt,
            DateTimeOffset.UtcNow,
            sourceWarehouseId: null,
            destinationWarehouseId: _warehouseAId);
        txn.Post(DateTime.UtcNow, "prev-user");
        await _transactionRepository.AddAsync(txn);

        var postHandler = new PostInventoryTransactionCommandHandler(
            _transactionRepository,
            _postingService,
            _currentUser,
            TimeProvider.System);

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            postHandler.Handle(new PostInventoryTransactionCommand(txn.Id), CancellationToken.None));
        Assert.That(ex!.Code, Is.EqualTo("transaction_already_posted"));
    }

    [Test]
    public async Task PostTransaction_InsufficientStock_ThrowsConflictException()
    {
        var balance = new InventoryBalance(_warehouseAId, _variantId);
        balance.ApplyInbound(5, 10m, DateTimeOffset.UtcNow);
        await _balanceRepository.AddAsync(balance);

        var txn = new InventoryTransaction(
            "TXN-005",
            InventoryTransactionType.Issue,
            DateTimeOffset.UtcNow,
            sourceWarehouseId: _warehouseAId,
            destinationWarehouseId: null);
        await _transactionRepository.AddAsync(txn);

        var line = new InventoryTransactionLine(txn.Id, _variantId, 10, 10m);
        _lineRepository.Items.Add(line);

        var postHandler = new PostInventoryTransactionCommandHandler(
            _transactionRepository,
            _postingService,
            _currentUser,
            TimeProvider.System);

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            postHandler.Handle(new PostInventoryTransactionCommand(txn.Id), CancellationToken.None));
        Assert.That(ex!.Code, Is.EqualTo("insufficient_stock"));
    }
}
