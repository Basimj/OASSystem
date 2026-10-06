using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.CustomerDemand;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.Services;

public sealed class CustomerDemandSourcingService(
    IPurchaseRequestRepository requests,
    IPurchaseOrderRepository purchaseOrders,
    ISupplierCatalogRepository catalog,
    IPurchasingReferenceDataPort references,
    IPurchasingCommercialTermsPort commercialTerms,
    IPurchasingCodeService codes,
    IRepository<PurchaseRequestLine, Guid> lines,
    TimeProvider timeProvider) : ICustomerDemandSourcingService
{
    public async Task AssignSupplierAsync(
        Guid purchaseRequestLineId,
        AssignCustomerDemandSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var (header, line) = await LoadLineAsync(purchaseRequestLineId, request.RowVersion, cancellationToken);
        EnsureCustomerDemandOpen(header);
        var allocations = await requests.GetAllocatedQuantitiesAsync([line.Id], cancellationToken);
        if (allocations.TryGetValue(line.Id, out var allocated) && allocated > 0m)
            throw new ConflictException("purchasing_customer_demand_committed", "لا يمكن استبدال المورد المفضل بعد ربط السطر بأمر شراء؛ استخدم إعادة توجيه المتبقي.");

        if (request.PreferredSupplierId.HasValue)
            PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(request.PreferredSupplierId.Value, cancellationToken));

        line.Update(line.LineSequence, line.ProductVariantId, line.RequestedQuantity, line.RequiredDate,
            line.CustomerOrderLineId, request.PreferredSupplierId, line.Notes);
        lines.Update(line);
    }

    public async Task ScheduleAsync(
        Guid purchaseRequestLineId,
        ScheduleCustomerDemandRequest request,
        CancellationToken cancellationToken = default)
    {
        var (header, line) = await LoadLineAsync(purchaseRequestLineId, request.RowVersion, cancellationToken);
        EnsureCustomerDemandOpen(header);
        line.ScheduleOrder(request.ScheduledOrderAtUtc);
        lines.Update(line);
    }

    public async Task<Guid> CreatePurchaseOrderAsync(
        Guid purchaseRequestLineId,
        CreateCustomerDemandPurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var (header, line) = await LoadLineAsync(purchaseRequestLineId, request.RowVersion, cancellationToken);
        EnsureConvertible(header);
        return await AllocateRemainingAsync(header, line, request.SupplierId, request.ExpectedDeliveryDate,
            request.ExistingDraftPurchaseOrderId, cancellationToken);
    }

    public async Task<Guid> ResourceRemainingAsync(
        Guid purchaseRequestLineId,
        ResourceCustomerDemandRemainingRequest request,
        CancellationToken cancellationToken = default)
    {
        var (header, line) = await LoadLineAsync(purchaseRequestLineId, request.RowVersion, cancellationToken);
        EnsureConvertible(header);
        var allocated = await requests.GetAllocatedQuantitiesAsync([line.Id], cancellationToken);
        allocated.TryGetValue(line.Id, out var allocatedQuantity);
        if (allocatedQuantity <= 0m)
            throw new ConflictException("purchasing_customer_demand_not_sourced", "السطر لم يتم توريده سابقًا؛ استخدم إنشاء أمر شراء بدل إعادة التوجيه.");
        if (allocatedQuantity >= line.RequestedQuantity)
            throw new ConflictException("purchasing_customer_demand_no_remaining", "لا توجد كمية متبقية لإعادة توجيهها لمورد آخر.");

        line.ScheduleOrder(request.ScheduledOrderAtUtc);
        lines.Update(line);
        return await AllocateRemainingAsync(header, line, request.NewSupplierId, request.ExpectedDeliveryDate, null, cancellationToken);
    }

    private async Task<Guid> AllocateRemainingAsync(
        PurchaseRequest header,
        PurchaseRequestLine requestLine,
        Guid supplierId,
        DateOnly? expectedDeliveryDate,
        Guid? existingDraftPurchaseOrderId,
        CancellationToken ct)
    {
        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(supplierId, ct));
        var allocatedMap = await requests.GetAllocatedQuantitiesAsync([requestLine.Id], ct);
        allocatedMap.TryGetValue(requestLine.Id, out var allocated);
        var remaining = requestLine.RequestedQuantity - allocated;
        if (remaining <= 0m)
            throw new ConflictException("purchasing_customer_demand_no_remaining", "تم تخصيص كامل الكمية المطلوبة مسبقًا.");

        var orderDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var terms = await commercialTerms.ResolveAsync(supplierId, orderDate, ct);
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(terms.CurrencyId, ct));
        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(header.WarehouseId, ct));
        var defaults = await catalog.GetPurchaseDefaultsAsync(supplierId, requestLine.ProductVariantId, terms.CurrencyId, ct)
            ?? throw new ConflictException("purchasing_supplier_catalog_missing", "لا يوجد كتالوج/سعر صالح للمورد والمنتج والعملة المحددة.");
        var product = await references.GetProductVariantAsync(requestLine.ProductVariantId, ct);
        PurchasingApplicationGuard.Product(product);
        var unit = await references.GetUnitAsync(defaults.PurchaseUnitId, ct);
        PurchasingApplicationGuard.Unit(unit);

        PurchaseOrder order;
        var sources = new List<PurchaseOrderLineSource>();
        if (existingDraftPurchaseOrderId.HasValue)
        {
            order = await purchaseOrders.GetForUpdateAsync(existingDraftPurchaseOrderId.Value, ct)
                ?? throw new NotFoundException(nameof(PurchaseOrder), existingDraftPurchaseOrderId.Value);
            if (order.Status != PurchaseOrderStatus.Draft || order.SupplierId != supplierId || order.DestinationWarehouseId != header.WarehouseId || order.CurrencyId != terms.CurrencyId)
                throw new ConflictException("purchasing_customer_demand_draft_po_mismatch", "أمر الشراء المحدد لا يطابق المورد/المخزن/العملة أو لم يعد مسودة.");
            sources.AddRange(await purchaseOrders.GetSourcesAsync(order.Id, ct));
        }
        else
        {
            var id = Guid.NewGuid();
            order = PurchaseOrder.Create(
                id,
                await codes.NextPurchaseOrderCodeAsync(orderDate, ct),
                supplierId,
                header.WarehouseId,
                orderDate,
                expectedDeliveryDate,
                terms.CurrencyId,
                terms.ExchangeRate,
                terms.ExchangeRateDate,
                terms.TaxCalculationMode,
                terms.PaymentTermDays,
                $"Customer demand {header.RequestCode}");
        }

        var orderedQty = Math.Round(remaining / defaults.UnitConversionFactor, 3, MidpointRounding.AwayFromZero);
        var lineId = Guid.NewGuid();
        var lineSequence = order.Lines.Count == 0 ? 1 : order.Lines.Max(x => x.LineSequence) + 1;
        var poLine = PurchaseOrderLine.Create(
            lineId, order.Id, lineSequence, requestLine.ProductVariantId,
            defaults.CatalogItemId, defaults.PurchaseUnitId, defaults.UnitConversionFactor,
            product!.ProductCode, product.ProductName, unit!.Name,
            orderedQty, defaults.UnitPrice, 0m, 0m, order.TaxCalculationMode,
            expectedDeliveryDate ?? requestLine.RequiredDate, requestLine.Notes);
        order.AddLine(poLine);
        sources.Add(PurchaseOrderLineSource.Create(Guid.NewGuid(), lineId, requestLine.Id, remaining));

        if (existingDraftPurchaseOrderId.HasValue)
            await purchaseOrders.ReplaceLinesAndSourcesAsync(order, sources, ct);
        else
            await purchaseOrders.AddAsync(order, sources, ct);

        var totalRequested = header.Lines.Sum(x => x.RequestedQuantity);
        var allIds = header.Lines.Select(x => x.Id).ToArray();
        var allAllocated = await requests.GetAllocatedQuantitiesAsync(allIds, ct);
        var newAllocatedTotal = allAllocated.Values.Sum() + remaining;
        header.MarkConversion(newAllocatedTotal, totalRequested);
        requests.Update(header);
        return order.Id;
    }

    private async Task<(PurchaseRequest Header, PurchaseRequestLine Line)> LoadLineAsync(
        Guid lineId,
        string rowVersion,
        CancellationToken ct)
    {
        var header = await requests.GetByLineIdAsync(lineId, ct)
            ?? throw new NotFoundException("PurchaseRequestLine", lineId);
        var line = header.Lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new NotFoundException("PurchaseRequestLine", lineId);
        PurchasingRowVersion.EnsureMatches(line.RowVersion, rowVersion, "Purchase request line");
        return (header, line);
    }

    private static void EnsureCustomerDemandOpen(PurchaseRequest header)
    {
        if (header.RequestType != PurchaseRequestType.CustomerDemand || header.Status is PurchaseRequestStatus.Cancelled or PurchaseRequestStatus.Rejected or PurchaseRequestStatus.Converted)
            throw new ConflictException("purchasing_customer_demand_not_open", "سطر النقص ليس ضمن طلب عميل مفتوح.");
    }

    private static void EnsureConvertible(PurchaseRequest header)
    {
        if (header.RequestType != PurchaseRequestType.CustomerDemand)
            throw new ConflictException("purchasing_customer_demand_invalid_type", "السطر ليس من نوع CustomerDemand.");
        if (header.Status is not (PurchaseRequestStatus.Approved or PurchaseRequestStatus.PartiallyConverted))
            throw new ConflictException("purchasing_customer_demand_not_approved", "يجب اعتماد طلب النقص قبل إنشاء أمر شراء.");
    }
}
