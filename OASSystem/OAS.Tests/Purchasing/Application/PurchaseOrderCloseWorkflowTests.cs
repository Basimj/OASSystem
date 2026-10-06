using System.Reflection;
using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.PurchaseOrders.Commands;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class PurchaseOrderCloseWorkflowTests
{
    [Test]
    public async Task Close_PartiallyReceived_ReleasesRemainingOnOrder_ThenCloses()
    {
        var order = CreatePartiallyReceivedOrder();
        SetRowVersion(order, [1, 2, 3]);
        var repository = new FakePurchaseOrderRepository(order, receivedBase: 4m, canClose: true);
        var inventory = new RecordingInventoryPort();
        var handler = new ClosePurchaseOrderCommandHandler(repository, inventory, new TestCurrentUser(), TimeProvider.System);

        await handler.Handle(new ClosePurchaseOrderCommand(order.Id, new ClosePurchaseOrderRequest(Convert.ToBase64String([1, 2, 3]))), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Closed));
            Assert.That(inventory.Decreased, Has.Count.EqualTo(1));
            Assert.That(inventory.Decreased[0].BaseQuantity, Is.EqualTo(6m));
        });
    }

    private static PurchaseOrder CreatePartiallyReceivedOrder()
    {
        var order = PurchaseOrder.Create(Guid.NewGuid(), "PO-TEST-001", Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 10, 5), null, Guid.NewGuid(), 1m, new DateOnly(2026, 10, 5),
            TaxCalculationMode.Exclusive, 0, null);
        order.AddLine(PurchaseOrderLine.Create(Guid.NewGuid(), order.Id, 1, Guid.NewGuid(), null, Guid.NewGuid(), 1m,
            "P-1", "Product", "Each", 10m, 10m, 0m, 0m, TaxCalculationMode.Exclusive, null, null));
        order.Submit(DateTimeOffset.UtcNow, "user");
        order.Approve(DateTimeOffset.UtcNow, "approver");
        order.Send(DateTimeOffset.UtcNow, "sender");
        order.MarkReceived(false);
        return order;
    }

    private static void SetRowVersion(PurchaseOrder order, byte[] value)
    {
        typeof(PurchaseOrder).GetProperty(nameof(PurchaseOrder.RowVersion), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(order, value);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId => "test-user";
        public bool IsAuthenticated => true;
    }

    private sealed class RecordingInventoryPort : IPurchasingInventoryPort
    {
        public List<PurchasingOnOrderLine> Decreased { get; } = [];
        public Task IncreaseOnOrderAsync(Guid warehouseId, IReadOnlyList<PurchasingOnOrderLine> lines, Guid purchaseOrderId, DateOnly effectiveDate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DecreaseOnOrderAsync(Guid warehouseId, IReadOnlyList<PurchasingOnOrderLine> lines, Guid purchaseOrderId, DateOnly effectiveDate, CancellationToken cancellationToken = default)
        { Decreased.AddRange(lines); return Task.CompletedTask; }
        public Task ValidatePostingDateAsync(Guid warehouseId, DateOnly postingDate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PurchasingInventoryPostingResult> PostPurchaseReceiptAsync(PurchasingReceiptInventoryContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakePurchaseOrderRepository(PurchaseOrder order, decimal receivedBase, bool canClose) : IPurchaseOrderRepository
    {
        public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(order);
        public Task<PurchaseOrder?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(order);
        public Task<PurchaseOrder?> GetByLineIdAsync(Guid purchaseOrderLineId, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(order);
        public Task<PagedData<PurchaseOrder>> GetPageAsync(PageRequest request, OAS.Contracts.Purchasing.Enums.PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, CancellationToken cancellationToken = default) => Task.FromResult(new PagedData<PurchaseOrder>([order], 1));
        public Task<IReadOnlyList<PurchaseOrderLineSource>> GetSourcesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PurchaseOrderLineSource>>([]);
        public Task<IReadOnlyDictionary<Guid, decimal>> GetPostedReceivedBaseQuantitiesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal> { [order.Lines.Single().Id] = receivedBase });
        public Task<bool> HasPostedReceiptAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CanCloseAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult(canClose);
        public Task AddAsync(PurchaseOrder entity, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReplaceLinesAndSourcesAsync(PurchaseOrder entity, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(PurchaseOrder entity) { }
    }
}
