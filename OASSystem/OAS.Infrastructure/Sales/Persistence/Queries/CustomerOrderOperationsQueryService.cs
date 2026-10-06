using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.OrderOperations;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using ContractOrderStatus = OAS.Contracts.Sales.Enums.CustomerOrderStatus;
using ContractLineType = OAS.Contracts.Sales.Enums.SalesLineType;
using ContractEyeSide = OAS.Contracts.Sales.Enums.EyeSide;
using ContractMeasurementSource = OAS.Contracts.Sales.Enums.OpticalMeasurementSource;
using ContractPrismBase = OAS.Contracts.Sales.Enums.PrismBaseDirection;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

/// <summary>
/// Read model for customer-order operations. It projects the authoritative order,
/// stock-reservation, CustomerDemand, PO/receipt and OpticalJob documents without
/// introducing a tracking table or exposing finance fields.
/// </summary>
public sealed class CustomerOrderOperationsQueryService(
    OasDbContext db,
    TimeProvider timeProvider) : ICustomerOrderOperationsQueryService
{
    private static readonly PurchaseRequestStatus[] DemandStatuses =
    [
        PurchaseRequestStatus.Draft,
        PurchaseRequestStatus.PendingApproval,
        PurchaseRequestStatus.Approved,
        PurchaseRequestStatus.PartiallyConverted,
        PurchaseRequestStatus.Converted
    ];

    private static readonly PurchaseOrderStatus[] OrderedStatuses =
    [
        PurchaseOrderStatus.Sent,
        PurchaseOrderStatus.PartiallyReceived,
        PurchaseOrderStatus.FullyReceived,
        PurchaseOrderStatus.Closed
    ];

    public async Task<PagedResult<CustomerOrderOperationsItemDto>> GetPageAsync(
        CustomerOrderOperationsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.ToPageRequest().Normalize();
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var query = BuildBaseQuery(request, today);
        query = ApplySort(query, request.SortBy, request.SortDirection);

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var orderIds = rows.Select(x => x.CustomerOrderId).ToArray();
        var itemsByOrder = await BuildItemsSummaryAsync(orderIds, cancellationToken);
        var supplyByOrder = await BuildSupplySummariesAsync(orderIds, today, cancellationToken);

        return new PagedResult<CustomerOrderOperationsItemDto>
        {
            Items = rows.Select(x => new CustomerOrderOperationsItemDto(
                x.CustomerOrderId,
                x.OrderCode,
                x.OrderDate,
                x.RequiredDate,
                x.CustomerId,
                x.CustomerCode,
                x.CustomerName,
                x.Mobile,
                itemsByOrder.GetValueOrDefault(x.CustomerOrderId) ?? string.Empty,
                x.TotalLines,
                x.AvailableLines,
                x.ShortageLines,
                supplyByOrder.GetValueOrDefault(x.CustomerOrderId) ?? EmptySupplySummary(),
                x.RequiresProduction,
                (ContractOrderStatus)(byte)x.Status,
                x.OpticalJobId,
                x.OpticalJobCode,
                x.TechnicianId,
                x.TechnicianName,
                x.LastUpdatedAt)).ToArray(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = total
        };
    }

    public async Task<CustomerOrderOperationsSummaryDto> GetSummaryAsync(
        CustomerOrderOperationsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var baseRequest = request with { Status = null, Availability = null, OverdueOnly = false };
        var query = BuildBaseQuery(baseRequest, today);

        var total = await query.CountAsync(cancellationToken);
        var todayOrders = await query.CountAsync(x => x.OrderDate == today, cancellationToken);
        var awaiting = await query.CountAsync(x => x.Status == OAS.Domain.Sales.Enums.CustomerOrderStatus.AwaitingStock || x.Status == OAS.Domain.Sales.Enums.CustomerOrderStatus.PartiallyAvailable, cancellationToken);
        var readyForProduction = await query.CountAsync(x => x.Status == OAS.Domain.Sales.Enums.CustomerOrderStatus.ReadyForProduction, cancellationToken);
        var inProduction = await query.CountAsync(x => x.Status == OAS.Domain.Sales.Enums.CustomerOrderStatus.InProduction, cancellationToken);
        var readyForDelivery = await query.CountAsync(x => x.Status == OAS.Domain.Sales.Enums.CustomerOrderStatus.ReadyForDelivery, cancellationToken);
        var overdue = await query.CountAsync(x => x.RequiredDate != null && x.RequiredDate < today && x.Status != OAS.Domain.Sales.Enums.CustomerOrderStatus.Completed && x.Status != OAS.Domain.Sales.Enums.CustomerOrderStatus.Cancelled, cancellationToken);

        return new CustomerOrderOperationsSummaryDto(todayOrders, awaiting, readyForProduction, inProduction, readyForDelivery, overdue, total);
    }

    public async Task<CustomerOrderOperationsDetailsDto> GetDetailsAsync(
        Guid customerOrderId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var order = await (
            from o in db.Set<CustomerOrder>().AsNoTracking()
            join c in db.Set<Customer>().AsNoTracking() on o.CustomerId equals c.Id
            join j in db.Set<OpticalJob>().AsNoTracking() on o.Id equals j.CustomerOrderId into jobJoin
            from j in jobJoin.Where(x => x.IsActive || x.Status == OAS.Domain.Sales.Enums.OpticalJobStatus.Delivered).DefaultIfEmpty()
            where o.Id == customerOrderId
            select new
            {
                Order = o,
                Customer = c,
                OpticalJobId = j == null ? null : (Guid?)j.Id,
                OpticalJobCode = j == null ? null : j.JobCode
            }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), customerOrderId);

        var lines = await BuildLineDetailsAsync(customerOrderId, today, cancellationToken);
        var supply = (await BuildSupplySummariesAsync([customerOrderId], today, cancellationToken))
            .GetValueOrDefault(customerOrderId) ?? EmptySupplySummary();
        var timeline = await BuildTimelineAsync(order.Order, cancellationToken);

        return new CustomerOrderOperationsDetailsDto(
            order.Order.Id,
            order.Order.OrderCode,
            order.Order.OrderDate,
            order.Order.RequiredDate,
            (ContractOrderStatus)(byte)order.Order.Status,
            order.Customer.Id,
            order.Customer.CustomerCode,
            order.Customer.NameAr,
            order.Customer.ContactInfo.Mobile,
            order.Order.Notes,
            supply,
            order.OpticalJobId,
            order.OpticalJobCode,
            lines,
            timeline);
    }

    private IQueryable<OrderRow> BuildBaseQuery(CustomerOrderOperationsQueryRequest request, DateOnly today)
    {
        var orders = db.Set<CustomerOrder>().AsNoTracking();
        var customers = db.Set<Customer>().AsNoTracking();
        var jobs = db.Set<OpticalJob>().AsNoTracking();
        var employees = db.Set<Employee>().AsNoTracking();
        var lines = db.Set<CustomerOrderLine>().AsNoTracking();
        var reservations = db.Set<StockReservation>().AsNoTracking();
        var variants = db.Set<ProductVariant>().AsNoTracking();
        var products = db.Set<Product>().AsNoTracking();

        var query =
            from order in orders
            join customer in customers on order.CustomerId equals customer.Id
            join job in jobs.Where(x => x.IsActive || x.Status == OAS.Domain.Sales.Enums.OpticalJobStatus.Delivered) on order.Id equals job.CustomerOrderId into jobJoin
            from job in jobJoin.DefaultIfEmpty()
            join technician in employees on job.AssignedTechnicianId equals (Guid?)technician.Id into technicianJoin
            from technician in technicianJoin.DefaultIfEmpty()
            let activeLines = lines.Where(x => x.CustomerOrderId == order.Id && x.IsActive)
            let totalLines = activeLines.Count()
            let availableLines = activeLines.Count(line =>
                line.ProductVariantId == null || line.WarehouseId == null ||
                reservations.Where(r =>
                    r.SourceModule == SalesSourceReferences.Module &&
                    r.SourceDocumentType == SalesSourceReferences.CustomerOrder &&
                    r.SourceDocumentId == order.Id &&
                    r.SourceLineId == line.Id &&
                    (r.Status == StockReservationStatus.Active || r.Status == StockReservationStatus.Consumed))
                    .Sum(r => (decimal?)r.Quantity) >= line.Quantity)
            let shortageLines = totalLines - availableLines
            select new OrderRow
            {
                CustomerOrderId = order.Id,
                OrderCode = order.OrderCode,
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                Status = order.Status,
                CustomerId = customer.Id,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.NameAr,
                Mobile = customer.ContactInfo.Mobile,
                TotalLines = totalLines,
                AvailableLines = availableLines,
                ShortageLines = shortageLines,
                RequiresProduction = activeLines.Any(x => x.RequiresProduction),
                OpticalJobId = job == null ? null : (Guid?)job.Id,
                OpticalJobCode = job == null ? null : job.JobCode,
                TechnicianId = job == null ? null : job.AssignedTechnicianId,
                TechnicianName = technician == null ? null : technician.FirstName + " " + technician.LastName,
                LastUpdatedAt = order.LastModifiedAtUtc ?? order.CreatedAtUtc
            };

        if (request.OrderDateFrom.HasValue)
            query = query.Where(x => x.OrderDate >= request.OrderDateFrom.Value);
        if (request.OrderDateTo.HasValue)
            query = query.Where(x => x.OrderDate <= request.OrderDateTo.Value);
        if (request.Status.HasValue)
            query = query.Where(x => (byte)x.Status == (byte)request.Status.Value);
        if (request.RequiredDate.HasValue)
            query = query.Where(x => x.RequiredDate == request.RequiredDate.Value);
        if (request.OverdueOnly)
            query = query.Where(x => x.RequiredDate != null && x.RequiredDate < today && x.Status != OAS.Domain.Sales.Enums.CustomerOrderStatus.Completed && x.Status != OAS.Domain.Sales.Enums.CustomerOrderStatus.Cancelled);
        if (request.RequiresProduction.HasValue)
            query = query.Where(x => x.RequiresProduction == request.RequiresProduction.Value);
        if (request.TechnicianId.HasValue)
            query = query.Where(x => x.TechnicianId == request.TechnicianId.Value);
        if (request.Availability.HasValue)
        {
            query = request.Availability.Value switch
            {
                CustomerOrderLineAvailabilityState.Shortage => query.Where(x => x.ShortageLines > 0),
                CustomerOrderLineAvailabilityState.Reserved => query.Where(x => x.ShortageLines == 0 && x.TotalLines > 0),
                CustomerOrderLineAvailabilityState.Available => query.Where(x => x.ShortageLines == 0),
                _ => query
            };
        }
        if (request.ProductType.HasValue || request.WarehouseId.HasValue)
        {
            var lineType = request.ProductType.HasValue ? (OAS.Domain.Sales.Enums.SalesLineType?)(byte)request.ProductType.Value : null;
            var warehouseId = request.WarehouseId;
            query = query.Where(x => lines.Any(l => l.CustomerOrderId == x.CustomerOrderId && l.IsActive &&
                                                     (!lineType.HasValue || l.LineType == lineType.Value) &&
                                                     (!warehouseId.HasValue || l.WarehouseId == warehouseId.Value)));
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.OrderCode.Contains(term) ||
                x.CustomerCode.Contains(term) ||
                x.CustomerName.Contains(term) ||
                (x.Mobile != null && x.Mobile.Contains(term)) ||
                lines.Any(l => l.CustomerOrderId == x.CustomerOrderId && l.IsActive && l.ProductVariantId != null &&
                    variants.Any(v => v.Id == l.ProductVariantId &&
                        (v.SKU.Contains(term) || products.Any(p => p.Id == v.ProductId && p.NameAr.Contains(term))))));
        }

        return query;
    }

    private static IQueryable<OrderRow> ApplySort(IQueryable<OrderRow> query, string? sortBy, SortDirection direction)
    {
        var key = sortBy?.Trim().ToLowerInvariant();
        var desc = direction == SortDirection.Descending;
        return key switch
        {
            "ordercode" => desc ? query.OrderByDescending(x => x.OrderCode) : query.OrderBy(x => x.OrderCode),
            "requireddate" => desc ? query.OrderByDescending(x => x.RequiredDate) : query.OrderBy(x => x.RequiredDate),
            "customercode" => desc ? query.OrderByDescending(x => x.CustomerCode) : query.OrderBy(x => x.CustomerCode),
            "customername" => desc ? query.OrderByDescending(x => x.CustomerName) : query.OrderBy(x => x.CustomerName),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "lastupdatedat" => desc ? query.OrderByDescending(x => x.LastUpdatedAt) : query.OrderBy(x => x.LastUpdatedAt),
            _ => desc ? query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.OrderCode)
                      : query.OrderBy(x => x.OrderDate).ThenBy(x => x.OrderCode)
        };
    }

    private async Task<Dictionary<Guid, string>> BuildItemsSummaryAsync(Guid[] orderIds, CancellationToken ct)
    {
        if (orderIds.Length == 0) return [];

        var lines = await db.Set<CustomerOrderLine>().AsNoTracking()
            .Where(x => orderIds.Contains(x.CustomerOrderId) && x.IsActive)
            .OrderBy(x => x.CustomerOrderId)
            .ThenBy(x => x.LineNumber)
            .Select(x => new
            {
                x.CustomerOrderId,
                x.LineType,
                x.ProductVariantId,
                x.DescriptionSnapshot
            })
            .ToListAsync(ct);

        var variantIds = lines
            .Where(x => x.ProductVariantId.HasValue)
            .Select(x => x.ProductVariantId!.Value)
            .Distinct()
            .ToArray();

        var variants = variantIds.Length == 0
            ? new Dictionary<Guid, ProductVariant>()
            : await db.Set<ProductVariant>().AsNoTracking()
                .Where(x => variantIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

        var productIds = variants.Values.Select(x => x.ProductId).Distinct().ToArray();
        var products = productIds.Length == 0
            ? new Dictionary<Guid, Product>()
            : await db.Set<Product>().AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

        return lines.GroupBy(x => x.CustomerOrderId).ToDictionary(
            group => group.Key,
            group =>
            {
                var names = group.Take(6).Select(line =>
                {
                    if (line.ProductVariantId.HasValue && variants.TryGetValue(line.ProductVariantId.Value, out var variant) &&
                        products.TryGetValue(variant.ProductId, out var product))
                        return product.NameAr;

                    return string.IsNullOrWhiteSpace(line.DescriptionSnapshot)
                        ? line.LineType.ToString()
                        : line.DescriptionSnapshot;
                });

                var summary = string.Join(" + ", names);
                return group.Count() > 6 ? $"{summary} +{group.Count() - 6}" : summary;
            });
    }

    private async Task<IReadOnlyList<CustomerOrderLineOperationsDto>> BuildLineDetailsAsync(Guid orderId, DateOnly today, CancellationToken ct)
    {
        var lines = await db.Set<CustomerOrderLine>().AsNoTracking()
            .Where(x => x.CustomerOrderId == orderId && x.IsActive)
            .OrderBy(x => x.LineNumber)
            .ToListAsync(ct);
        if (lines.Count == 0) return [];

        var lineIds = lines.Select(x => x.Id).ToArray();
        var variantIds = lines.Where(x => x.ProductVariantId.HasValue).Select(x => x.ProductVariantId!.Value).Distinct().ToArray();
        var warehouseIds = lines.Where(x => x.WarehouseId.HasValue).Select(x => x.WarehouseId!.Value).Distinct().ToArray();

        var variants = await db.Set<ProductVariant>().AsNoTracking().Where(x => variantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var productIds = variants.Values.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Set<Product>().AsNoTracking().Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var warehouses = await db.Set<Warehouse>().AsNoTracking().Where(x => warehouseIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var snapshots = await db.Set<CustomerOrderLineOpticalSnapshot>().AsNoTracking().Where(x => lineIds.Contains(x.CustomerOrderLineId) && x.IsActive).ToDictionaryAsync(x => x.CustomerOrderLineId, ct);
        var reservations = await db.Set<StockReservation>().AsNoTracking()
            .Where(x => x.SourceModule == SalesSourceReferences.Module && x.SourceDocumentType == SalesSourceReferences.CustomerOrder && x.SourceDocumentId == orderId && lineIds.Contains(x.SourceLineId))
            .ToListAsync(ct);

        var supply = await BuildLineSupplyAsync(lineIds, today, ct);

        return lines.Select(line =>
        {
            var activeReserved = reservations.Where(x => x.SourceLineId == line.Id && x.Status == StockReservationStatus.Active && x.IsActive).Sum(x => x.Quantity);
            var fulfilled = reservations.Where(x => x.SourceLineId == line.Id && (x.Status == StockReservationStatus.Active || x.Status == StockReservationStatus.Consumed)).Sum(x => x.Quantity);
            var requiresInventory = line.ProductVariantId.HasValue && line.WarehouseId.HasValue;
            var shortage = requiresInventory ? Math.Max(0m, line.Quantity - fulfilled) : 0m;
            var availability = !requiresInventory || shortage <= 0m
                ? (activeReserved > 0m ? CustomerOrderLineAvailabilityState.Reserved : CustomerOrderLineAvailabilityState.Available)
                : CustomerOrderLineAvailabilityState.Shortage;
            variants.TryGetValue(line.ProductVariantId ?? Guid.Empty, out var variant);
            Product? product = variant is null ? null : products.GetValueOrDefault(variant.ProductId);
            snapshots.TryGetValue(line.Id, out var snapshot);
            var lineSupply = supply.GetValueOrDefault(line.Id);

            return new CustomerOrderLineOperationsDto(
                line.Id,
                line.LineNumber,
                (ContractLineType)(byte)line.LineType,
                line.ProductVariantId,
                variant?.SKU,
                product?.NameAr ?? line.DescriptionSnapshot,
                snapshot is not null ? (ContractEyeSide?)(byte)snapshot.Eye : line.PrescriptionEye.HasValue ? (ContractEyeSide?)(byte)line.PrescriptionEye.Value : null,
                line.Quantity,
                line.WarehouseId,
                line.WarehouseId.HasValue ? warehouses.GetValueOrDefault(line.WarehouseId.Value)?.NameAr : null,
                availability,
                activeReserved,
                shortage,
                lineSupply?.Status ?? CustomerOrderSupplyStatus.None,
                lineSupply?.SupplierId,
                lineSupply?.SupplierName,
                lineSupply?.ExpectedDeliveryDate,
                line.RequiresProduction,
                snapshot is null ? null : MapSnapshot(snapshot));
        }).ToArray();
    }

    private async Task<Dictionary<Guid, CustomerOrderSupplySummaryDto>> BuildSupplySummariesAsync(Guid[] orderIds, DateOnly today, CancellationToken ct)
    {
        if (orderIds.Length == 0) return [];
        var lineIdsByOrder = await db.Set<CustomerOrderLine>().AsNoTracking()
            .Where(x => orderIds.Contains(x.CustomerOrderId) && x.IsActive)
            .Select(x => new { x.Id, x.CustomerOrderId })
            .ToListAsync(ct);
        var allLineIds = lineIdsByOrder.Select(x => x.Id).ToArray();
        var supplyByLine = await BuildLineSupplyAsync(allLineIds, today, ct);

        var result = new Dictionary<Guid, CustomerOrderSupplySummaryDto>();
        foreach (var orderId in orderIds)
        {
            var statuses = lineIdsByOrder.Where(x => x.CustomerOrderId == orderId)
                .Select(x => supplyByLine.GetValueOrDefault(x.Id)?.Status ?? CustomerOrderSupplyStatus.None)
                .Where(x => x != CustomerOrderSupplyStatus.None)
                .ToArray();
            result[orderId] = BuildSupplySummary(statuses);
        }
        return result;
    }

    private async Task<Dictionary<Guid, LineSupplyInfo>> BuildLineSupplyAsync(Guid[] customerOrderLineIds, DateOnly today, CancellationToken ct)
    {
        if (customerOrderLineIds.Length == 0) return [];
        var requestRows = await (
            from line in db.Set<PurchaseRequestLine>().AsNoTracking()
            join header in db.Set<PurchaseRequest>().AsNoTracking() on line.PurchaseRequestId equals header.Id
            where line.CustomerOrderLineId != null && customerOrderLineIds.Contains(line.CustomerOrderLineId.Value)
                  && header.RequestType == PurchaseRequestType.CustomerDemand && DemandStatuses.Contains(header.Status)
            select new
            {
                line.Id,
                CustomerOrderLineId = line.CustomerOrderLineId!.Value,
                line.RequestedQuantity,
                line.PreferredSupplierId,
                line.ScheduledOrderAtUtc
            }).ToListAsync(ct);
        if (requestRows.Count == 0) return [];

        var requestLineIds = requestRows.Select(x => x.Id).ToArray();
        var sourceRows = await (
            from source in db.Set<PurchaseOrderLineSource>().AsNoTracking()
            join poLine in db.Set<PurchaseOrderLine>().AsNoTracking() on source.PurchaseOrderLineId equals poLine.Id
            join po in db.Set<PurchaseOrder>().AsNoTracking() on poLine.PurchaseOrderId equals po.Id
            join supplier in db.Set<Supplier>().AsNoTracking() on po.SupplierId equals supplier.Id
            where requestLineIds.Contains(source.PurchaseRequestLineId)
                  && po.Status != PurchaseOrderStatus.Cancelled && po.Status != PurchaseOrderStatus.Rejected
            select new
            {
                source.PurchaseRequestLineId,
                source.AllocatedQuantity,
                PurchaseOrderLineId = poLine.Id,
                po.SupplierId,
                SupplierName = supplier.NameAr,
                po.Status,
                po.SentAt,
                ExpectedDeliveryDate = poLine.ExpectedDeliveryDate ?? po.ExpectedDeliveryDate,
                po.OrderDate
            }).ToListAsync(ct);

        var poLineIds = sourceRows.Select(x => x.PurchaseOrderLineId).Distinct().ToArray();
        var receiptRows = poLineIds.Length == 0 ? [] : await (
            from rl in db.Set<PurchaseReceiptLine>().AsNoTracking()
            join r in db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals r.Id
            where poLineIds.Contains(rl.PurchaseOrderLineId) && r.Status == PurchaseReceiptStatus.Posted
            select new { rl.PurchaseOrderLineId, rl.BaseAcceptedQuantity }).ToListAsync(ct);

        var supplierIds = requestRows.Where(x => x.PreferredSupplierId.HasValue).Select(x => x.PreferredSupplierId!.Value).Distinct().ToArray();
        var preferredNames = await db.Set<Supplier>().AsNoTracking().Where(x => supplierIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.NameAr, ct);

        var result = new Dictionary<Guid, LineSupplyInfo>();
        foreach (var group in requestRows.GroupBy(x => x.CustomerOrderLineId))
        {
            // One customer order line can have multiple demand rows after re-sourcing. Derive the most advanced state.
            var statuses = new List<LineSupplyInfo>();
            foreach (var requestLine in group)
            {
                var sources = sourceRows.Where(x => x.PurchaseRequestLineId == requestLine.Id).ToArray();
                var received = sources.Sum(source =>
                {
                    var totalAllocatedOnPoLine = sourceRows
                        .Where(x => x.PurchaseOrderLineId == source.PurchaseOrderLineId)
                        .Sum(x => x.AllocatedQuantity);
                    var receivedBaseOnPoLine = receiptRows
                        .Where(x => x.PurchaseOrderLineId == source.PurchaseOrderLineId)
                        .Sum(x => x.BaseAcceptedQuantity);
                    if (totalAllocatedOnPoLine <= 0m || receivedBaseOnPoLine <= 0m)
                        return 0m;
                    var prorated = receivedBaseOnPoLine * source.AllocatedQuantity / totalAllocatedOnPoLine;
                    return Math.Min(source.AllocatedQuantity, prorated);
                });
                var latest = sources.OrderByDescending(x => x.OrderDate).FirstOrDefault();
                var ordered = sources.Any(x => OrderedStatuses.Contains(x.Status));
                CustomerOrderSupplyStatus status;
                if (received >= requestLine.RequestedQuantity) status = CustomerOrderSupplyStatus.Received;
                else if (received > 0m) status = CustomerOrderSupplyStatus.PartiallyReceived;
                else if (ordered && latest?.ExpectedDeliveryDate is { } expected && expected < today) status = CustomerOrderSupplyStatus.Overdue;
                else if (ordered && latest?.ExpectedDeliveryDate == today) status = CustomerOrderSupplyStatus.DueToday;
                else if (ordered) status = CustomerOrderSupplyStatus.Ordered;
                else if (requestLine.ScheduledOrderAtUtc.HasValue) status = CustomerOrderSupplyStatus.Scheduled;
                else status = CustomerOrderSupplyStatus.NotOrdered;

                var supplierId = latest?.SupplierId ?? requestLine.PreferredSupplierId;
                var supplierName = latest?.SupplierName ?? (supplierId.HasValue ? preferredNames.GetValueOrDefault(supplierId.Value) : null);
                statuses.Add(new LineSupplyInfo(status, supplierId, supplierName, latest?.ExpectedDeliveryDate));
            }
            result[group.Key] = statuses.OrderByDescending(x => SupplyRank(x.Status)).First();
        }
        return result;
    }

    private async Task<IReadOnlyList<CustomerOrderOperationalTimelineItemDto>> BuildTimelineAsync(CustomerOrder order, CancellationToken ct)
    {
        var events = new List<CustomerOrderOperationalTimelineItemDto>();
        if (order.ConfirmedAtUtc.HasValue)
            events.Add(new("OrderConfirmed", "تم تأكيد طلب العميل", order.ConfirmedAtUtc.Value, order.ConfirmedBy, order.Id, order.OrderCode));

        var reservations = await db.Set<StockReservation>().AsNoTracking()
            .Where(x => x.SourceModule == SalesSourceReferences.Module && x.SourceDocumentType == SalesSourceReferences.CustomerOrder && x.SourceDocumentId == order.Id)
            .OrderBy(x => x.ReservedAtUtc).ToListAsync(ct);
        if (reservations.Count > 0)
            events.Add(new("StockReserved", "تم حجز المخزون", reservations.Min(x => x.ReservedAtUtc), reservations.OrderBy(x => x.ReservedAtUtc).First().CreatedBy, null, null));

        var demands = await db.Set<PurchaseRequest>().AsNoTracking()
            .Where(x => x.RequestType == PurchaseRequestType.CustomerDemand && x.CustomerOrderId == order.Id)
            .OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        foreach (var demand in demands)
            events.Add(new("ShortageCreated", $"تم إنشاء نقص {demand.RequestCode}", demand.CreatedAtUtc, demand.CreatedBy, demand.Id, demand.RequestCode));

        var demandIds = demands.Select(x => x.Id).ToArray();
        if (demandIds.Length > 0)
        {
            var requestLineIds = await db.Set<PurchaseRequestLine>().AsNoTracking().Where(x => demandIds.Contains(x.PurchaseRequestId)).Select(x => x.Id).ToArrayAsync(ct);
            var poRows = await (
                from source in db.Set<PurchaseOrderLineSource>().AsNoTracking()
                join poLine in db.Set<PurchaseOrderLine>().AsNoTracking() on source.PurchaseOrderLineId equals poLine.Id
                join po in db.Set<PurchaseOrder>().AsNoTracking() on poLine.PurchaseOrderId equals po.Id
                where requestLineIds.Contains(source.PurchaseRequestLineId)
                select po).Distinct().ToListAsync(ct);
            foreach (var po in poRows.Where(x => x.SentAt.HasValue))
                events.Add(new("PurchaseOrderSent", $"تم إرسال أمر الشراء {po.PurchaseOrderCode}", po.SentAt!.Value, po.SentBy, po.Id, po.PurchaseOrderCode));

            var poIds = poRows.Select(x => x.Id).ToArray();
            var receiptRows = await db.Set<PurchaseReceipt>().AsNoTracking().Where(x => poIds.Contains(x.PurchaseOrderId) && x.Status == PurchaseReceiptStatus.Posted).ToListAsync(ct);
            foreach (var receipt in receiptRows.Where(x => x.PostedAt.HasValue))
                events.Add(new("Received", $"تم استلام {receipt.ReceiptCode}", receipt.PostedAt!.Value, receipt.PostedBy, receipt.Id, receipt.ReceiptCode));
        }

        var job = await db.Set<OpticalJob>().AsNoTracking()
            .Where(x => x.CustomerOrderId == order.Id)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (job is not null)
        {
            events.Add(new("OpticalJobCreated", $"تم إنشاء أمر المعمل {job.JobCode}", job.CreatedAtUtc, job.CreatedBy, job.Id, job.JobCode));
            if (job.StartedAtUtc.HasValue)
                events.Add(new("ProductionStarted", "بدأ تجهيز الطلب في المعمل", job.StartedAtUtc.Value, job.LastModifiedBy, job.Id, job.JobCode));
            if (job.CompletedAtUtc.HasValue)
                events.Add(new("ReadyForDelivery", "أصبح الطلب جاهزًا للتسليم", job.CompletedAtUtc.Value, job.LastModifiedBy, job.Id, job.JobCode));
        }
        if (order.Status == OAS.Domain.Sales.Enums.CustomerOrderStatus.Completed && order.LastModifiedAtUtc.HasValue)
            events.Add(new("Delivered", "تم تسليم الطلب للعميل", order.LastModifiedAtUtc.Value, order.LastModifiedBy, order.Id, order.OrderCode));

        return events.OrderBy(x => x.OccurredAt).ToArray();
    }

    private static CustomerOrderSupplySummaryDto BuildSupplySummary(IEnumerable<CustomerOrderSupplyStatus> statuses)
    {
        var values = statuses.ToArray();
        if (values.Length == 0) return EmptySupplySummary();
        var notOrdered = values.Count(x => x == CustomerOrderSupplyStatus.NotOrdered);
        var scheduled = values.Count(x => x == CustomerOrderSupplyStatus.Scheduled);
        var ordered = values.Count(x => x == CustomerOrderSupplyStatus.Ordered);
        var due = values.Count(x => x == CustomerOrderSupplyStatus.DueToday);
        var overdue = values.Count(x => x == CustomerOrderSupplyStatus.Overdue);
        var partial = values.Count(x => x == CustomerOrderSupplyStatus.PartiallyReceived);
        var received = values.Count(x => x == CustomerOrderSupplyStatus.Received);
        var text = overdue > 0 ? $"متأخر: {overdue}"
            : due > 0 ? $"تصل اليوم: {due}"
            : partial > 0 ? $"مستلم جزئيًا: {partial}"
            : ordered > 0 ? $"تم الطلب: {ordered}"
            : scheduled > 0 ? $"مجدول: {scheduled}"
            : notOrdered > 0 ? $"لم يطلب: {notOrdered}"
            : received > 0 ? "مستلم بالكامل"
            : null;
        return new(true, notOrdered, scheduled, ordered, due, overdue, partial, received, text);
    }

    private static CustomerOrderSupplySummaryDto EmptySupplySummary() =>
        new(false, 0, 0, 0, 0, 0, 0, 0, null);

    private static int SupplyRank(CustomerOrderSupplyStatus status) => status switch
    {
        CustomerOrderSupplyStatus.Overdue => 8,
        CustomerOrderSupplyStatus.DueToday => 7,
        CustomerOrderSupplyStatus.PartiallyReceived => 6,
        CustomerOrderSupplyStatus.Ordered => 5,
        CustomerOrderSupplyStatus.Scheduled => 4,
        CustomerOrderSupplyStatus.NotOrdered => 3,
        CustomerOrderSupplyStatus.Received => 2,
        _ => 0
    };

    private static CustomerOrderLineOpticalSnapshotDto MapSnapshot(CustomerOrderLineOpticalSnapshot x) => new(
        x.Id,
        x.CustomerOrderLineId,
        (ContractMeasurementSource)(byte)x.MeasurementSource,
        x.PrescriptionRevisionId,
        (ContractEyeSide)(byte)x.Eye,
        x.SPH,
        x.CYL,
        x.Axis,
        x.ADD,
        x.Prism,
        x.PrismBase.HasValue ? (ContractPrismBase?)(byte)x.PrismBase.Value : null,
        x.PD,
        x.MonocularPD,
        x.VA,
        x.FittingHeight,
        x.LensTypeSnapshot,
        x.MaterialSnapshot,
        x.CoatingSnapshot,
        x.RefractiveIndexSnapshot,
        x.IsActive,
        Convert.ToBase64String(x.RowVersion),
        x.CreatedAtUtc,
        x.CreatedBy,
        x.LastModifiedAtUtc,
        x.LastModifiedBy);

    private sealed class OrderRow
    {
        public Guid CustomerOrderId { get; init; }
        public string OrderCode { get; init; } = string.Empty;
        public DateOnly OrderDate { get; init; }
        public DateOnly? RequiredDate { get; init; }
        public OAS.Domain.Sales.Enums.CustomerOrderStatus Status { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerCode { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string? Mobile { get; init; }
        public int TotalLines { get; init; }
        public int AvailableLines { get; init; }
        public int ShortageLines { get; init; }
        public bool RequiresProduction { get; init; }
        public Guid? OpticalJobId { get; init; }
        public string? OpticalJobCode { get; init; }
        public Guid? TechnicianId { get; init; }
        public string? TechnicianName { get; init; }
        public DateTimeOffset LastUpdatedAt { get; init; }
    }

    private sealed record LineSupplyInfo(
        CustomerOrderSupplyStatus Status,
        Guid? SupplierId,
        string? SupplierName,
        DateOnly? ExpectedDeliveryDate);
}
