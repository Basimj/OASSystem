using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Integration;

[TestFixture]
public sealed class FoundationGapWorkflowIntegrationTests
{
    [Test]
    public async Task ReceiptLikeInbound_Reconciliation_ReservesArrivedLensAndMovesOrderToProductionReady()
    {
        var order = CreateOrder(CustomerOrderStatus.AwaitingStock);
        var line = order.Lines.Single();
        var balance = new InventoryBalance(line.WarehouseId!.Value, line.ProductVariantId!.Value);
        balance.IncreaseOnOrder(1m);

        // Equivalent inventory effect of a posted purchase receipt.
        balance.ApplyInbound(1m, 20m, new DateTimeOffset(2026, 10, 5, 10, 0, 0, TimeSpan.Zero));
        balance.DecreaseOnOrder(1m);

        var repository = new OrderRepository(order);
        var stock = new BalanceBackedReservationService(balance);
        var fulfillment = new CustomerOrderFulfillmentService(repository, stock);

        await fulfillment.ReconcileOrdersAsync([order.Id]);
        await fulfillment.ReconcileOrdersAsync([order.Id]); // retry/idempotency

        Assert.Multiple(() =>
        {
            Assert.That(balance.OnHandQuantity, Is.EqualTo(1m));
            Assert.That(balance.ReservedQuantity, Is.EqualTo(1m));
            Assert.That(balance.OnOrderQuantity, Is.Zero);
            Assert.That(stock.ReservationCount, Is.EqualTo(1));
            Assert.That(order.Status, Is.EqualTo(CustomerOrderStatus.ReadyForProduction));
        });
    }

    [Test]
    public async Task PartialReceipt_KeepsOrderPartiallyAvailable()
    {
        var order = CreateOrder(CustomerOrderStatus.AwaitingStock, quantity: 2m);
        var line = order.Lines.Single();
        var balance = new InventoryBalance(line.WarehouseId!.Value, line.ProductVariantId!.Value);
        balance.ApplyInbound(1m, 20m, DateTimeOffset.UtcNow);

        var repository = new OrderRepository(order);
        var stock = new BalanceBackedReservationService(balance);
        var fulfillment = new CustomerOrderFulfillmentService(repository, stock);

        await fulfillment.ReconcileOrdersAsync([order.Id]);

        Assert.Multiple(() =>
        {
            Assert.That(balance.ReservedQuantity, Is.EqualTo(1m));
            Assert.That(order.Status, Is.EqualTo(CustomerOrderStatus.PartiallyAvailable));
        });
    }

    private static CustomerOrder CreateOrder(CustomerOrderStatus status, decimal quantity = 1m)
    {
        var date = new DateOnly(2026, 10, 4);
        var order = CustomerOrder.Create(
            Guid.NewGuid(), "CO-INT-001", Guid.NewGuid(), null, date, date.AddDays(3), Guid.NewGuid(),
            "YER", "ر.ي", 2, 1m, date, ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0);
        order.AddLine(CustomerOrderLine.Create(
            Guid.NewGuid(), order.Id, 1, null, SalesLineType.Lens, Guid.NewGuid(), Guid.NewGuid(),
            "Exact optical lens", quantity, 20m, 20m, SalesDiscountType.None, null, null,
            null, null, true, null, TaxCalculationMode.Exclusive, 2));
        order.Confirm(status, new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero), "integration-test");
        return order;
    }

    private sealed class BalanceBackedReservationService(InventoryBalance balance) : ISalesStockReservationService
    {
        private readonly HashSet<Guid> _reservedLines = [];
        public int ReservationCount => _reservedLines.Count;

        public Task<CustomerOrderStatus> ReserveForOrderAsync(CustomerOrder order, CancellationToken cancellationToken = default)
        {
            foreach (var line in order.Lines.Where(x => x.IsActive && x.RequiresInventory))
            {
                if (_reservedLines.Contains(line.Id)) continue;
                var quantity = Math.Min(line.Quantity, balance.AvailableQuantity);
                if (quantity > 0m)
                {
                    balance.Reserve(quantity);
                    _reservedLines.Add(line.Id);
                }
            }

            var requested = order.Lines.Where(x => x.IsActive && x.RequiresInventory).Sum(x => x.Quantity);
            var reserved = balance.ReservedQuantity;
            var status = reserved <= 0m ? CustomerOrderStatus.AwaitingStock
                : reserved < requested ? CustomerOrderStatus.PartiallyAvailable
                : order.Lines.Any(x => x.IsActive && x.RequiresProduction) ? CustomerOrderStatus.ReadyForProduction
                : CustomerOrderStatus.Confirmed;
            return Task.FromResult(status);
        }

        public Task<bool> HasSufficientStockForInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task EnsureReservationsForInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseOrderReservationsAsync(Guid orderId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseInvoiceReservationsAsync(Guid invoiceId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ValidateInvoiceReservationsAsync(SalesInvoice invoice, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class OrderRepository(CustomerOrder order) : ICustomerOrderAggregateRepository
    {
        public Task<CustomerOrder?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default) => Task.FromResult<CustomerOrder?>(id == order.Id ? order : null);
        public Task<CustomerOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => GetAggregateAsync(id, false, cancellationToken);
        public Task<CustomerOrder?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetAggregateAsync(id, true, cancellationToken);
        public Task<IReadOnlyList<CustomerOrder>> ListAsync(ISpecification<CustomerOrder>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CustomerOrder>>([order]);
        public Task<PagedData<CustomerOrder>> GetPageAsync(ISpecification<CustomerOrder> specification, CancellationToken cancellationToken = default) => Task.FromResult(new PagedData<CustomerOrder>([order], 1));
        public Task<long> CountAsync(ISpecification<CustomerOrder>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult(1L);
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == order.Id);
        public Task AddAsync(CustomerOrder entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<CustomerOrder> entities, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(CustomerOrder entity) { }
        public void Delete(CustomerOrder entity) { }
        public void DeleteRange(IEnumerable<CustomerOrder> entities) { }
    }
}
