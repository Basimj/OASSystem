using System.Reflection;
using NUnit.Framework;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.PurchaseRequests.Commands;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Sales.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
public sealed class CreatePurchaseOrderFromRequestTests
{
    [Test]
    public async Task NoSupplierCatalog_CreatesDraftUsingProductDefaultUnitAndZeroPrice()
    {
        var fixture = new Fixture(catalogDefaults: null);

        await fixture.ExecuteAsync();

        var order = fixture.SavedOrder!;
        var line = order.Lines.Single();
        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Draft));
            Assert.That(line.SupplierCatalogItemId, Is.Null);
            Assert.That(line.PurchaseUnitId, Is.EqualTo(fixture.ProductDefaultUnitId));
            Assert.That(line.UnitConversionFactor, Is.EqualTo(1m));
            Assert.That(line.OrderedQuantity, Is.EqualTo(5m));
            Assert.That(line.UnitPrice, Is.EqualTo(0m));
            Assert.That(fixture.Request.Status, Is.EqualTo(PurchaseRequestStatus.Converted));
        });
    }

    [Test]
    public async Task CatalogWithoutCurrentPrice_CreatesDraftUsingSupplierUnitAndZeroPrice()
    {
        var catalogId = Guid.NewGuid();
        var supplierUnitId = Guid.NewGuid();
        var fixture = new Fixture(new PurchasingCatalogDefaults(catalogId, supplierUnitId, 10m, null), supplierUnitId);

        await fixture.ExecuteAsync();

        var line = fixture.SavedOrder!.Lines.Single();
        Assert.Multiple(() =>
        {
            Assert.That(line.SupplierCatalogItemId, Is.EqualTo(catalogId));
            Assert.That(line.PurchaseUnitId, Is.EqualTo(supplierUnitId));
            Assert.That(line.UnitConversionFactor, Is.EqualTo(10m));
            Assert.That(line.OrderedQuantity, Is.EqualTo(0.5m));
            Assert.That(line.BaseQuantity, Is.EqualTo(5m));
            Assert.That(line.UnitPrice, Is.EqualTo(0m));
        });
    }

    private sealed class Fixture
    {
        public Guid SupplierId { get; } = Guid.NewGuid();
        public Guid WarehouseId { get; } = Guid.NewGuid();
        public Guid CurrencyId { get; } = Guid.NewGuid();
        public Guid ProductVariantId { get; } = Guid.NewGuid();
        public Guid ProductDefaultUnitId { get; } = Guid.NewGuid();
        public PurchaseRequest Request { get; }
        public PurchaseOrder? SavedOrder { get; private set; }

        private readonly CreatePurchaseOrderFromRequestCommandHandler _handler;
        private readonly Guid _requestLineId;

        public Fixture(PurchasingCatalogDefaults? catalogDefaults, Guid? extraUnitId = null)
        {
            Request = PurchaseRequest.Create(Guid.NewGuid(), "PRQ-2026-TEST", PurchaseRequestType.Manual, WarehouseId, null,
                new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 10), null, null, "requester");
            _requestLineId = Guid.NewGuid();
            Request.AddLine(PurchaseRequestLine.Create(_requestLineId, Request.Id, 1, ProductVariantId, 5m,
                new DateOnly(2026, 10, 10), null, SupplierId, null));
            Request.Submit(DateTimeOffset.UtcNow, "requester");
            Request.Approve(DateTimeOffset.UtcNow, "approver");
            SetRowVersion(Request, [1, 2, 3]);

            var requestRepository = new RequestRepository(Request);
            var orderRepository = new OrderRepository(order => SavedOrder = order);
            var catalogRepository = new CatalogRepository(catalogDefaults);
            var references = new ReferencePort(SupplierId, WarehouseId, CurrencyId, ProductVariantId, ProductDefaultUnitId, extraUnitId);
            _handler = new CreatePurchaseOrderFromRequestCommandHandler(requestRepository, orderRepository, catalogRepository, references, new CodeService());
        }

        public Task<Guid> ExecuteAsync()
        {
            var request = new CreatePurchaseOrderFromRequestRequest(
                SupplierId,
                WarehouseId,
                new DateOnly(2026, 10, 5),
                new DateOnly(2026, 10, 10),
                CurrencyId,
                1m,
                new DateOnly(2026, 10, 5),
                TaxCalculationMode.Exclusive,
                0,
                null,
                [new PurchaseRequestLineAllocationRequest(_requestLineId, 5m)],
                Convert.ToBase64String([1, 2, 3]));
            return _handler.Handle(new CreatePurchaseOrderFromRequestCommand(Request.Id, request), CancellationToken.None);
        }

        private static void SetRowVersion(PurchaseRequest request, byte[] value) =>
            typeof(PurchaseRequest).GetProperty(nameof(PurchaseRequest.RowVersion), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(request, value);
    }

    private sealed class RequestRepository(PurchaseRequest request) : IPurchaseRequestRepository
    {
        public Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseRequest?>(id == request.Id ? request : null);
        public Task<PurchaseRequest?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);
        public Task<PagedData<PurchaseRequest>> GetPageAsync(PageRequest page, OAS.Contracts.Purchasing.Enums.PurchaseRequestStatus? status, Guid? warehouseId, CancellationToken cancellationToken = default) => Task.FromResult(new PagedData<PurchaseRequest>([request], 1));
        public Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
        public Task<PurchaseRequest?> GetByLineIdAsync(Guid lineId, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseRequest?>(request.Lines.Any(x => x.Id == lineId) ? request : null);
        public Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesExcludingPurchaseOrderAsync(IReadOnlyCollection<Guid> ids, Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
        public Task AddAsync(PurchaseRequest entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReplaceLinesAsync(PurchaseRequest entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(PurchaseRequest entity) { }
    }

    private sealed class OrderRepository(Action<PurchaseOrder> onAdd) : IPurchaseOrderRepository
    {
        public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(null);
        public Task<PurchaseOrder?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(null);
        public Task<PurchaseOrder?> GetByLineIdAsync(Guid purchaseOrderLineId, CancellationToken cancellationToken = default) => Task.FromResult<PurchaseOrder?>(null);
        public Task<PagedData<PurchaseOrder>> GetPageAsync(PageRequest request, OAS.Contracts.Purchasing.Enums.PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, CancellationToken cancellationToken = default) => Task.FromResult(new PagedData<PurchaseOrder>([], 0));
        public Task<IReadOnlyList<PurchaseOrderLineSource>> GetSourcesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PurchaseOrderLineSource>>([]);
        public Task<IReadOnlyDictionary<Guid, decimal>> GetPostedReceivedBaseQuantitiesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
        public Task<bool> HasPostedReceiptAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> CanCloseAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(PurchaseOrder order, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default) { onAdd(order); return Task.CompletedTask; }
        public Task ReplaceLinesAndSourcesAsync(PurchaseOrder order, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(PurchaseOrder order) { }
    }

    private sealed class CatalogRepository(PurchasingCatalogDefaults? defaults) : ISupplierCatalogRepository
    {
        public Task<PurchasingCatalogDefaults?> GetPurchaseDefaultsAsync(Guid supplierId, Guid productVariantId, Guid currencyId, CancellationToken cancellationToken = default) => Task.FromResult(defaults);
        public Task<SupplierCatalogItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<SupplierCatalogItem?>(null);
        public Task<SupplierCatalogItem?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<SupplierCatalogItem?>(null);
        public Task<PagedData<SupplierCatalogItem>> GetPageAsync(PageRequest request, Guid? supplierId, Guid? productVariantId, bool? isActive, CancellationToken cancellationToken = default) => Task.FromResult(new PagedData<SupplierCatalogItem>([], 0));
        public Task<IReadOnlyList<SupplierPriceHistory>> GetPricesAsync(Guid catalogItemId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SupplierPriceHistory>>([]);
        public Task<SupplierPriceHistory?> GetPriceForUpdateAsync(Guid priceId, CancellationToken cancellationToken = default) => Task.FromResult<SupplierPriceHistory?>(null);
        public Task<bool> ExistsAsync(Guid supplierId, Guid productVariantId, Guid purchaseUnitId, Guid? exceptId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasCurrentPriceAsync(Guid catalogItemId, Guid currencyId, Guid? exceptPriceId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(SupplierCatalogItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPriceAsync(SupplierPriceHistory price, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(SupplierCatalogItem item) { }
        public void UpdatePrice(SupplierPriceHistory price) { }
    }

    private sealed class ReferencePort(Guid supplierId, Guid warehouseId, Guid currencyId, Guid productId, Guid defaultUnitId, Guid? extraUnitId) : IPurchasingReferenceDataPort
    {
        public Task<PurchasingSupplierSnapshot?> GetSupplierAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchasingSupplierSnapshot?>(id == supplierId ? new(id, "SUP", "Supplier", true, Guid.NewGuid()) : null);
        public Task<PurchasingWarehouseSnapshot?> GetWarehouseAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchasingWarehouseSnapshot?>(id == warehouseId ? new(id, "WH", "Warehouse", true) : null);
        public Task<PurchasingProductSnapshot?> GetProductVariantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchasingProductSnapshot?>(id == productId ? new(id, "P-1", "Product", true, true, true, defaultUnitId) : null);
        public Task<PurchasingUnitSnapshot?> GetUnitAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchasingUnitSnapshot?>(id == defaultUnitId || id == extraUnitId ? new(id, "U", "Unit", true) : null);
        public Task<PurchasingCurrencySnapshot?> GetCurrencyAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<PurchasingCurrencySnapshot?>(id == currencyId ? new(id, "CUR", true) : null);
    }

    private sealed class CodeService : IPurchasingCodeService
    {
        public Task<string> NextPurchaseRequestCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PRQ-TEST");
        public Task<string> NextPurchaseOrderCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PO-TEST");
        public Task<string> NextPurchaseReceiptCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("GR-TEST");
        public Task<string> NextPurchaseInvoiceCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PI-TEST");
        public Task<string> NextPurchaseReturnCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) => Task.FromResult("PRT-TEST");
    }
}
