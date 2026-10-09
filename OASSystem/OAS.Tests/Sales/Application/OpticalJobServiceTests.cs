using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.OpticalJobs.Services;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class OpticalJobServiceTests
{
    [Test]
    public async Task Create_ReadyForProductionOrder_CreatesSingleMaterialsAvailableJob()
    {
        var order = CreateOrder(CustomerOrderStatus.ReadyForProduction);
        var jobs = new FakeGenericRepository<OpticalJob, Guid>();
        var service = CreateService(order, jobs);

        var id = await service.CreateAsync(new CreateOpticalJobRequest(order.Id, null, order.RequiredDate, "lab", []));
        var job = jobs.Items.Single(x => x.Id == id);

        Assert.Multiple(() =>
        {
            Assert.That(job.CustomerOrderId, Is.EqualTo(order.Id));
            Assert.That(job.Status, Is.EqualTo(OpticalJobStatus.MaterialsAvailable));
            Assert.That(job.Lines.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Create_SecondActiveJobForSameOrder_IsRejected()
    {
        var order = CreateOrder(CustomerOrderStatus.ReadyForProduction);
        var jobs = new FakeGenericRepository<OpticalJob, Guid>();
        var service = CreateService(order, jobs);
        var request = new CreateOpticalJobRequest(order.Id, null, order.RequiredDate, null, []);

        await service.CreateAsync(request);
        Assert.ThrowsAsync<ConflictException>(async () => await service.CreateAsync(request));
    }

    [Test]
    public void Create_OrderWithoutCompleteMaterials_IsRejected()
    {
        var order = CreateOrder(CustomerOrderStatus.AwaitingStock);
        var service = CreateService(order, new FakeGenericRepository<OpticalJob, Guid>());

        Assert.ThrowsAsync<ConflictException>(async () => await service.CreateAsync(
            new CreateOpticalJobRequest(order.Id, null, order.RequiredDate, null, [])));
    }

    private static OpticalJobService CreateService(CustomerOrder order, FakeGenericRepository<OpticalJob, Guid> jobs) => new(
        jobs,
        new FakeGenericRepository<OpticalJobLine, Guid>(),
        new FakeGenericRepository<OpticalJobStatusHistory, Guid>(),
        new FakeGenericRepository<OpticalQualityCheck, Guid>(),
        new FakeGenericRepository<OpticalQualityCheckItem, Guid>(),
        new FakeGenericRepository<OpticalJobBreakage, Guid>(),
        new FakeGenericRepository<OpticalJobRemake, Guid>(),
        new ReadRepository<CustomerOrderLineOpticalSnapshot>([]),
        new OrderRepository(order),
        new ReadRepository<SalesInvoice>([]),
        new ReadRepository<Employee>([]),
        new FakeInventorySequenceNumberGenerator(),
        new NoopInventoryPort(),
        new NoopAccountingPort(),
        new NoopPurchasingPort(),
        new NoopSalesPort(),
        new NoopQueryService(),
        new TestCurrentUser(),
        TimeProvider.System);

    private static CustomerOrder CreateOrder(CustomerOrderStatus status)
    {
        var date = new DateOnly(2026, 10, 4);
        var order = CustomerOrder.Create(
            Guid.NewGuid(), "CO-OJ-001", Guid.NewGuid(), null, date, date.AddDays(2), Guid.NewGuid(),
            "YER", "ر.ي", 2, 1m, date, ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0);
        order.AddLine(CustomerOrderLine.Create(
            Guid.NewGuid(), order.Id, 1, null, SalesLineType.Lens, Guid.NewGuid(), Guid.NewGuid(),
            "Production lens", 1m, 10m, 10m, SalesDiscountType.None, null, null,
            null, null, true, null, TaxCalculationMode.Exclusive, 2));
        order.Confirm(status, DateTimeOffset.UtcNow, "tester");
        return order;
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
    private sealed class NoopInventoryPort : IOpticalJobInventoryPort
    {
        public Task<decimal> GetAvailableQuantityAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default) => Task.FromResult(100m);
        public Task<OpticalJobInventoryPostingResult> IssueMaterialsAsync(Guid opticalJobId, Guid requestId, string jobCode, Guid warehouseId, IReadOnlyCollection<OpticalJobMaterialIssueLine> lines, string createdBy, DateTimeOffset atUtc, CancellationToken cancellationToken = default) => Task.FromResult(new OpticalJobInventoryPostingResult(Guid.NewGuid(), 1m));
        public Task<OpticalJobInventoryPostingResult> ScrapAsync(Guid breakageId, string jobCode, Guid warehouseId, Guid productVariantId, decimal quantity, string createdBy, DateTimeOffset atUtc, CancellationToken cancellationToken = default) => Task.FromResult(new OpticalJobInventoryPostingResult(Guid.NewGuid(), quantity));
    }

    private sealed class NoopAccountingPort : IOpticalJobAccountingPort
    {
        public Task<Guid> PostBreakageAsync(OpticalJobBreakage breakage, Guid warehouseId, decimal totalCostBase, Guid postedBy, DateTimeOffset postedAtUtc, CancellationToken cancellationToken = default) => Task.FromResult(Guid.NewGuid());
    }

    private sealed class NoopPurchasingPort : IOpticalJobPurchasingPort
    {
        public Task<Guid?> CreateReplacementDemandAsync(OpticalJob job, OpticalJobLine line, Guid warehouseId, Guid productVariantId, decimal quantity, DateOnly requestDate, string reason, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class NoopSalesPort : IOpticalJobSalesPort
    {
        public Task MarkReadyForDeliveryAsync(OpticalJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureDeliveryEligibleAsync(OpticalJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkDeliveredAsync(OpticalJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoopQueryService : IOpticalJobQueryService
    {
        public Task<OAS.Contracts.Common.Pagination.PagedResult<OpticalJobWorkQueueDto>> GetWorkQueueAsync(OpticalJobWorkQueueRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OAS.Contracts.Common.Pagination.PagedResult<OpticalJobWorkQueueDto>
            {
                Items = [],
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = 0
            });

        public Task<OpticalJobDetailsDto> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId => "00000000-0000-0000-0000-000000000123";
        public bool IsAuthenticated => true;
    }

}
