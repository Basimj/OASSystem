using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Common;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.Services;

public sealed class CustomerDemandProcurementService(
    IPurchaseRequestRepository requests,
    IReadRepository<PurchaseRequest, Guid> requestHeaders,
    IRepository<PurchaseRequestLine, Guid> lines,
    IPurchasingReferenceDataPort references,
    IPurchasingCodeService codes,
    ICurrentUser currentUser) : ICustomerDemandProcurementPort
{
    private static readonly PurchaseRequestStatus[] CoverageStatuses =
    [
        PurchaseRequestStatus.Draft,
        PurchaseRequestStatus.PendingApproval,
        PurchaseRequestStatus.Approved,
        PurchaseRequestStatus.PartiallyConverted,
        PurchaseRequestStatus.Converted
    ];

    private static readonly PurchaseRequestStatus[] OpenStatuses =
    [
        PurchaseRequestStatus.Draft,
        PurchaseRequestStatus.PendingApproval,
        PurchaseRequestStatus.Approved,
        PurchaseRequestStatus.PartiallyConverted
    ];

    public async Task CreateOrUpdateShortageAsync(
        CustomerDemandShortage shortage,
        CancellationToken cancellationToken = default)
    {
        if (shortage.CustomerOrderId == Guid.Empty || shortage.CustomerOrderLineId == Guid.Empty ||
            shortage.WarehouseId == Guid.Empty || shortage.ProductVariantId == Guid.Empty)
            throw new ConflictException("purchasing_customer_demand_invalid", "بيانات نقص طلب العميل غير مكتملة.");
        if (shortage.ShortageQuantity <= 0m)
            return;

        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(shortage.WarehouseId, cancellationToken));
        PurchasingApplicationGuard.Product(await references.GetProductVariantAsync(shortage.ProductVariantId, cancellationToken));
        if (shortage.PreferredSupplierId.HasValue)
            PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(shortage.PreferredSupplierId.Value, cancellationToken));

        var matchingLines = await lines.ListAsync(
            new Specification<PurchaseRequestLine>().Where(x =>
                x.CustomerOrderLineId == shortage.CustomerOrderLineId &&
                x.ProductVariantId == shortage.ProductVariantId),
            cancellationToken);

        var coveredQuantity = 0m;
        PurchaseRequest? draftTarget = null;
        PurchaseRequestLine? draftLine = null;

        foreach (var line in matchingLines)
        {
            var parent = await requests.GetByLineIdAsync(line.Id, cancellationToken);
            if (parent is null || parent.RequestType != PurchaseRequestType.CustomerDemand ||
                parent.CustomerOrderId != shortage.CustomerOrderId || !CoverageStatuses.Contains(parent.Status))
                continue;

            coveredQuantity += line.RequestedQuantity;
            if (parent.Status == PurchaseRequestStatus.Draft && parent.WarehouseId == shortage.WarehouseId && draftTarget is null)
            {
                draftTarget = parent;
                draftLine = parent.Lines.SingleOrDefault(x => x.Id == line.Id) ?? line;
            }
        }

        if (coveredQuantity >= shortage.ShortageQuantity)
            return;

        var delta = shortage.ShortageQuantity - coveredQuantity;

        if (draftTarget is null)
        {
            var headerSpec = new Specification<PurchaseRequest>().Where(x =>
                x.RequestType == PurchaseRequestType.CustomerDemand &&
                x.CustomerOrderId == shortage.CustomerOrderId &&
                x.WarehouseId == shortage.WarehouseId &&
                x.Status == PurchaseRequestStatus.Draft);
            var draftHeader = (await requestHeaders.ListAsync(headerSpec, cancellationToken)).FirstOrDefault();
            if (draftHeader is not null)
                draftTarget = await requests.GetForUpdateAsync(draftHeader.Id, cancellationToken);
        }

        if (draftTarget is null)
        {
            var requestId = Guid.NewGuid();
            var request = PurchaseRequest.Create(
                requestId,
                await codes.NextPurchaseRequestCodeAsync(shortage.RequestDate, cancellationToken),
                PurchaseRequestType.CustomerDemand,
                shortage.WarehouseId,
                shortage.CustomerOrderId,
                shortage.RequestDate,
                shortage.RequiredDate,
                "Customer order shortage",
                shortage.Notes,
                currentUser.UserId);

            var line = PurchaseRequestLine.Create(
                Guid.NewGuid(),
                requestId,
                1,
                shortage.ProductVariantId,
                delta,
                shortage.RequiredDate,
                shortage.CustomerOrderLineId,
                shortage.PreferredSupplierId,
                shortage.Notes);
            line.ScheduleOrder(shortage.ScheduledOrderAtUtc);
            request.AddLine(line);
            await requests.AddAsync(request, cancellationToken);
            return;
        }

        draftLine ??= draftTarget.Lines.FirstOrDefault(x =>
            x.CustomerOrderLineId == shortage.CustomerOrderLineId && x.ProductVariantId == shortage.ProductVariantId);

        if (draftLine is not null)
        {
            draftLine.Update(
                draftLine.LineSequence,
                draftLine.ProductVariantId,
                draftLine.RequestedQuantity + delta,
                shortage.RequiredDate ?? draftLine.RequiredDate,
                shortage.CustomerOrderLineId,
                shortage.PreferredSupplierId ?? draftLine.PreferredSupplierId,
                shortage.Notes ?? draftLine.Notes);
            if (shortage.ScheduledOrderAtUtc.HasValue)
                draftLine.ScheduleOrder(shortage.ScheduledOrderAtUtc);
        }
        else
        {
            var nextSequence = draftTarget.Lines.Count == 0 ? 1 : draftTarget.Lines.Max(x => x.LineSequence) + 1;
            var newLine = PurchaseRequestLine.Create(
                Guid.NewGuid(),
                draftTarget.Id,
                nextSequence,
                shortage.ProductVariantId,
                delta,
                shortage.RequiredDate,
                shortage.CustomerOrderLineId,
                shortage.PreferredSupplierId,
                shortage.Notes);
            newLine.ScheduleOrder(shortage.ScheduledOrderAtUtc);
            draftTarget.AddLine(newLine);
        }

        await requests.ReplaceLinesAsync(draftTarget, cancellationToken);
        requests.Update(draftTarget);
    }

    public async Task<IReadOnlyList<CustomerDemandLine>> GetOpenDemandForOrderAsync(
        Guid customerOrderId,
        CancellationToken cancellationToken = default)
    {
        var headers = await requestHeaders.ListAsync(
            new Specification<PurchaseRequest>().Where(x =>
                x.RequestType == PurchaseRequestType.CustomerDemand &&
                x.CustomerOrderId == customerOrderId &&
                OpenStatuses.Contains(x.Status)),
            cancellationToken);

        if (headers.Count == 0)
            return [];

        var requestIds = headers.Select(x => x.Id).ToArray();
        var demandLines = await lines.ListAsync(
            new Specification<PurchaseRequestLine>().Where(x =>
                requestIds.Contains(x.PurchaseRequestId) && x.CustomerOrderLineId.HasValue),
            cancellationToken);

        return demandLines.Select(x => new CustomerDemandLine(
            x.PurchaseRequestId,
            x.Id,
            customerOrderId,
            x.CustomerOrderLineId!.Value,
            x.ProductVariantId,
            x.RequestedQuantity,
            x.PreferredSupplierId,
            x.ScheduledOrderAtUtc)).ToArray();
    }

    public async Task ReassignPreferredSupplierAsync(
        Guid customerOrderLineId,
        Guid productVariantId,
        Guid? preferredSupplierId,
        DateTimeOffset? scheduledOrderAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (preferredSupplierId.HasValue)
            PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(preferredSupplierId.Value, cancellationToken));

        var matching = await lines.ListAsync(
            new Specification<PurchaseRequestLine>().Where(x =>
                x.CustomerOrderLineId == customerOrderLineId && x.ProductVariantId == productVariantId),
            cancellationToken);

        var open = new List<PurchaseRequestLine>();
        foreach (var line in matching)
        {
            var parent = await requests.GetByLineIdAsync(line.Id, cancellationToken);
            if (parent is not null && parent.RequestType == PurchaseRequestType.CustomerDemand && OpenStatuses.Contains(parent.Status))
                open.Add(parent.Lines.SingleOrDefault(x => x.Id == line.Id) ?? line);
        }

        if (open.Count == 0)
            throw new NotFoundException("CustomerDemand", customerOrderLineId);
        if (open.Count > 1)
            throw new ConflictException("purchasing_customer_demand_duplicate", "يوجد أكثر من طلب نقص فعال لنفس سطر العميل والمنتج.");

        var target = open[0];
        var allocated = await requests.GetAllocatedQuantitiesAsync([target.Id], cancellationToken);
        if (allocated.TryGetValue(target.Id, out var allocatedQuantity) && allocatedQuantity > 0m)
            throw new ConflictException("purchasing_customer_demand_committed", "لا يمكن تغيير المورد بعد ربط النقص بأمر شراء.");

        target.Update(
            target.LineSequence,
            target.ProductVariantId,
            target.RequestedQuantity,
            target.RequiredDate,
            target.CustomerOrderLineId,
            preferredSupplierId,
            target.Notes);
        target.ScheduleOrder(scheduledOrderAtUtc);
        lines.Update(target);
    }
}
