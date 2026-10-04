using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Services;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Common.Entities;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class CustomerDemandProcurementServiceTests
{
    [Test]
    public async Task RetrySameShortage_DoesNotCreateDuplicateDemand()
    {
        var fixture = new Fixture();
        var shortage = fixture.Shortage(2m);

        await fixture.Service.CreateOrUpdateShortageAsync(shortage);
        await fixture.Service.CreateOrUpdateShortageAsync(shortage);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Requests.Count, Is.EqualTo(1));
            Assert.That(fixture.Lines.Count, Is.EqualTo(1));
            Assert.That(fixture.Lines.Single().RequestedQuantity, Is.EqualTo(2m));
        });
    }

    [Test]
    public async Task LargerRetry_OnlyAddsUncoveredDelta()
    {
        var fixture = new Fixture();
        await fixture.Service.CreateOrUpdateShortageAsync(fixture.Shortage(2m));
        await fixture.Service.CreateOrUpdateShortageAsync(fixture.Shortage(5m));

        Assert.That(fixture.Lines.Single().RequestedQuantity, Is.EqualTo(5m));
    }

    [Test]
    public async Task ScheduledOrderAndPreferredSupplier_ArePersistedOnDemandLine()
    {
        var fixture = new Fixture();
        var schedule = new DateTimeOffset(2026, 10, 5, 8, 30, 0, TimeSpan.Zero);
        await fixture.Service.CreateOrUpdateShortageAsync(fixture.Shortage(1m) with
        {
            PreferredSupplierId = fixture.SupplierId,
            ScheduledOrderAtUtc = schedule
        });

        var line = fixture.Lines.Single();
        Assert.Multiple(() =>
        {
            Assert.That(line.PreferredSupplierId, Is.EqualTo(fixture.SupplierId));
            Assert.That(line.ScheduledOrderAtUtc, Is.EqualTo(schedule));
        });
    }

    [Test]
    public async Task ReassignPreferredSupplier_UpdatesOpenUncommittedDemand()
    {
        var fixture = new Fixture();
        await fixture.Service.CreateOrUpdateShortageAsync(fixture.Shortage(1m));
        var schedule = new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);

        await fixture.Service.ReassignPreferredSupplierAsync(
            fixture.CustomerOrderLineId, fixture.ProductVariantId, fixture.SupplierId, schedule);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Lines.Single().PreferredSupplierId, Is.EqualTo(fixture.SupplierId));
            Assert.That(fixture.Lines.Single().ScheduledOrderAtUtc, Is.EqualTo(schedule));
        });
    }

    private sealed class Fixture
    {
        public Guid CustomerOrderId { get; } = Guid.NewGuid();
        public Guid CustomerOrderLineId { get; } = Guid.NewGuid();
        public Guid WarehouseId { get; } = Guid.NewGuid();
        public Guid ProductVariantId { get; } = Guid.NewGuid();
        public Guid SupplierId { get; } = Guid.NewGuid();
        public List<PurchaseRequest> Requests { get; } = [];
        public List<PurchaseRequestLine> Lines { get; } = [];
        public CustomerDemandProcurementService Service { get; }

        public Fixture()
        {
            var lineRepository = new SharedRepository<PurchaseRequestLine>(Lines);
            var headerRepository = new SharedRepository<PurchaseRequest>(Requests);
            var aggregate = new FakePurchaseRequestRepository(Requests, Lines);
            Service = new CustomerDemandProcurementService(
                aggregate,
                headerRepository,
                lineRepository,
                new ReferencePort(WarehouseId, ProductVariantId, SupplierId),
                new CodeService(),
                new FakeCurrentUser("purchasing-test"));
        }

        public CustomerDemandShortage Shortage(decimal quantity) => new(
            CustomerOrderId, CustomerOrderLineId, WarehouseId, ProductVariantId, quantity,
            new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 7));
    }

    private sealed class SharedRepository<TEntity>(List<TEntity> items) : IRepository<TEntity, Guid>
        where TEntity : Entity<Guid>
    {
        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(x => x.Id == id));
        public Task<TEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);
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
        public async Task<long> CountAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default) =>
            (await ListAsync(specification, cancellationToken)).Count;
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(items.Any(x => x.Id == id));
        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) { items.Add(entity); return Task.CompletedTask; }
        public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) { items.AddRange(entities); return Task.CompletedTask; }
        public void Update(TEntity entity) { }
        public void Delete(TEntity entity) => items.Remove(entity);
        public void DeleteRange(IEnumerable<TEntity> entities) { foreach (var entity in entities.ToArray()) items.Remove(entity); }
    }

    private sealed class FakePurchaseRequestRepository(List<PurchaseRequest> requests, List<PurchaseRequestLine> lines) : IPurchaseRequestRepository
    {
        public Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(requests.FirstOrDefault(x => x.Id == id));
        public Task<PurchaseRequest?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);
        public Task<PagedData<PurchaseRequest>> GetPageAsync(PageRequest request, OAS.Contracts.Purchasing.Enums.PurchaseRequestStatus? status, Guid? warehouseId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedData<PurchaseRequest>(requests, requests.Count));
        public Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
        public Task<PurchaseRequest?> GetByLineIdAsync(Guid lineId, CancellationToken cancellationToken = default) =>
            Task.FromResult(requests.FirstOrDefault(x => x.Lines.Any(l => l.Id == lineId)));
        public Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesExcludingPurchaseOrderAsync(IReadOnlyCollection<Guid> ids, Guid purchaseOrderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
        public Task AddAsync(PurchaseRequest request, CancellationToken cancellationToken = default)
        {
            requests.Add(request);
            foreach (var line in request.Lines) if (lines.All(x => x.Id != line.Id)) lines.Add(line);
            return Task.CompletedTask;
        }
        public Task ReplaceLinesAsync(PurchaseRequest request, CancellationToken cancellationToken = default)
        {
            lines.RemoveAll(x => x.PurchaseRequestId == request.Id);
            lines.AddRange(request.Lines);
            return Task.CompletedTask;
        }
        public void Update(PurchaseRequest request) { }
    }

    private sealed class ReferencePort(Guid warehouseId, Guid productVariantId, Guid supplierId) : IPurchasingReferenceDataPort
    {
        public Task<PurchasingSupplierSnapshot?> GetSupplierAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PurchasingSupplierSnapshot?>(id == supplierId ? new(id, "SUP", "Supplier", true, Guid.NewGuid()) : null);
        public Task<PurchasingWarehouseSnapshot?> GetWarehouseAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PurchasingWarehouseSnapshot?>(id == warehouseId ? new(id, "WH", "Warehouse", true) : null);
        public Task<PurchasingProductSnapshot?> GetProductVariantAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PurchasingProductSnapshot?>(id == productVariantId ? new(id, "P", "Lens", true, true, true, null) : null);
        public Task<PurchasingUnitSnapshot?> GetUnitAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchasingUnitSnapshot?>(null);
        public Task<PurchasingCurrencySnapshot?> GetCurrencyAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchasingCurrencySnapshot?>(null);
    }

    private sealed class CodeService : IPurchasingCodeService
    {
        public Task<string> NextPurchaseRequestCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PR-TEST-001");
        public Task<string> NextPurchaseOrderCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PO-TEST-001");
        public Task<string> NextPurchaseReceiptCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("RCV-TEST-001");
        public Task<string> NextPurchaseInvoiceCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PI-TEST-001");
        public Task<string> NextPurchaseReturnCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PRT-TEST-001");
    }
}
