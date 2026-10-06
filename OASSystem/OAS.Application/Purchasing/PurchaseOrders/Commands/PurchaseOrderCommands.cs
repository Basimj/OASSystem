using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Domain.Purchasing.Entities;
using DomainOrderStatus = OAS.Domain.Purchasing.Enums.PurchaseOrderStatus;
using DomainRequestStatus = OAS.Domain.Purchasing.Enums.PurchaseRequestStatus;

namespace OAS.Application.Purchasing.PurchaseOrders.Commands;

public sealed record CreatePurchaseOrderCommand(CreatePurchaseOrderRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Create]; }
public sealed record UpdatePurchaseOrderCommand(Guid Id, UpdatePurchaseOrderRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Edit]; }
public sealed record SubmitPurchaseOrderCommand(Guid Id, SubmitPurchaseOrderRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Edit]; }
public sealed record ApprovePurchaseOrderCommand(Guid Id, ApprovePurchaseOrderRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Approve]; }
public sealed record RejectPurchaseOrderCommand(Guid Id, RejectPurchaseOrderRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Approve]; }
public sealed record SendPurchaseOrderCommand(Guid Id, SendPurchaseOrderRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Send]; }
public sealed record CancelPurchaseOrderCommand(Guid Id, CancelPurchaseOrderRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Cancel]; }
public sealed record ClosePurchaseOrderCommand(Guid Id, ClosePurchaseOrderRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Orders.Close]; }

public sealed class CreatePurchaseOrderCommandHandler(
    IPurchaseOrderRepository orderRepository,
    IPurchaseRequestRepository requestRepository,
    ISupplierCatalogRepository catalogRepository,
    IPurchasingReferenceDataPort references,
    IPurchasingCodeService codes) : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseOrderCommand command, CancellationToken ct)
    {
        var r = command.Request;
        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(r.SupplierId, ct));
        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(r.DestinationWarehouseId, ct));
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(r.CurrencyId, ct));

        var id = Guid.NewGuid();
        var order = PurchaseOrder.Create(id, await codes.NextPurchaseOrderCodeAsync(r.OrderDate, ct), r.SupplierId,
            r.DestinationWarehouseId, r.OrderDate, r.ExpectedDeliveryDate, r.CurrencyId, r.ExchangeRate, r.ExchangeRateDate, (OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)r.TaxCalculationMode,
            r.PaymentTermDays, r.Notes);
        var sources = new List<PurchaseOrderLineSource>();

        foreach (var line in r.Lines.OrderBy(x => x.LineSequence))
        {
            var product = await references.GetProductVariantAsync(line.ProductVariantId, ct);
            PurchasingApplicationGuard.Product(product);
            var unit = await references.GetUnitAsync(line.PurchaseUnitId, ct);
            PurchasingApplicationGuard.Unit(unit);
            if (line.SupplierCatalogItemId.HasValue)
            {
                var catalog = await catalogRepository.GetByIdAsync(line.SupplierCatalogItemId.Value, ct)
                    ?? throw new NotFoundException("SupplierCatalogItem", line.SupplierCatalogItemId.Value);
                if (catalog.SupplierId != r.SupplierId || catalog.ProductVariantId != line.ProductVariantId || catalog.PurchaseUnitId != line.PurchaseUnitId)
                    throw new ConflictException("purchasing_catalog_line_mismatch", "عنصر كتالوج المورد لا يطابق المورد أو المنتج أو وحدة السطر.");
            }

            var lineId = Guid.NewGuid();
            order.AddLine(PurchaseOrderLine.Create(lineId, id, line.LineSequence, line.ProductVariantId, line.SupplierCatalogItemId,
                line.PurchaseUnitId, line.UnitConversionFactor, product!.ProductCode, product.ProductName, unit!.Name,
                line.OrderedQuantity, line.UnitPrice, line.DiscountAmount, line.TaxRate, order.TaxCalculationMode, line.ExpectedDeliveryDate, line.Notes));

            foreach (var source in line.Sources)
            {
                await ValidateSourceAllocationAsync(requestRepository, source.PurchaseRequestLineId, source.AllocatedQuantity, line.ProductVariantId, null, ct);
                sources.Add(PurchaseOrderLineSource.Create(Guid.NewGuid(), lineId, source.PurchaseRequestLineId, source.AllocatedQuantity));
            }
        }

        ValidateSourceTotals(order, sources);
        await UpdateRequestConversionStatesAsync(requestRepository, sources.Select(x => x.PurchaseRequestLineId), sources, null, ct);
        await orderRepository.AddAsync(order, sources, ct);
        return id;
    }

    internal static async Task ValidateSourceAllocationAsync(IPurchaseRequestRepository repository, Guid lineId, decimal quantity, Guid expectedProductVariantId, Guid? excludingOrderId, CancellationToken ct)
    {
        var request = await repository.GetByLineIdAsync(lineId, ct) ?? throw new NotFoundException("PurchaseRequestLine", lineId);
        if (request.Status is not (DomainRequestStatus.Approved or DomainRequestStatus.PartiallyConverted))
            throw new ConflictException("purchasing_request_line_not_approved", "لا يمكن ربط أمر شراء بسطر طلب غير معتمد.");
        var line = request.Lines.Single(x => x.Id == lineId);
        if (line.ProductVariantId != expectedProductVariantId)
            throw new ConflictException("purchasing_request_source_product_mismatch", "منتج سطر أمر الشراء لا يطابق المنتج في سطر طلب الشراء المصدر.");
        var ids = new[] { lineId };
        var allocated = excludingOrderId.HasValue
            ? await repository.GetAllocatedQuantitiesExcludingPurchaseOrderAsync(ids, excludingOrderId.Value, ct)
            : await repository.GetAllocatedQuantitiesAsync(ids, ct);
        allocated.TryGetValue(lineId, out var current);
        if (quantity <= 0 || current + quantity > line.RequestedQuantity)
            throw new ConflictException("purchasing_request_overallocated", "إجمالي الكمية المخصصة لأوامر الشراء يتجاوز كمية طلب الشراء.");
    }

    internal static void ValidateSourceTotals(PurchaseOrder order, IReadOnlyList<PurchaseOrderLineSource> sources)
    {
        foreach (var line in order.Lines)
        {
            var allocated = sources.Where(x => x.PurchaseOrderLineId == line.Id).Sum(x => x.AllocatedQuantity);
            if (allocated > line.BaseQuantity)
                throw new ConflictException("purchasing_order_source_quantity_exceeds_line", "إجمالي كميات مصادر طلبات الشراء يتجاوز الكمية الأساسية لسطر أمر الشراء.");
        }
    }

    internal static async Task UpdateRequestConversionStatesAsync(
        IPurchaseRequestRepository repository,
        IEnumerable<Guid> affectedLineIds,
        IReadOnlyList<PurchaseOrderLineSource> replacementSources,
        Guid? excludingPurchaseOrderId,
        CancellationToken ct)
    {
        var requests = new Dictionary<Guid, PurchaseRequest>();
        var lineToRequest = new Dictionary<Guid, Guid>();
        foreach (var lineId in affectedLineIds.Distinct())
        {
            var request = await repository.GetByLineIdAsync(lineId, ct);
            if (request is null) continue;
            requests[request.Id] = request;
            foreach (var line in request.Lines) lineToRequest[line.Id] = request.Id;
        }
        foreach (var request in requests.Values)
        {
            var ids = request.Lines.Select(x => x.Id).ToArray();
            var baseline = excludingPurchaseOrderId.HasValue
                ? await repository.GetAllocatedQuantitiesExcludingPurchaseOrderAsync(ids, excludingPurchaseOrderId.Value, ct)
                : await repository.GetAllocatedQuantitiesAsync(ids, ct);
            foreach (var line in request.Lines)
            {
                baseline.TryGetValue(line.Id, out var existing);
                var replacement = replacementSources.Where(x => x.PurchaseRequestLineId == line.Id).Sum(x => x.AllocatedQuantity);
                if (existing + replacement > line.RequestedQuantity)
                    throw new ConflictException("purchasing_request_overallocated", "إجمالي الكمية المخصصة لأوامر الشراء يتجاوز كمية سطر طلب الشراء.");
            }
            var added = replacementSources.Where(x => lineToRequest.TryGetValue(x.PurchaseRequestLineId, out var requestId) && requestId == request.Id)
                .Sum(x => x.AllocatedQuantity);
            request.MarkConversion(baseline.Values.Sum() + added, request.Lines.Sum(x => x.RequestedQuantity));
            repository.Update(request);
        }
    }
}

public sealed class UpdatePurchaseOrderCommandHandler(
    IPurchaseOrderRepository orderRepository,
    IPurchaseRequestRepository requestRepository,
    ISupplierCatalogRepository catalogRepository,
    IPurchasingReferenceDataPort references) : IRequestHandler<UpdatePurchaseOrderCommand>
{
    public async Task Handle(UpdatePurchaseOrderCommand command, CancellationToken ct)
    {
        var order = await orderRepository.GetForUpdateAsync(command.Id, ct) ?? throw new NotFoundException("PurchaseOrder", command.Id);
        PurchasingRowVersion.EnsureMatches(order.RowVersion, command.Request.RowVersion, "Purchase order");
        if (order.Status != DomainOrderStatus.Draft)
            throw new ConflictException("purchasing_order_not_draft", "يمكن تعديل أمر الشراء عندما يكون مسودة فقط.");

        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(command.Request.SupplierId, ct));
        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(command.Request.DestinationWarehouseId, ct));
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(command.Request.CurrencyId, ct));
        order.UpdateHeader(command.Request.SupplierId, command.Request.DestinationWarehouseId, command.Request.OrderDate,
            command.Request.ExpectedDeliveryDate, command.Request.CurrencyId, command.Request.ExchangeRate,
            command.Request.ExchangeRateDate, (OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)command.Request.TaxCalculationMode, command.Request.PaymentTermDays, command.Request.Notes);

        var oldSources = await orderRepository.GetSourcesAsync(order.Id, ct);
        var incomingLineIds = command.Request.Lines.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        foreach (var old in order.Lines.Where(x => !incomingLineIds.Contains(x.Id)).ToArray()) order.RemoveLine(old.Id);
        var newSources = new List<PurchaseOrderLineSource>();

        foreach (var line in command.Request.Lines.OrderBy(x => x.LineSequence))
        {
            var product = await references.GetProductVariantAsync(line.ProductVariantId, ct);
            PurchasingApplicationGuard.Product(product);
            var unit = await references.GetUnitAsync(line.PurchaseUnitId, ct);
            PurchasingApplicationGuard.Unit(unit);
            if (line.SupplierCatalogItemId.HasValue)
            {
                var catalog = await catalogRepository.GetByIdAsync(line.SupplierCatalogItemId.Value, ct)
                    ?? throw new NotFoundException("SupplierCatalogItem", line.SupplierCatalogItemId.Value);
                if (catalog.SupplierId != command.Request.SupplierId || catalog.ProductVariantId != line.ProductVariantId || catalog.PurchaseUnitId != line.PurchaseUnitId)
                    throw new ConflictException("purchasing_catalog_line_mismatch", "عنصر كتالوج المورد لا يطابق المورد أو المنتج أو وحدة السطر.");
            }

            PurchaseOrderLine target;
            if (line.Id.HasValue)
            {
                var existing = order.Lines.SingleOrDefault(x => x.Id == line.Id.Value)
                    ?? throw new ConflictException("purchasing_order_line_missing", "أحد بنود أمر الشراء لم يعد موجودًا. أعد تحميل البيانات.");
                if (string.IsNullOrWhiteSpace(line.RowVersion)) throw new ConcurrencyException("Purchase order line row version is required.");
                PurchasingRowVersion.EnsureMatches(existing.RowVersion, line.RowVersion, "Purchase order line");
                var structuralChange = existing.ProductVariantId != line.ProductVariantId || existing.PurchaseUnitId != line.PurchaseUnitId ||
                    existing.SupplierCatalogItemId != line.SupplierCatalogItemId || existing.UnitConversionFactor != line.UnitConversionFactor || existing.LineSequence != line.LineSequence;
                if (structuralChange)
                {
                    order.RemoveLine(existing.Id);
                    target = PurchaseOrderLine.Create(existing.Id, order.Id, line.LineSequence, line.ProductVariantId, line.SupplierCatalogItemId,
                        line.PurchaseUnitId, line.UnitConversionFactor, product!.ProductCode, product.ProductName, unit!.Name,
                        line.OrderedQuantity, line.UnitPrice, line.DiscountAmount, line.TaxRate, order.TaxCalculationMode, line.ExpectedDeliveryDate, line.Notes);
                    order.AddLine(target);
                }
                else
                {
                    existing.UpdateCommercials(line.OrderedQuantity, line.UnitPrice, line.DiscountAmount, line.TaxRate, order.TaxCalculationMode, line.ExpectedDeliveryDate, line.Notes);
                    target = existing;
                }
            }
            else
            {
                target = PurchaseOrderLine.Create(Guid.NewGuid(), order.Id, line.LineSequence, line.ProductVariantId, line.SupplierCatalogItemId,
                    line.PurchaseUnitId, line.UnitConversionFactor, product!.ProductCode, product.ProductName, unit!.Name,
                    line.OrderedQuantity, line.UnitPrice, line.DiscountAmount, line.TaxRate, order.TaxCalculationMode, line.ExpectedDeliveryDate, line.Notes);
                order.AddLine(target);
            }

            foreach (var source in line.Sources)
            {
                await CreatePurchaseOrderCommandHandler.ValidateSourceAllocationAsync(requestRepository, source.PurchaseRequestLineId,
                    source.AllocatedQuantity, line.ProductVariantId, order.Id, ct);
                var sourceId = source.Id ?? Guid.NewGuid();
                if (source.Id.HasValue)
                {
                    var oldSource = oldSources.SingleOrDefault(x => x.Id == source.Id.Value)
                        ?? throw new ConflictException("purchasing_order_source_missing", "أحد مصادر سطر أمر الشراء لم يعد موجودًا.");
                    if (string.IsNullOrWhiteSpace(source.RowVersion)) throw new ConcurrencyException("Purchase order line source row version is required.");
                    PurchasingRowVersion.EnsureMatches(oldSource.RowVersion, source.RowVersion, "Purchase order line source");
                }
                newSources.Add(PurchaseOrderLineSource.Create(sourceId, target.Id, source.PurchaseRequestLineId, source.AllocatedQuantity));
            }
        }

        order.RecalculateTotals();
        CreatePurchaseOrderCommandHandler.ValidateSourceTotals(order, newSources);
        await CreatePurchaseOrderCommandHandler.UpdateRequestConversionStatesAsync(requestRepository,
            oldSources.Select(x => x.PurchaseRequestLineId).Concat(newSources.Select(x => x.PurchaseRequestLineId)), newSources, order.Id, ct);
        await orderRepository.ReplaceLinesAndSourcesAsync(order, newSources, ct);
        orderRepository.Update(order);
    }
}

public abstract class PurchaseOrderStatusHandlerBase(IPurchaseOrderRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    protected async Task<PurchaseOrder> LoadAsync(Guid id, string rowVersion, CancellationToken ct)
    {
        var order = await repository.GetForUpdateAsync(id, ct) ?? throw new NotFoundException("PurchaseOrder", id);
        PurchasingRowVersion.EnsureMatches(order.RowVersion, rowVersion, "Purchase order");
        return order;
    }
    protected DateTimeOffset Now => timeProvider.GetUtcNow();
    protected string? UserId => currentUser.UserId;
    protected void Save(PurchaseOrder order) => repository.Update(order);
}

public sealed class SubmitPurchaseOrderCommandHandler(IPurchaseOrderRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : PurchaseOrderStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<SubmitPurchaseOrderCommand>
{ public async Task Handle(SubmitPurchaseOrderCommand c, CancellationToken ct) { var e = await LoadAsync(c.Id, c.Request.RowVersion, ct); e.Submit(Now, UserId); Save(e); } }
public sealed class ApprovePurchaseOrderCommandHandler(IPurchaseOrderRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : PurchaseOrderStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<ApprovePurchaseOrderCommand>
{ public async Task Handle(ApprovePurchaseOrderCommand c, CancellationToken ct) { var e = await LoadAsync(c.Id, c.Request.RowVersion, ct); e.Approve(Now, UserId); Save(e); } }
public sealed class RejectPurchaseOrderCommandHandler(IPurchaseOrderRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : PurchaseOrderStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<RejectPurchaseOrderCommand>
{ public async Task Handle(RejectPurchaseOrderCommand c, CancellationToken ct) { var e = await LoadAsync(c.Id, c.Request.RowVersion, ct); e.Reject(Now, UserId, c.Request.Reason); Save(e); } }

public sealed class SendPurchaseOrderCommandHandler(
    IPurchaseOrderRepository repository,
    IPurchasingInventoryPort inventory,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : PurchaseOrderStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<SendPurchaseOrderCommand>
{
    public async Task Handle(SendPurchaseOrderCommand c, CancellationToken ct)
    {
        var order = await LoadAsync(c.Id, c.Request.RowVersion, ct);
        if (order.Status != DomainOrderStatus.Approved)
            throw new ConflictException("purchasing_order_not_approved", "يجب اعتماد أمر الشراء قبل إرساله للمورد.");
        var received = await repository.GetPostedReceivedBaseQuantitiesAsync(order.Id, ct);
        var lines = order.Lines.Select(x =>
        {
            received.TryGetValue(x.Id, out var r);
            return new PurchasingOnOrderLine(x.ProductVariantId, Math.Max(0m, x.BaseQuantity - r));
        }).Where(x => x.BaseQuantity > 0).ToArray();
        await inventory.IncreaseOnOrderAsync(order.DestinationWarehouseId, lines, order.Id, order.OrderDate, ct);
        order.Send(Now, UserId);
        Save(order);
    }
}

public sealed class CancelPurchaseOrderCommandHandler(
    IPurchaseOrderRepository repository,
    IPurchasingInventoryPort inventory,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : PurchaseOrderStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<CancelPurchaseOrderCommand>
{
    public async Task Handle(CancelPurchaseOrderCommand c, CancellationToken ct)
    {
        var order = await LoadAsync(c.Id, c.Request.RowVersion, ct);
        var hasPostedReceipt = await repository.HasPostedReceiptAsync(order.Id, ct);
        if (hasPostedReceipt)
            throw new ConflictException("purchasing_order_has_posted_receipt", "لا يمكن إلغاء أمر شراء يحتوي على استلام مرحل.");
        if (order.Status == DomainOrderStatus.Sent)
        {
            var received = await repository.GetPostedReceivedBaseQuantitiesAsync(order.Id, ct);
            var open = order.Lines.Select(x => { received.TryGetValue(x.Id, out var r); return new PurchasingOnOrderLine(x.ProductVariantId, Math.Max(0m, x.BaseQuantity-r)); })
                .Where(x => x.BaseQuantity > 0).ToArray();
            await inventory.DecreaseOnOrderAsync(order.DestinationWarehouseId, open, order.Id, order.OrderDate, ct);
        }
        order.Cancel(Now, UserId, c.Request.Reason, false);
        Save(order);
    }
}

public sealed class ClosePurchaseOrderCommandHandler(
    IPurchaseOrderRepository repository,
    IPurchasingInventoryPort inventory,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : PurchaseOrderStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<ClosePurchaseOrderCommand>
{
    public async Task Handle(ClosePurchaseOrderCommand c, CancellationToken ct)
    {
        var order = await LoadAsync(c.Id, c.Request.RowVersion, ct);
        if (!await repository.CanCloseAsync(order.Id, ct))
            throw new ConflictException("purchasing_order_close_requirements_not_met", "لا يمكن إغلاق أمر الشراء لوجود استلامات أو فواتير أو فروقات معلقة.");

        if (order.Status == DomainOrderStatus.PartiallyReceived)
        {
            var received = await repository.GetPostedReceivedBaseQuantitiesAsync(order.Id, ct);
            var open = order.Lines.Select(x =>
            {
                received.TryGetValue(x.Id, out var receivedBase);
                return new PurchasingOnOrderLine(x.ProductVariantId, Math.Max(0m, x.BaseQuantity - receivedBase));
            }).Where(x => x.BaseQuantity > 0).ToArray();

            if (open.Length > 0)
                await inventory.DecreaseOnOrderAsync(order.DestinationWarehouseId, open, order.Id, order.OrderDate, ct);
        }

        order.Close(Now, UserId);
        Save(order);
    }
}
