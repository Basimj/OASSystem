using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Domain.Purchasing.Entities;
using DomainRequestStatus = OAS.Domain.Purchasing.Enums.PurchaseRequestStatus;
using DomainRequestType = OAS.Domain.Purchasing.Enums.PurchaseRequestType;

namespace OAS.Application.Purchasing.PurchaseRequests.Commands;

public sealed record CreatePurchaseRequestCommand(CreatePurchaseRequestRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.Create]; }
public sealed record UpdatePurchaseRequestCommand(Guid Id, UpdatePurchaseRequestRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.Edit]; }
public sealed record SubmitPurchaseRequestCommand(Guid Id, SetPurchaseRequestStatusRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.Edit]; }
public sealed record ApprovePurchaseRequestCommand(Guid Id, SetPurchaseRequestStatusRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.Approve]; }
public sealed record RejectPurchaseRequestCommand(Guid Id, SetPurchaseRequestStatusRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.Approve]; }
public sealed record CancelPurchaseRequestCommand(Guid Id, SetPurchaseRequestStatusRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.Cancel]; }
public sealed record CreatePurchaseOrderFromRequestCommand(Guid Id, CreatePurchaseOrderFromRequestRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.Edit, PurchasingPermissions.Orders.Create]; }

public sealed class CreatePurchaseRequestCommandHandler(
    IPurchaseRequestRepository repository,
    IPurchasingReferenceDataPort references,
    IPurchasingCodeService codes,
    ICurrentUser currentUser) : IRequestHandler<CreatePurchaseRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseRequestCommand command, CancellationToken ct)
    {
        var r = command.Request;
        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(r.WarehouseId, ct));
        var id = Guid.NewGuid();
        var code = await codes.NextPurchaseRequestCodeAsync(r.RequestDate, ct);
        var entity = PurchaseRequest.Create(id, code, (DomainRequestType)(byte)r.RequestType, r.WarehouseId, r.CustomerOrderId,
            r.RequestDate, r.RequiredDate, r.Reason, r.Notes, currentUser.UserId);
        foreach (var line in r.Lines.OrderBy(x => x.LineSequence))
        {
            PurchasingApplicationGuard.Product(await references.GetProductVariantAsync(line.ProductVariantId, ct));
            if (line.PreferredSupplierId.HasValue)
                PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(line.PreferredSupplierId.Value, ct));
            var requestLine = PurchaseRequestLine.Create(Guid.NewGuid(), id, line.LineSequence, line.ProductVariantId,
                line.RequestedQuantity, line.RequiredDate, line.CustomerOrderLineId, line.PreferredSupplierId, line.Notes);
            requestLine.ScheduleOrder(line.ScheduledOrderAtUtc);
            entity.AddLine(requestLine);
        }
        await repository.AddAsync(entity, ct);
        return id;
    }
}

public sealed class UpdatePurchaseRequestCommandHandler(
    IPurchaseRequestRepository repository,
    IPurchasingReferenceDataPort references) : IRequestHandler<UpdatePurchaseRequestCommand>
{
    public async Task Handle(UpdatePurchaseRequestCommand command, CancellationToken ct)
    {
        var entity = await repository.GetForUpdateAsync(command.Id, ct) ?? throw new NotFoundException("PurchaseRequest", command.Id);
        PurchasingRowVersion.EnsureMatches(entity.RowVersion, command.Request.RowVersion, "Purchase request");
        if (entity.Status != DomainRequestStatus.Draft)
            throw new ConflictException("purchasing_request_not_draft", "يمكن تعديل طلب الشراء عندما يكون مسودة فقط.");

        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(command.Request.WarehouseId, ct));
        entity.UpdateRequestType((DomainRequestType)(byte)command.Request.RequestType);
        entity.UpdateHeader(command.Request.WarehouseId, command.Request.CustomerOrderId, command.Request.RequestDate,
            command.Request.RequiredDate, command.Request.Reason, command.Request.Notes);

        var incomingIds = command.Request.Lines.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        foreach (var old in entity.Lines.Where(x => !incomingIds.Contains(x.Id)).ToArray()) entity.RemoveLine(old.Id);

        foreach (var line in command.Request.Lines.OrderBy(x => x.LineSequence))
        {
            PurchasingApplicationGuard.Product(await references.GetProductVariantAsync(line.ProductVariantId, ct));
            if (line.PreferredSupplierId.HasValue)
                PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(line.PreferredSupplierId.Value, ct));

            if (line.Id.HasValue)
            {
                var existing = entity.Lines.SingleOrDefault(x => x.Id == line.Id.Value)
                    ?? throw new ConflictException("purchasing_request_line_missing", "أحد بنود طلب الشراء لم يعد موجودًا. أعد تحميل البيانات.");
                if (string.IsNullOrWhiteSpace(line.RowVersion))
                    throw new ConcurrencyException("Purchase request line row version is required.");
                PurchasingRowVersion.EnsureMatches(existing.RowVersion, line.RowVersion, "Purchase request line");
                existing.Update(line.LineSequence, line.ProductVariantId, line.RequestedQuantity, line.RequiredDate,
                    line.CustomerOrderLineId, line.PreferredSupplierId, line.Notes);
                existing.ScheduleOrder(line.ScheduledOrderAtUtc);
            }
            else
            {
                var newLine = PurchaseRequestLine.Create(Guid.NewGuid(), entity.Id, line.LineSequence, line.ProductVariantId,
                    line.RequestedQuantity, line.RequiredDate, line.CustomerOrderLineId, line.PreferredSupplierId, line.Notes);
                newLine.ScheduleOrder(line.ScheduledOrderAtUtc);
                entity.AddLine(newLine);
            }
        }
        await repository.ReplaceLinesAsync(entity, ct);
        repository.Update(entity);
    }
}

public abstract class PurchaseRequestStatusHandlerBase(IPurchaseRequestRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    protected async Task<PurchaseRequest> LoadAsync(Guid id, string rowVersion, CancellationToken ct)
    {
        var entity = await repository.GetForUpdateAsync(id, ct) ?? throw new NotFoundException("PurchaseRequest", id);
        PurchasingRowVersion.EnsureMatches(entity.RowVersion, rowVersion, "Purchase request");
        return entity;
    }
    protected string? UserId => currentUser.UserId;
    protected DateTimeOffset Now => timeProvider.GetUtcNow();
    protected void Save(PurchaseRequest request) => repository.Update(request);
}

public sealed class SubmitPurchaseRequestCommandHandler(IPurchaseRequestRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : PurchaseRequestStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<SubmitPurchaseRequestCommand>
{ public async Task Handle(SubmitPurchaseRequestCommand c, CancellationToken ct) { var e = await LoadAsync(c.Id, c.Request.RowVersion, ct); e.Submit(Now, UserId); Save(e); } }

public sealed class ApprovePurchaseRequestCommandHandler(IPurchaseRequestRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : PurchaseRequestStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<ApprovePurchaseRequestCommand>
{ public async Task Handle(ApprovePurchaseRequestCommand c, CancellationToken ct) { var e = await LoadAsync(c.Id, c.Request.RowVersion, ct); e.Approve(Now, UserId); Save(e); } }

public sealed class RejectPurchaseRequestCommandHandler(IPurchaseRequestRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : PurchaseRequestStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<RejectPurchaseRequestCommand>
{ public async Task Handle(RejectPurchaseRequestCommand c, CancellationToken ct) { var e = await LoadAsync(c.Id, c.Request.RowVersion, ct); e.Reject(Now, UserId, c.Request.Reason ?? string.Empty); Save(e); } }

public sealed class CancelPurchaseRequestCommandHandler(IPurchaseRequestRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
    : PurchaseRequestStatusHandlerBase(repository, currentUser, timeProvider), IRequestHandler<CancelPurchaseRequestCommand>
{ public async Task Handle(CancelPurchaseRequestCommand c, CancellationToken ct) { var e = await LoadAsync(c.Id, c.Request.RowVersion, ct); e.Cancel(Now, UserId, c.Request.Reason); Save(e); } }

public sealed class CreatePurchaseOrderFromRequestCommandHandler(
    IPurchaseRequestRepository requestRepository,
    IPurchaseOrderRepository orderRepository,
    ISupplierCatalogRepository catalogRepository,
    IPurchasingReferenceDataPort references,
    IPurchasingCodeService codes) : IRequestHandler<CreatePurchaseOrderFromRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseOrderFromRequestCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetForUpdateAsync(command.Id, ct) ?? throw new NotFoundException("PurchaseRequest", command.Id);
        PurchasingRowVersion.EnsureMatches(request.RowVersion, command.Request.RowVersion, "Purchase request");
        if (request.Status is not (DomainRequestStatus.Approved or DomainRequestStatus.PartiallyConverted))
            throw new ConflictException("purchasing_request_not_approved", "لا يمكن تحويل طلب شراء غير معتمد إلى أمر شراء.");

        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(command.Request.SupplierId, ct));
        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(command.Request.DestinationWarehouseId, ct));
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(command.Request.CurrencyId, ct));

        var requestLines = request.Lines.ToDictionary(x => x.Id);
        var currentAllocated = await requestRepository.GetAllocatedQuantitiesAsync(requestLines.Keys.ToArray(), ct);
        var orderId = Guid.NewGuid();
        var code = await codes.NextPurchaseOrderCodeAsync(command.Request.OrderDate, ct);
        var order = PurchaseOrder.Create(orderId, code, command.Request.SupplierId, command.Request.DestinationWarehouseId,
            command.Request.OrderDate, command.Request.ExpectedDeliveryDate, command.Request.CurrencyId, command.Request.ExchangeRate,
            command.Request.ExchangeRateDate, (OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)command.Request.TaxCalculationMode, command.Request.PaymentTermDays, command.Request.Notes);
        var sources = new List<PurchaseOrderLineSource>();
        var sequence = 1;

        foreach (var allocation in command.Request.Allocations)
        {
            if (!requestLines.TryGetValue(allocation.PurchaseRequestLineId, out var requestLine))
                throw new ConflictException("purchasing_request_line_not_found", "سطر طلب الشراء المحدد غير موجود.");
            currentAllocated.TryGetValue(requestLine.Id, out var alreadyAllocated);
            if (allocation.AllocatedQuantity <= 0 || alreadyAllocated + allocation.AllocatedQuantity > requestLine.RequestedQuantity)
                throw new ConflictException("purchasing_request_overallocated", "لا يمكن تخصيص كمية لأمر الشراء أكبر من الكمية المتبقية في طلب الشراء.");

            var product = await references.GetProductVariantAsync(requestLine.ProductVariantId, ct);
            PurchasingApplicationGuard.Product(product);

            var defaults = await catalogRepository.GetPurchaseDefaultsAsync(
                command.Request.SupplierId, requestLine.ProductVariantId, command.Request.CurrencyId, ct);

            Guid? supplierCatalogItemId;
            Guid purchaseUnitId;
            decimal unitConversionFactor;
            decimal unitPrice;

            if (defaults is not null)
            {
                // Use supplier-specific purchasing defaults whenever a catalog entry exists.
                // Missing price is allowed for a Draft PO; the user can enter it before approval/send.
                supplierCatalogItemId = defaults.CatalogItemId;
                purchaseUnitId = defaults.PurchaseUnitId;
                unitConversionFactor = defaults.UnitConversionFactor;
                unitPrice = defaults.UnitPrice ?? 0m;
            }
            else
            {
                // Do not block PR -> PO conversion just because the supplier catalog has not been prepared yet.
                // Fall back to the product's default/base unit and create the PO line with price = 0.
                if (!product!.DefaultUnitId.HasValue || product.DefaultUnitId.Value == Guid.Empty)
                    throw new ConflictException(
                        "purchasing_product_default_unit_missing",
                        "لا يوجد كتالوج مورد لهذا الصنف، كما أن الصنف لا يحتوي على وحدة افتراضية يمكن استخدامها لإنشاء مسودة أمر الشراء.");

                supplierCatalogItemId = null;
                purchaseUnitId = product.DefaultUnitId.Value;
                unitConversionFactor = 1m;
                unitPrice = 0m;
            }

            var unit = await references.GetUnitAsync(purchaseUnitId, ct);
            PurchasingApplicationGuard.Unit(unit);

            var orderedQty = Math.Round(allocation.AllocatedQuantity / unitConversionFactor, 3);
            var lineId = Guid.NewGuid();
            order.AddLine(PurchaseOrderLine.Create(lineId, orderId, sequence++, requestLine.ProductVariantId,
                supplierCatalogItemId, purchaseUnitId, unitConversionFactor, product!.ProductCode,
                product.ProductName, unit!.Name, orderedQty, unitPrice, 0m, 0m, order.TaxCalculationMode,
                requestLine.RequiredDate ?? command.Request.ExpectedDeliveryDate, requestLine.Notes));
            sources.Add(PurchaseOrderLineSource.Create(Guid.NewGuid(), lineId, requestLine.Id, allocation.AllocatedQuantity));
        }

        if (order.Lines.Count == 0)
            throw new ConflictException("purchasing_order_lines_required", "يجب تخصيص بند واحد على الأقل لإنشاء أمر الشراء.");

        await orderRepository.AddAsync(order, sources, ct);
        var totalRequested = request.Lines.Sum(x => x.RequestedQuantity);
        var newAllocatedTotal = currentAllocated.Values.Sum() + command.Request.Allocations.Sum(x => x.AllocatedQuantity);
        request.MarkConversion(newAllocatedTotal, totalRequested);
        requestRepository.Update(request);
        return orderId;
    }
}
