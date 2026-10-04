using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class CustomerOrderFulfillmentServiceTests
{
    [Test]
    public async Task Reconcile_AwaitingStockOrder_ReevaluatesAndMovesToReadyForProduction()
    {
        var order = CreateConfirmedOrder(CustomerOrderStatus.AwaitingStock, requiresProduction: true);
        var repository = new OrderRepository(order);
        var stock = new StockReservationService(CustomerOrderStatus.ReadyForProduction);
        var service = new CustomerOrderFulfillmentService(repository, stock);

        await service.ReconcileOrdersAsync([order.Id]);

        Assert.Multiple(() =>
        {
            Assert.That(stock.Calls, Is.EqualTo(1));
            Assert.That(order.Status, Is.EqualTo(CustomerOrderStatus.ReadyForProduction));
            Assert.That(repository.UpdateCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Reconcile_DistinctsOrderIds_AndSkipsClosedStates()
    {
        var order = CreateConfirmedOrder(CustomerOrderStatus.ReadyForProduction, requiresProduction: true);
        order.StartProduction();
        var repository = new OrderRepository(order);
        var stock = new StockReservationService(CustomerOrderStatus.ReadyForProduction);
        var service = new CustomerOrderFulfillmentService(repository, stock);

        await service.ReconcileOrdersAsync([order.Id, order.Id, Guid.Empty]);

        Assert.That(stock.Calls, Is.Zero);
    }

    internal static CustomerOrder CreateConfirmedOrder(CustomerOrderStatus status, bool requiresProduction)
    {
        var date = new DateOnly(2026, 10, 4);
        var currencyId = Guid.NewGuid();
        var order = CustomerOrder.Create(
            Guid.NewGuid(), "CO-TEST-001", Guid.NewGuid(), null, date, date.AddDays(3),
            currencyId, "YER", "ر.ي", 2, 1m, date,
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0);
        order.AddLine(CustomerOrderLine.Create(
            Guid.NewGuid(), order.Id, 1, null, SalesLineType.Lens,
            Guid.NewGuid(), Guid.NewGuid(), "Lens", 1m,
            10m, 10m, SalesDiscountType.None, null, null,
            null, null, requiresProduction, null, TaxCalculationMode.Exclusive, 2));
        order.Confirm(status, new DateTimeOffset(2026, 10, 4, 10, 0, 0, TimeSpan.Zero), "tester");
        return order;
    }

    private sealed class OrderRepository(CustomerOrder order) : ICustomerOrderAggregateRepository
    {
        public int UpdateCalls { get; private set; }
        public Task<CustomerOrder?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default) => Task.FromResult<CustomerOrder?>(id == order.Id ? order : null);
        public Task<CustomerOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => GetAggregateAsync(id, false, cancellationToken);
        public Task<CustomerOrder?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetAggregateAsync(id, true, cancellationToken);
        public Task<IReadOnlyList<CustomerOrder>> ListAsync(ISpecification<CustomerOrder>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CustomerOrder>>([order]);
        public Task<PagedData<CustomerOrder>> GetPageAsync(ISpecification<CustomerOrder> specification, CancellationToken cancellationToken = default) => Task.FromResult(new PagedData<CustomerOrder>([order], 1));
        public Task<long> CountAsync(ISpecification<CustomerOrder>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult(1L);
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == order.Id);
        public Task AddAsync(CustomerOrder entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<CustomerOrder> entities, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(CustomerOrder entity) => UpdateCalls++;
        public void Delete(CustomerOrder entity) { }
        public void DeleteRange(IEnumerable<CustomerOrder> entities) { }
    }

    private sealed class StockReservationService(CustomerOrderStatus result) : ISalesStockReservationService
    {
        public int Calls { get; private set; }
        public Task<CustomerOrderStatus> ReserveForOrderAsync(CustomerOrder order, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult(result); }
        public Task<bool> HasSufficientStockForInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task EnsureReservationsForInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseOrderReservationsAsync(Guid orderId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseInvoiceReservationsAsync(Guid invoiceId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ValidateInvoiceReservationsAsync(SalesInvoice invoice, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
