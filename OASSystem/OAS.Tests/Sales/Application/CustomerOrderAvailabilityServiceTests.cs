using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Sales.Services;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class CustomerOrderAvailabilityServiceTests
{
    [Test]
    public async Task Assess_UsesAvailableAndAlreadyReservedQuantitiesWithoutChangingInventory()
    {
        var order = CreateOrder(2m);
        var line = order.Lines.Single();
        var balance = new InventoryBalance(line.WarehouseId!.Value, line.ProductVariantId!.Value, onHandQuantity: 3m, reservedQuantity: 2m);
        var reservation = StockReservation.Create(
            Guid.NewGuid(), line.ProductVariantId.Value, line.WarehouseId.Value, 1m,
            SalesSourceReferences.Module, SalesSourceReferences.CustomerOrder,
            order.Id, line.Id, DateTimeOffset.UtcNow);

        var service = new CustomerOrderAvailabilityService(
            new BalanceRepository(balance),
            new ReadRepository<StockReservation>([reservation]));

        var result = await service.AssessAsync(order);
        var availability = result.Lines.Single();

        Assert.Multiple(() =>
        {
            Assert.That(availability.RequestedQuantity, Is.EqualTo(2m));
            Assert.That(availability.OnHandQuantity, Is.EqualTo(3m));
            Assert.That(availability.ReservedQuantity, Is.EqualTo(2m));
            Assert.That(availability.AvailableQuantity, Is.EqualTo(1m));
            Assert.That(availability.AlreadyReservedForThisOrder, Is.EqualTo(1m));
            Assert.That(availability.ShortageQuantity, Is.Zero);
            Assert.That(balance.ReservedQuantity, Is.EqualTo(2m), "Availability query must not reserve stock.");
        });
    }

    [Test]
    public async Task Assess_NoInventory_ReturnsFullShortage()
    {
        var order = CreateOrder(2m);
        var service = new CustomerOrderAvailabilityService(
            new BalanceRepository(),
            new ReadRepository<StockReservation>([]));

        var availability = (await service.AssessAsync(order)).Lines.Single();
        Assert.Multiple(() =>
        {
            Assert.That(availability.ShortageQuantity, Is.EqualTo(2m));
            Assert.That(availability.AvailabilityStatus, Is.EqualTo("Unavailable"));
        });
    }

    private static CustomerOrder CreateOrder(decimal quantity)
    {
        var date = new DateOnly(2026, 10, 4);
        var order = CustomerOrder.Create(
            Guid.NewGuid(), "CO-AV-001", Guid.NewGuid(), null, date, date.AddDays(1), Guid.NewGuid(),
            "YER", "ر.ي", 2, 1m, date, ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0);
        order.AddLine(CustomerOrderLine.Create(
            Guid.NewGuid(), order.Id, 1, null, SalesLineType.Lens, Guid.NewGuid(), Guid.NewGuid(),
            "Lens", quantity, 10m, 10m, SalesDiscountType.None, null, null,
            null, null, true, null, TaxCalculationMode.Exclusive, 2));
        return order;
    }

    private sealed class BalanceRepository(params InventoryBalance[] initial) : IInventoryBalanceRepository
    {
        private readonly List<InventoryBalance> _items = [.. initial];
        public Task<InventoryBalance?> GetByWarehouseAndVariantAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.WarehouseId == warehouseId && x.ProductVariantId == productVariantId));
        public Task<IReadOnlyList<InventoryBalance>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBalance>>(_items.Where(x => x.WarehouseId == warehouseId).ToArray());
        public Task<InventoryBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<InventoryBalance?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);
        public Task<IReadOnlyList<InventoryBalance>> ListAsync(ISpecification<InventoryBalance>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InventoryBalance>>(_items);
        public Task<PagedData<InventoryBalance>> GetPageAsync(ISpecification<InventoryBalance> specification, CancellationToken cancellationToken = default) => Task.FromResult(new PagedData<InventoryBalance>(_items, _items.Count));
        public Task<long> CountAsync(ISpecification<InventoryBalance>? specification = null, CancellationToken cancellationToken = default) => Task.FromResult((long)_items.Count);
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_items.Any(x => x.Id == id));
        public Task AddAsync(InventoryBalance entity, CancellationToken cancellationToken = default) { _items.Add(entity); return Task.CompletedTask; }
        public Task AddRangeAsync(IEnumerable<InventoryBalance> entities, CancellationToken cancellationToken = default) { _items.AddRange(entities); return Task.CompletedTask; }
        public void Update(InventoryBalance entity) { }
        public void Delete(InventoryBalance entity) => _items.Remove(entity);
        public void DeleteRange(IEnumerable<InventoryBalance> entities) { foreach (var entity in entities.ToArray()) _items.Remove(entity); }
    }

    private sealed class ReadRepository<TEntity>(List<TEntity> items) : IReadRepository<TEntity, Guid>
        where TEntity : Entity<Guid>
    {
        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<TEntity> query = items;
            if (specification?.Criteria is not null) query = query.Where(specification.Criteria.Compile());
            return Task.FromResult<IReadOnlyList<TEntity>>(query.ToArray());
        }
        public async Task<PagedData<TEntity>> GetPageAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        {
            var list = await ListAsync(specification, cancellationToken);
            return new PagedData<TEntity>(list, list.Count);
        }
        public async Task<long> CountAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default) => (await ListAsync(specification, cancellationToken)).Count;
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(items.Any(x => x.Id == id));
    }
}
