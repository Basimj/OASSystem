using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Services;
using OAS.Application.Inventory.StockCounts.Commands.ApproveStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CancelStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CompleteStockCount;
using OAS.Application.Inventory.StockCounts.Commands.CreateStockCount;
using OAS.Application.Inventory.StockCounts.Commands.PostStockCount;
using OAS.Application.Inventory.StockCounts.Commands.RecordStockCount;
using OAS.Application.Inventory.StockCounts.Commands.StartStockCount;
using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class StockCountTests
{
    private List<StockCount> _stockCountItems = null!;
    private List<StockCountLine> _lineItems = null!;

    private List<InventoryTransaction> _transactionItems = null!;
    private List<InventoryTransactionLine> _transactionLineItems = null!;

    private FakeStockCountRepository _stockCountRepository = null!;
    private FakeGenericRepository<StockCountLine, Guid> _lineRepository = null!;

    private FakeInventoryTransactionRepository _transactionRepository = null!;
    private FakeGenericRepository<InventoryTransactionLine, Guid> _transactionLineRepository = null!;

    private FakeGenericRepository<Warehouse, Guid> _warehouseRepository = null!;
    private FakeInventoryBalanceRepository _balanceRepository = null!;
    private FakeGenericRepository<InventoryLedger, Guid> _ledgerRepository = null!;

    private FakeInventorySequenceNumberGenerator _sequenceGenerator = null!;
    private FakeCurrentUser _currentUser = null!;
    private InventoryPostingService _postingService = null!;

    private Guid _warehouseId;
    private Guid _variantAId;
    private Guid _variantBId;

    [SetUp]
    public void SetUp()
    {
        _stockCountItems = [];
        _lineItems = [];

        _transactionItems = [];
        _transactionLineItems = [];

        _stockCountRepository = new FakeStockCountRepository(
            _stockCountItems,
            _lineItems);

        _lineRepository =
            new FakeGenericRepository<StockCountLine, Guid>(
                _lineItems);

        _transactionRepository =
            new FakeInventoryTransactionRepository(
                _transactionItems,
                _transactionLineItems);

        _transactionLineRepository =
            new FakeGenericRepository<InventoryTransactionLine, Guid>(
                _transactionLineItems);

        _warehouseRepository =
            new FakeGenericRepository<Warehouse, Guid>();

        _balanceRepository =
            new FakeInventoryBalanceRepository();

        _ledgerRepository =
            new FakeGenericRepository<InventoryLedger, Guid>();

        _sequenceGenerator =
            new FakeInventorySequenceNumberGenerator();

        _currentUser =
            new FakeCurrentUser("test-auditor");

        _postingService =
            new InventoryPostingService(
                _balanceRepository,
                _ledgerRepository,
                _sequenceGenerator,
                TimeProvider.System);

        var warehouse =
            new Warehouse(
                "WH-01",
                "Main Warehouse");

        _warehouseId = warehouse.Id;

        _warehouseRepository.Items.Add(warehouse);

        var variantA =
            new ProductVariant(
                Guid.NewGuid(),
                "VAR-01",
                20m,
                30m,
                variantName: "Variant A");

        _variantAId = variantA.Id;

        var variantB =
            new ProductVariant(
                Guid.NewGuid(),
                "VAR-02",
                50m,
                70m,
                variantName: "Variant B");

        _variantBId = variantB.Id;
    }

    [Test]
    public async Task CreateStockCount_SnapshotsExistingBalances()
    {
        var balA =
            new InventoryBalance(
                _warehouseId,
                _variantAId);

        balA.ApplyInbound(
            10,
            20m,
            DateTimeOffset.UtcNow);

        await _balanceRepository.AddAsync(balA);

        var balB =
            new InventoryBalance(
                _warehouseId,
                _variantBId);

        balB.ApplyInbound(
            5,
            50m,
            DateTimeOffset.UtcNow);

        await _balanceRepository.AddAsync(balB);

        var handler =
            new CreateStockCountCommandHandler(
                _stockCountRepository,
                _lineRepository,
                _balanceRepository,
                _warehouseRepository,
                _sequenceGenerator);

        var request =
            new CreateStockCountRequest(
                "SC-001",
                _warehouseId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Monthly count");

        var countId =
            await handler.Handle(
                new CreateStockCountCommand(request),
                CancellationToken.None);

        var count =
            await _stockCountRepository.GetByIdAsync(countId);

        Assert.That(count, Is.Not.Null);

        Assert.That(
            count!.Status,
            Is.EqualTo(StockCountStatus.Draft));

        Assert.That(
            count.CountNumber,
            Does.StartWith("SC-"));

        var lines =
            await _stockCountRepository.GetLinesAsync(countId);

        Assert.That(
            lines.Count,
            Is.EqualTo(2));

        var lineA =
            lines.First(
                x => x.ProductVariantId == _variantAId);

        Assert.That(
            lineA.SystemQuantity,
            Is.EqualTo(10));

        Assert.That(
            lineA.AverageCostSnapshot,
            Is.EqualTo(20m));

        var lineB =
            lines.First(
                x => x.ProductVariantId == _variantBId);

        Assert.That(
            lineB.SystemQuantity,
            Is.EqualTo(5));

        Assert.That(
            lineB.AverageCostSnapshot,
            Is.EqualTo(50m));
    }

    [Test]
    public async Task FullLifecycle_DraftToCountingToReviewToApprovedToPosted_AdjustsBalances()
    {
        // 1. Setup existing balance
        var balance =
            new InventoryBalance(
                _warehouseId,
                _variantAId);

        balance.ApplyInbound(
            10,
            25m,
            DateTimeOffset.UtcNow);

        await _balanceRepository.AddAsync(balance);

        var count =
            new StockCount(
                "SC-TEST-01",
                _warehouseId,
                DateOnly.FromDateTime(DateTime.UtcNow));

        await _stockCountRepository.AddAsync(count);

        var line =
            new StockCountLine(
                count.Id,
                _variantAId,
                10,
                25m);

        _lineRepository.Items.Add(line);

        // 2. Start Counting
        var startHandler =
            new StartStockCountCommandHandler(
                _stockCountRepository,
                TimeProvider.System);

        await startHandler.Handle(
            new StartStockCountCommand(count.Id),
            CancellationToken.None);

        Assert.That(
            count.Status,
            Is.EqualTo(StockCountStatus.Counting));

        // 3. Record Count: counted 13 instead of 10 (+3 surplus)
        var recordHandler =
            new RecordStockCountCommandHandler(
                _stockCountRepository,
                _lineRepository,
                _currentUser,
                TimeProvider.System);

        var recordRequest =
            new RecordStockCountRequest(
                13,
                DateTimeOffset.UtcNow,
                "auditor");

        await recordHandler.Handle(
            new RecordStockCountCommand(
                count.Id,
                line.Id,
                recordRequest),
            CancellationToken.None);

        Assert.That(
            line.CountedQuantity,
            Is.EqualTo(13));

        Assert.That(
            line.DifferenceQuantity,
            Is.EqualTo(3));

        Assert.That(
            line.VarianceValue,
            Is.EqualTo(75m));

        // 4. Complete -> Review
        var completeHandler =
            new CompleteStockCountCommandHandler(
                _stockCountRepository,
                TimeProvider.System);

        await completeHandler.Handle(
            new CompleteStockCountCommand(count.Id),
            CancellationToken.None);

        Assert.That(
            count.Status,
            Is.EqualTo(StockCountStatus.Review));

        // 5. Approve -> Approved
        var approveHandler =
            new ApproveStockCountCommandHandler(
                _stockCountRepository,
                _currentUser,
                TimeProvider.System);

        await approveHandler.Handle(
            new ApproveStockCountCommand(count.Id),
            CancellationToken.None);

        Assert.That(
            count.Status,
            Is.EqualTo(StockCountStatus.Approved));

        Assert.That(
            count.ApprovedBy,
            Is.EqualTo("test-auditor"));

        // 6. Post -> Posted & inventory balance updated
        var postHandler =
            new PostStockCountCommandHandler(
                _stockCountRepository,
                _transactionRepository,
                _transactionLineRepository,
                _postingService,
                _sequenceGenerator,
                _currentUser,
                TimeProvider.System);

        await postHandler.Handle(
            new PostStockCountCommand(count.Id),
            CancellationToken.None);

        Assert.That(
            count.Status,
            Is.EqualTo(StockCountStatus.Posted));

        Assert.That(
            count.PostedBy,
            Is.EqualTo("test-auditor"));

        // Balance updated to 13
        Assert.That(
            balance.OnHandQuantity,
            Is.EqualTo(13));

        Assert.That(
            balance.AverageUnitCost,
            Is.EqualTo(25m));

        // Transaction generated for the variance.
        Assert.That(
            _transactionItems.Count,
            Is.EqualTo(1));

        Assert.That(
            _transactionItems[0].TransactionType,
            Is.EqualTo(InventoryTransactionType.AdjustmentIncrease));

        Assert.That(
            _transactionItems[0].Status,
            Is.EqualTo(InventoryTransactionStatus.Posted));

        Assert.That(
            _transactionLineItems.Count,
            Is.EqualTo(1));

        // Ledger created with In movement of 3
        Assert.That(
            _ledgerRepository.Items.Count,
            Is.EqualTo(1));

        var ledger =
            _ledgerRepository.Items[0];

        Assert.That(
            ledger.MovementType,
            Is.EqualTo(InventoryMovementType.In));

        Assert.That(
            ledger.QuantityIn,
            Is.EqualTo(3));

        Assert.That(
            ledger.BalanceAfter,
            Is.EqualTo(13));
    }

    [Test]
    public async Task PostStockCount_NegativeDifference_DecreasesBalance()
    {
        var balance =
            new InventoryBalance(
                _warehouseId,
                _variantAId);

        balance.ApplyInbound(
            20,
            15m,
            DateTimeOffset.UtcNow);

        await _balanceRepository.AddAsync(balance);

        var count =
            new StockCount(
                "SC-TEST-02",
                _warehouseId,
                DateOnly.FromDateTime(DateTime.UtcNow));

        count.Start(DateTime.UtcNow);
        count.Complete(DateTime.UtcNow);
        count.Approve(DateTime.UtcNow, "approver");

        await _stockCountRepository.AddAsync(count);

        var line =
            new StockCountLine(
                count.Id,
                _variantAId,
                20,
                15m);

        line.RecordCount(
            16,
            DateTime.UtcNow,
            "counter");

        _lineRepository.Items.Add(line);

        var postHandler =
            new PostStockCountCommandHandler(
                _stockCountRepository,
                _transactionRepository,
                _transactionLineRepository,
                _postingService,
                _sequenceGenerator,
                _currentUser,
                TimeProvider.System);

        await postHandler.Handle(
            new PostStockCountCommand(count.Id),
            CancellationToken.None);

        Assert.That(
            count.Status,
            Is.EqualTo(StockCountStatus.Posted));

        Assert.That(
            balance.OnHandQuantity,
            Is.EqualTo(16));

        Assert.That(
            balance.AverageUnitCost,
            Is.EqualTo(15m));

        // AdjustmentDecrease transaction should be generated.
        Assert.That(
            _transactionItems.Count,
            Is.EqualTo(1));

        Assert.That(
            _transactionItems[0].TransactionType,
            Is.EqualTo(InventoryTransactionType.AdjustmentDecrease));

        Assert.That(
            _transactionItems[0].Status,
            Is.EqualTo(InventoryTransactionStatus.Posted));

        Assert.That(
            _transactionLineItems.Count,
            Is.EqualTo(1));

        Assert.That(
            _ledgerRepository.Items.Count,
            Is.EqualTo(1));

        var ledger =
            _ledgerRepository.Items[0];

        Assert.That(
            ledger.MovementType,
            Is.EqualTo(InventoryMovementType.Out));

        Assert.That(
            ledger.QuantityOut,
            Is.EqualTo(4));

        Assert.That(
            ledger.BalanceAfter,
            Is.EqualTo(16));
    }

    [Test]
    public async Task CancelStockCount_FromCountingStatus_Succeeds()
    {
        var count =
            new StockCount(
                "SC-TEST-03",
                _warehouseId,
                DateOnly.FromDateTime(DateTime.UtcNow));

        count.Start(DateTime.UtcNow);

        await _stockCountRepository.AddAsync(count);

        var cancelHandler =
            new CancelStockCountCommandHandler(
                _stockCountRepository);

        await cancelHandler.Handle(
            new CancelStockCountCommand(count.Id),
            CancellationToken.None);

        Assert.That(
            count.Status,
            Is.EqualTo(StockCountStatus.Cancelled));
    }

    [Test]
    public async Task CancelStockCount_WhenAlreadyPosted_ThrowsConflictException()
    {
        var count =
            new StockCount(
                "SC-TEST-04",
                _warehouseId,
                DateOnly.FromDateTime(DateTime.UtcNow));

        count.Start(DateTime.UtcNow);
        count.Complete(DateTime.UtcNow);
        count.Approve(DateTime.UtcNow, "approver");
        count.Post(DateTime.UtcNow, "poster");

        await _stockCountRepository.AddAsync(count);

        var cancelHandler =
            new CancelStockCountCommandHandler(
                _stockCountRepository);

        var ex =
            Assert.ThrowsAsync<ConflictException>(
                () => cancelHandler.Handle(
                    new CancelStockCountCommand(count.Id),
                    CancellationToken.None));

        Assert.That(
            ex!.Code,
            Is.EqualTo("cannot_cancel_posted_count"));
    }

    [Test]
    public async Task PostStockCount_WhenNotApproved_ThrowsConflictException()
    {
        var count =
            new StockCount(
                "SC-TEST-05",
                _warehouseId,
                DateOnly.FromDateTime(DateTime.UtcNow));

        count.Start(DateTime.UtcNow);
        count.Complete(DateTime.UtcNow);

        // Not approved yet.
        await _stockCountRepository.AddAsync(count);

        var postHandler =
            new PostStockCountCommandHandler(
                _stockCountRepository,
                _transactionRepository,
                _transactionLineRepository,
                _postingService,
                _sequenceGenerator,
                _currentUser,
                TimeProvider.System);

        var ex =
            Assert.ThrowsAsync<ConflictException>(
                () => postHandler.Handle(
                    new PostStockCountCommand(count.Id),
                    CancellationToken.None));

        Assert.That(
            ex!.Code,
            Is.EqualTo("invalid_status_transition"));

        // ÌÃ» √·« Ì‰‘∆ Transaction ⁄‰œ„« ·« ÌﬂÊ‰ «·Ã—œ Approved.
        Assert.That(
            _transactionItems,
            Is.Empty);

        Assert.That(
            _transactionLineItems,
            Is.Empty);
    }
}