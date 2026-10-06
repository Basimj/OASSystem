using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using ContractPurchaseOrderStatus = OAS.Contracts.Purchasing.Enums.PurchaseOrderStatus;
using ContractSalesLineType = OAS.Contracts.Sales.Enums.SalesLineType;
using ContractEyeSide = OAS.Contracts.Sales.Enums.EyeSide;
using ContractMeasurementSource = OAS.Contracts.Sales.Enums.OpticalMeasurementSource;
using ContractPrismBase = OAS.Contracts.Sales.Enums.PrismBaseDirection;

namespace OAS.Infrastructure.Purchasing.Persistence.Queries;

/// <summary>
/// Operational read model over PurchaseRequest -> PO source -> PO -> Receipt with
/// the originating CustomerOrder/line and immutable optical snapshot. No tracking
/// table is introduced; the source documents remain the system of record.
/// </summary>
public sealed class CustomerDemandTrackingQueryService(
    OasDbContext db,
    TimeProvider timeProvider) : ICustomerDemandTrackingQueryService
{
    private static readonly PurchaseRequestStatus[] OpenTrackingStatuses =
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

    public async Task<PagedResult<CustomerDemandTrackingItemDto>> GetPageAsync(
        CustomerDemandTrackingQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        var page = request.ToPageRequest().Normalize();
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var query = BuildQuery(request, today);

        query = ApplyTrackingStatusFilter(query, request.Status, today);
        query = ApplySort(query, request.SortBy, request.SortDirection);

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDemandTrackingItemDto>
        {
            Items = rows.Select(x => MapItem(x, includeCosts, today)).ToArray(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = total
        };
    }

    public async Task<CustomerDemandTrackingSummaryDto> GetSummaryAsync(
        CustomerDemandTrackingQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        // Summary is intentionally independent from the selected status tab so the cards
        // remain useful as navigation counters while all other filters stay applied.
        var baseRequest = request with { Status = null };
        var query = BuildQuery(baseRequest, today);

        var total = await query.CountAsync(cancellationToken);
        var received = await query.CountAsync(x => x.AcceptedReceivedQuantity >= x.RequestedQuantity, cancellationToken);
        var partial = await query.CountAsync(x => x.AcceptedReceivedQuantity > 0m && x.AcceptedReceivedQuantity < x.RequestedQuantity, cancellationToken);
        var overdue = await query.CountAsync(x =>
            x.AcceptedReceivedQuantity < x.RequestedQuantity && x.HasOrderedPurchaseOrder &&
            x.EffectiveExpectedDeliveryDate != null && x.EffectiveExpectedDeliveryDate < today, cancellationToken);
        var dueToday = await query.CountAsync(x =>
            x.AcceptedReceivedQuantity < x.RequestedQuantity && x.HasOrderedPurchaseOrder &&
            x.EffectiveExpectedDeliveryDate == today, cancellationToken);
        var ordered = await query.CountAsync(x =>
            x.AcceptedReceivedQuantity < x.RequestedQuantity && x.HasOrderedPurchaseOrder, cancellationToken);
        var scheduled = await query.CountAsync(x =>
            x.AcceptedReceivedQuantity < x.RequestedQuantity && !x.HasOrderedPurchaseOrder && x.ScheduledOrderAtUtc != null, cancellationToken);
        var newRows = await query.CountAsync(x =>
            x.AcceptedReceivedQuantity < x.RequestedQuantity && x.AllocatedToPurchaseOrderQuantity == 0m &&
            x.PreferredSupplierId == null &&
            (x.RequestStatus == PurchaseRequestStatus.Draft || x.RequestStatus == PurchaseRequestStatus.PendingApproval), cancellationToken);
        var awaitingSupplier = await query.CountAsync(x =>
            x.AcceptedReceivedQuantity < x.RequestedQuantity && !x.HasOrderedPurchaseOrder &&
            x.PreferredSupplierId == null && x.ActualSupplierId == null, cancellationToken);

        return new CustomerDemandTrackingSummaryDto(
            newRows,
            awaitingSupplier,
            scheduled,
            ordered,
            dueToday,
            overdue,
            partial,
            received,
            total);
    }

    public async Task<CustomerDemandTrackingDetailsDto> GetDetailsAsync(
        Guid purchaseRequestLineId,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var row = await BuildQuery(new CustomerDemandTrackingQueryRequest(), today)
            .SingleOrDefaultAsync(x => x.PurchaseRequestLineId == purchaseRequestLineId, cancellationToken)
            ?? throw new NotFoundException("PurchaseRequestLine", purchaseRequestLineId);

        return new CustomerDemandTrackingDetailsDto(
            MapItem(row, includeCosts, today),
            row.CustomerOrderRequiredDate,
            row.VariantDescription,
            row.Notes,
            row.MeasurementSource.HasValue ? (ContractMeasurementSource?)(byte)row.MeasurementSource.Value : null,
            row.Eye.HasValue ? (ContractEyeSide?)(byte)row.Eye.Value : null,
            row.SPH,
            row.CYL,
            row.Axis,
            row.ADD,
            row.Prism,
            row.PrismBase.HasValue ? (ContractPrismBase?)(byte)row.PrismBase.Value : null,
            row.PD,
            row.MonocularPD,
            row.FittingHeight,
            row.LensType,
            row.Material,
            row.Coating,
            row.PurchaseOrderStatus.HasValue ? (ContractPurchaseOrderStatus?)(byte)row.PurchaseOrderStatus.Value : null,
            row.OrderedQuantity,
            row.AcceptedReceivedQuantity,
            Math.Max(0m, row.RequestedQuantity - row.AcceptedReceivedQuantity),
            row.LastReceiptDate,
            includeCosts ? row.UnitPrice : null,
            includeCosts ? row.ActualUnitCost : null,
            Convert.ToBase64String(row.RowVersion));
    }

    public async Task<PagedResult<SupplierPurchaseHistoryItemDto>> GetSupplierHistoryAsync(
        SupplierPurchaseHistoryQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        var page = request.ToPageRequest().Normalize();
        var poLines = db.Set<PurchaseOrderLine>().AsNoTracking();
        var orders = db.Set<PurchaseOrder>().AsNoTracking();
        var suppliers = db.Set<Supplier>().AsNoTracking();
        var variants = db.Set<ProductVariant>().AsNoTracking();
        var products = db.Set<Product>().AsNoTracking();
        var receiptLines = db.Set<PurchaseReceiptLine>().AsNoTracking();
        var receipts = db.Set<PurchaseReceipt>().AsNoTracking();

        var query =
            from line in poLines
            join order in orders on line.PurchaseOrderId equals order.Id
            join supplier in suppliers on order.SupplierId equals supplier.Id
            join variant in variants on line.ProductVariantId equals variant.Id
            join product in products on variant.ProductId equals product.Id
            where order.SupplierId == request.SupplierId
                  && order.Status != PurchaseOrderStatus.Cancelled
                  && order.Status != PurchaseOrderStatus.Rejected
            let acceptedQuantity = (
                from rl in receiptLines
                join r in receipts on rl.PurchaseReceiptId equals r.Id
                where rl.PurchaseOrderLineId == line.Id && r.Status == PurchaseReceiptStatus.Posted
                select (decimal?)rl.AcceptedQuantity).Sum() ?? 0m
            let lastReceipt = (
                from rl in receiptLines
                join r in receipts on rl.PurchaseReceiptId equals r.Id
                where rl.PurchaseOrderLineId == line.Id && r.Status == PurchaseReceiptStatus.Posted
                orderby r.ReceiptDate descending, r.PostedAt descending
                select new
                {
                    ReceiptId = (Guid?)r.Id,
                    r.ReceiptCode,
                    ReceiptDate = (DateOnly?)r.ReceiptDate,
                    ActualUnitCost = (decimal?)rl.ActualUnitCost
                }).FirstOrDefault()
            select new
            {
                order.SupplierId,
                SupplierName = supplier.NameAr,
                line.ProductVariantId,
                ProductCode = variant.SKU,
                ProductName = product.NameAr,
                PurchaseOrderId = order.Id,
                order.PurchaseOrderCode,
                order.OrderDate,
                EffectiveExpectedDeliveryDate = line.ExpectedDeliveryDate ?? order.ExpectedDeliveryDate,
                ReceiptId = lastReceipt == null ? null : lastReceipt.ReceiptId,
                ReceiptCode = lastReceipt == null ? null : lastReceipt.ReceiptCode,
                ReceiptDate = lastReceipt == null ? null : lastReceipt.ReceiptDate,
                line.OrderedQuantity,
                AcceptedQuantity = acceptedQuantity,
                line.UnitPrice,
                ActualUnitCost = lastReceipt == null ? null : lastReceipt.ActualUnitCost
            };

        if (request.ProductVariantId.HasValue)
            query = query.Where(x => x.ProductVariantId == request.ProductVariantId.Value);
        if (request.FromDate.HasValue)
            query = query.Where(x => x.OrderDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(x => x.OrderDate <= request.ToDate.Value);

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.PurchaseOrderCode)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierPurchaseHistoryItemDto>
        {
            Items = rows.Select(x => new SupplierPurchaseHistoryItemDto(
                x.SupplierId,
                x.SupplierName,
                x.ProductVariantId,
                x.ProductCode,
                x.ProductName,
                x.PurchaseOrderId,
                x.PurchaseOrderCode,
                x.OrderDate,
                x.EffectiveExpectedDeliveryDate,
                x.ReceiptId,
                x.ReceiptCode,
                x.ReceiptDate,
                x.OrderedQuantity,
                x.AcceptedQuantity,
                includeCosts ? (decimal?)x.UnitPrice : null,
                includeCosts ? x.ActualUnitCost : null)).ToArray(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = total
        };
    }

    private IQueryable<DemandRow> BuildQuery(CustomerDemandTrackingQueryRequest request, DateOnly today)
    {
        var requestHeaders = db.Set<PurchaseRequest>().AsNoTracking();
        var requestLines = db.Set<PurchaseRequestLine>().AsNoTracking();
        var customerOrderLines = db.Set<CustomerOrderLine>().AsNoTracking();
        var customerOrders = db.Set<CustomerOrder>().AsNoTracking();
        var customers = db.Set<Customer>().AsNoTracking();
        var variants = db.Set<ProductVariant>().AsNoTracking();
        var products = db.Set<Product>().AsNoTracking();
        var warehouses = db.Set<Warehouse>().AsNoTracking();
        var suppliers = db.Set<Supplier>().AsNoTracking();
        var snapshots = db.Set<CustomerOrderLineOpticalSnapshot>().AsNoTracking();
        var sources = db.Set<PurchaseOrderLineSource>().AsNoTracking();
        var purchaseOrderLines = db.Set<PurchaseOrderLine>().AsNoTracking();
        var purchaseOrders = db.Set<PurchaseOrder>().AsNoTracking();
        var receiptLines = db.Set<PurchaseReceiptLine>().AsNoTracking();
        var receipts = db.Set<PurchaseReceipt>().AsNoTracking();

        var query =
            from requestLine in requestLines
            join header in requestHeaders on requestLine.PurchaseRequestId equals header.Id
            join orderLine in customerOrderLines on requestLine.CustomerOrderLineId equals (Guid?)orderLine.Id
            join order in customerOrders on orderLine.CustomerOrderId equals order.Id
            join customer in customers on order.CustomerId equals customer.Id
            join variant in variants on requestLine.ProductVariantId equals variant.Id
            join product in products on variant.ProductId equals product.Id
            join warehouse in warehouses on header.WarehouseId equals warehouse.Id
            join preferredSupplier in suppliers on requestLine.PreferredSupplierId equals (Guid?)preferredSupplier.Id into preferredSupplierJoin
            from preferredSupplier in preferredSupplierJoin.DefaultIfEmpty()
            join snapshot in snapshots on orderLine.Id equals snapshot.CustomerOrderLineId into snapshotJoin
            from snapshot in snapshotJoin.DefaultIfEmpty()
            where header.RequestType == PurchaseRequestType.CustomerDemand
                  && OpenTrackingStatuses.Contains(header.Status)
                  && requestLine.CustomerOrderLineId != null
            let allocated =
                (from source in sources
                 join poLine in purchaseOrderLines on source.PurchaseOrderLineId equals poLine.Id
                 join po in purchaseOrders on poLine.PurchaseOrderId equals po.Id
                 where source.PurchaseRequestLineId == requestLine.Id
                       && po.Status != PurchaseOrderStatus.Cancelled
                       && po.Status != PurchaseOrderStatus.Rejected
                 select (decimal?)source.AllocatedQuantity).Sum() ?? 0m
            let accepted =
                (from source in sources
                 join poLine in purchaseOrderLines on source.PurchaseOrderLineId equals poLine.Id
                 where source.PurchaseRequestLineId == requestLine.Id
                 let totalAllocatedOnPoLine = sources
                     .Where(s => s.PurchaseOrderLineId == source.PurchaseOrderLineId)
                     .Sum(s => (decimal?)s.AllocatedQuantity) ?? 0m
                 let receivedBaseOnPoLine =
                     (from receiptLine in receiptLines
                      join receipt in receipts on receiptLine.PurchaseReceiptId equals receipt.Id
                      where receiptLine.PurchaseOrderLineId == poLine.Id && receipt.Status == PurchaseReceiptStatus.Posted
                      select (decimal?)receiptLine.BaseAcceptedQuantity).Sum() ?? 0m
                 let prorated = totalAllocatedOnPoLine <= 0m
                     ? 0m
                     : receivedBaseOnPoLine * source.AllocatedQuantity / totalAllocatedOnPoLine
                 select (decimal?)(prorated > source.AllocatedQuantity ? source.AllocatedQuantity : prorated)).Sum() ?? 0m
            let latestPurchase =
                (from source in sources
                 join poLine in purchaseOrderLines on source.PurchaseOrderLineId equals poLine.Id
                 join po in purchaseOrders on poLine.PurchaseOrderId equals po.Id
                 where source.PurchaseRequestLineId == requestLine.Id
                       && po.Status != PurchaseOrderStatus.Cancelled
                       && po.Status != PurchaseOrderStatus.Rejected
                 orderby po.OrderDate descending, po.PurchaseOrderCode descending
                 select new
                 {
                     PurchaseOrderId = (Guid?)po.Id,
                     po.PurchaseOrderCode,
                     ActualSupplierId = (Guid?)po.SupplierId,
                     po.SentAt,
                     EffectiveExpectedDeliveryDate = poLine.ExpectedDeliveryDate ?? po.ExpectedDeliveryDate,
                     PurchaseOrderStatus = (PurchaseOrderStatus?)po.Status,
                     OrderedQuantity = (decimal?)poLine.OrderedQuantity,
                     UnitPrice = (decimal?)poLine.UnitPrice
                 }).FirstOrDefault()
            let actualSupplierName =
                (from source in sources
                 join poLine in purchaseOrderLines on source.PurchaseOrderLineId equals poLine.Id
                 join po in purchaseOrders on poLine.PurchaseOrderId equals po.Id
                 join supplier in suppliers on po.SupplierId equals supplier.Id
                 where source.PurchaseRequestLineId == requestLine.Id
                       && po.Status != PurchaseOrderStatus.Cancelled
                       && po.Status != PurchaseOrderStatus.Rejected
                 orderby po.OrderDate descending, po.PurchaseOrderCode descending
                 select supplier.NameAr).FirstOrDefault()
            let hasOrderedPurchaseOrder =
                (from source in sources
                 join poLine in purchaseOrderLines on source.PurchaseOrderLineId equals poLine.Id
                 join po in purchaseOrders on poLine.PurchaseOrderId equals po.Id
                 where source.PurchaseRequestLineId == requestLine.Id && OrderedStatuses.Contains(po.Status)
                 select source.Id).Any()
            let lastReceipt =
                (from source in sources
                 join poLine in purchaseOrderLines on source.PurchaseOrderLineId equals poLine.Id
                 join receiptLine in receiptLines on poLine.Id equals receiptLine.PurchaseOrderLineId
                 join receipt in receipts on receiptLine.PurchaseReceiptId equals receipt.Id
                 where source.PurchaseRequestLineId == requestLine.Id && receipt.Status != PurchaseReceiptStatus.Cancelled
                 orderby receipt.ReceiptDate descending, receipt.PostedAt descending
                 select new
                 {
                     ReceiptDate = (DateOnly?)receipt.ReceiptDate,
                     ReceiptStatus = (PurchaseReceiptStatus?)receipt.Status,
                     ActualUnitCost = (decimal?)receiptLine.ActualUnitCost
                 }).FirstOrDefault()
            select new DemandRow
            {
                PurchaseRequestLineId = requestLine.Id,
                PurchaseRequestId = header.Id,
                RequestCode = header.RequestCode,
                RequestDate = header.RequestDate,
                RequestStatus = header.Status,
                ScheduledOrderAtUtc = requestLine.ScheduledOrderAtUtc,
                CustomerOrderId = order.Id,
                CustomerOrderCode = order.OrderCode,
                CustomerOrderRequiredDate = order.RequiredDate,
                CustomerOrderLineId = orderLine.Id,
                CustomerId = customer.Id,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.NameAr,
                Mobile = customer.ContactInfo.Mobile,
                LineType = orderLine.LineType,
                ProductVariantId = variant.Id,
                ProductCode = variant.SKU,
                ProductName = product.NameAr,
                VariantDescription = variant.VariantName,
                Eye = snapshot == null ? orderLine.PrescriptionEye : snapshot.Eye,
                MeasurementSource = snapshot == null ? null : snapshot.MeasurementSource,
                SPH = snapshot == null ? null : snapshot.SPH,
                CYL = snapshot == null ? null : snapshot.CYL,
                Axis = snapshot == null ? null : snapshot.Axis,
                ADD = snapshot == null ? null : snapshot.ADD,
                Prism = snapshot == null ? null : snapshot.Prism,
                PrismBase = snapshot == null ? null : snapshot.PrismBase,
                PD = snapshot == null ? null : snapshot.PD,
                MonocularPD = snapshot == null ? null : snapshot.MonocularPD,
                FittingHeight = snapshot == null ? null : snapshot.FittingHeight,
                LensType = snapshot == null ? null : snapshot.LensTypeSnapshot,
                Material = snapshot == null ? null : snapshot.MaterialSnapshot,
                Coating = snapshot == null ? null : snapshot.CoatingSnapshot,
                RequestedQuantity = requestLine.RequestedQuantity,
                AllocatedToPurchaseOrderQuantity = allocated,
                AcceptedReceivedQuantity = accepted,
                WarehouseId = header.WarehouseId,
                WarehouseName = warehouse.NameAr,
                PreferredSupplierId = requestLine.PreferredSupplierId,
                PreferredSupplierName = preferredSupplier == null ? null : preferredSupplier.NameAr,
                ActualSupplierId = latestPurchase == null ? null : latestPurchase.ActualSupplierId,
                ActualSupplierName = actualSupplierName,
                PurchaseOrderId = latestPurchase == null ? null : latestPurchase.PurchaseOrderId,
                PurchaseOrderCode = latestPurchase == null ? null : latestPurchase.PurchaseOrderCode,
                SentAt = latestPurchase == null ? null : latestPurchase.SentAt,
                EffectiveExpectedDeliveryDate = latestPurchase == null ? null : latestPurchase.EffectiveExpectedDeliveryDate,
                PurchaseOrderStatus = latestPurchase == null ? null : latestPurchase.PurchaseOrderStatus,
                OrderedQuantity = latestPurchase == null ? 0m : (latestPurchase.OrderedQuantity ?? 0m),
                UnitPrice = latestPurchase == null ? null : latestPurchase.UnitPrice,
                LastReceiptDate = lastReceipt == null ? null : lastReceipt.ReceiptDate,
                ReceiptStatus = lastReceipt == null ? null : lastReceipt.ReceiptStatus,
                ActualUnitCost = lastReceipt == null ? null : lastReceipt.ActualUnitCost,
                RequiresProduction = orderLine.RequiresProduction,
                Notes = requestLine.Notes,
                HasOrderedPurchaseOrder = hasOrderedPurchaseOrder,
                RowVersion = requestLine.RowVersion
            };

        if (request.RequestDateFrom.HasValue)
            query = query.Where(x => x.RequestDate >= request.RequestDateFrom.Value);
        if (request.RequestDateTo.HasValue)
            query = query.Where(x => x.RequestDate <= request.RequestDateTo.Value);
        if (request.WarehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == request.WarehouseId.Value);
        if (request.CustomerOrderId.HasValue)
            query = query.Where(x => x.CustomerOrderId == request.CustomerOrderId.Value);
        if (request.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        if (request.ProductType.HasValue)
            query = query.Where(x => (byte)x.LineType == (byte)request.ProductType.Value);
        if (request.Eye.HasValue)
            query = query.Where(x => x.Eye != null && (byte)x.Eye.Value == (byte)request.Eye.Value);
        if (request.RequiresProduction.HasValue)
            query = query.Where(x => x.RequiresProduction == request.RequiresProduction.Value);
        if (request.SupplierId.HasValue)
            query = query.Where(x => x.PreferredSupplierId == request.SupplierId.Value || x.ActualSupplierId == request.SupplierId.Value);
        if (request.ExpectedDeliveryDate.HasValue)
            query = query.Where(x => x.EffectiveExpectedDeliveryDate == request.ExpectedDeliveryDate.Value);
        if (request.PurchaseOrderStatus.HasValue)
            query = query.Where(x => x.PurchaseOrderStatus != null && (byte)x.PurchaseOrderStatus.Value == (byte)request.PurchaseOrderStatus.Value);
        if (request.ReceiptStatus.HasValue)
            query = query.Where(x => x.ReceiptStatus != null && (byte)x.ReceiptStatus.Value == (byte)request.ReceiptStatus.Value);
        if (request.OverdueOnly)
            query = query.Where(x => x.AcceptedReceivedQuantity < x.RequestedQuantity && x.HasOrderedPurchaseOrder &&
                                     x.EffectiveExpectedDeliveryDate != null && x.EffectiveExpectedDeliveryDate < today);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.RequestCode.Contains(term) ||
                x.CustomerOrderCode.Contains(term) ||
                x.CustomerCode.Contains(term) ||
                x.CustomerName.Contains(term) ||
                (x.Mobile != null && x.Mobile.Contains(term)) ||
                x.ProductCode.Contains(term) ||
                x.ProductName.Contains(term));
        }

        // Materialized field is defined after the subqueries to keep status filters SQL-translatable.
        return query.Select(x => new DemandRow
        {
            PurchaseRequestLineId = x.PurchaseRequestLineId,
            PurchaseRequestId = x.PurchaseRequestId,
            RequestCode = x.RequestCode,
            RequestDate = x.RequestDate,
            RequestStatus = x.RequestStatus,
            ScheduledOrderAtUtc = x.ScheduledOrderAtUtc,
            CustomerOrderId = x.CustomerOrderId,
            CustomerOrderCode = x.CustomerOrderCode,
            CustomerOrderRequiredDate = x.CustomerOrderRequiredDate,
            CustomerOrderLineId = x.CustomerOrderLineId,
            CustomerId = x.CustomerId,
            CustomerCode = x.CustomerCode,
            CustomerName = x.CustomerName,
            Mobile = x.Mobile,
            LineType = x.LineType,
            ProductVariantId = x.ProductVariantId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            VariantDescription = x.VariantDescription,
            Eye = x.Eye,
            MeasurementSource = x.MeasurementSource,
            SPH = x.SPH,
            CYL = x.CYL,
            Axis = x.Axis,
            ADD = x.ADD,
            Prism = x.Prism,
            PrismBase = x.PrismBase,
            PD = x.PD,
            MonocularPD = x.MonocularPD,
            FittingHeight = x.FittingHeight,
            LensType = x.LensType,
            Material = x.Material,
            Coating = x.Coating,
            RequestedQuantity = x.RequestedQuantity,
            AllocatedToPurchaseOrderQuantity = x.AllocatedToPurchaseOrderQuantity,
            AcceptedReceivedQuantity = x.AcceptedReceivedQuantity,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.WarehouseName,
            PreferredSupplierId = x.PreferredSupplierId,
            PreferredSupplierName = x.PreferredSupplierName,
            ActualSupplierId = x.ActualSupplierId,
            ActualSupplierName = x.ActualSupplierName,
            PurchaseOrderId = x.PurchaseOrderId,
            PurchaseOrderCode = x.PurchaseOrderCode,
            SentAt = x.SentAt,
            EffectiveExpectedDeliveryDate = x.EffectiveExpectedDeliveryDate,
            PurchaseOrderStatus = x.PurchaseOrderStatus,
            OrderedQuantity = x.OrderedQuantity,
            UnitPrice = x.UnitPrice,
            LastReceiptDate = x.LastReceiptDate,
            ReceiptStatus = x.ReceiptStatus,
            ActualUnitCost = x.ActualUnitCost,
            RequiresProduction = x.RequiresProduction,
            Notes = x.Notes,
            HasOrderedPurchaseOrder = x.HasOrderedPurchaseOrder
        });
    }

    private static IQueryable<DemandRow> ApplyTrackingStatusFilter(
        IQueryable<DemandRow> query,
        CustomerDemandTrackingStatus? status,
        DateOnly today) => status switch
    {
        null => query,
        CustomerDemandTrackingStatus.Received => query.Where(x => x.AcceptedReceivedQuantity >= x.RequestedQuantity),
        CustomerDemandTrackingStatus.PartiallyReceived => query.Where(x => x.AcceptedReceivedQuantity > 0m && x.AcceptedReceivedQuantity < x.RequestedQuantity),
        CustomerDemandTrackingStatus.Overdue => query.Where(x => x.AcceptedReceivedQuantity < x.RequestedQuantity && x.HasOrderedPurchaseOrder && x.EffectiveExpectedDeliveryDate != null && x.EffectiveExpectedDeliveryDate < today),
        CustomerDemandTrackingStatus.DueToday => query.Where(x => x.AcceptedReceivedQuantity < x.RequestedQuantity && x.HasOrderedPurchaseOrder && x.EffectiveExpectedDeliveryDate == today),
        CustomerDemandTrackingStatus.Ordered => query.Where(x => x.AcceptedReceivedQuantity < x.RequestedQuantity && x.HasOrderedPurchaseOrder),
        CustomerDemandTrackingStatus.Scheduled => query.Where(x => x.AcceptedReceivedQuantity < x.RequestedQuantity && !x.HasOrderedPurchaseOrder && x.ScheduledOrderAtUtc != null),
        CustomerDemandTrackingStatus.New => query.Where(x => x.AcceptedReceivedQuantity < x.RequestedQuantity && x.AllocatedToPurchaseOrderQuantity == 0m && x.PreferredSupplierId == null && (x.RequestStatus == PurchaseRequestStatus.Draft || x.RequestStatus == PurchaseRequestStatus.PendingApproval)),
        CustomerDemandTrackingStatus.AwaitingSupplier => query.Where(x => x.AcceptedReceivedQuantity < x.RequestedQuantity && !x.HasOrderedPurchaseOrder && x.PreferredSupplierId == null && x.ActualSupplierId == null),
        _ => query
    };

    private static IQueryable<DemandRow> ApplySort(
        IQueryable<DemandRow> query,
        string? sortBy,
        SortDirection direction)
    {
        var key = sortBy?.Trim().ToLowerInvariant();
        var desc = direction == SortDirection.Descending;
        return key switch
        {
            "requestcode" => desc ? query.OrderByDescending(x => x.RequestCode) : query.OrderBy(x => x.RequestCode),
            "customerordercode" => desc ? query.OrderByDescending(x => x.CustomerOrderCode) : query.OrderBy(x => x.CustomerOrderCode),
            "customercode" => desc ? query.OrderByDescending(x => x.CustomerCode) : query.OrderBy(x => x.CustomerCode),
            "customername" => desc ? query.OrderByDescending(x => x.CustomerName) : query.OrderBy(x => x.CustomerName),
            "productname" => desc ? query.OrderByDescending(x => x.ProductName) : query.OrderBy(x => x.ProductName),
            "expecteddeliverydate" => desc ? query.OrderByDescending(x => x.EffectiveExpectedDeliveryDate) : query.OrderBy(x => x.EffectiveExpectedDeliveryDate),
            "remainingqty" or "remainingquantity" => desc
                ? query.OrderByDescending(x => x.RequestedQuantity - x.AcceptedReceivedQuantity)
                : query.OrderBy(x => x.RequestedQuantity - x.AcceptedReceivedQuantity),
            "scheduledorderatutc" => desc ? query.OrderByDescending(x => x.ScheduledOrderAtUtc) : query.OrderBy(x => x.ScheduledOrderAtUtc),
            _ => desc ? query.OrderByDescending(x => x.RequestDate).ThenByDescending(x => x.RequestCode)
                      : query.OrderBy(x => x.RequestDate).ThenBy(x => x.RequestCode)
        };
    }

    private static CustomerDemandTrackingItemDto MapItem(DemandRow x, bool includeCosts, DateOnly today)
    {
        var remaining = Math.Max(0m, x.RequestedQuantity - x.AcceptedReceivedQuantity);
        var status = DeriveStatus(x, today);
        int? delay = status == CustomerDemandTrackingStatus.Overdue && x.EffectiveExpectedDeliveryDate.HasValue
            ? today.DayNumber - x.EffectiveExpectedDeliveryDate.Value.DayNumber
            : null;

        return new CustomerDemandTrackingItemDto(
            x.PurchaseRequestLineId,
            x.PurchaseRequestId,
            x.RequestCode,
            x.RequestDate,
            x.ScheduledOrderAtUtc,
            x.CustomerOrderId,
            x.CustomerOrderCode,
            x.CustomerOrderLineId,
            x.CustomerId,
            x.CustomerCode,
            x.CustomerName,
            x.Mobile,
            (ContractSalesLineType)(byte)x.LineType,
            x.ProductVariantId,
            x.ProductCode,
            x.ProductName,
            x.Eye.HasValue ? (ContractEyeSide?)(byte)x.Eye.Value : null,
            FormatOpticalSummary(x),
            x.RequestedQuantity,
            x.AllocatedToPurchaseOrderQuantity,
            x.AcceptedReceivedQuantity,
            remaining,
            x.WarehouseId,
            x.WarehouseName,
            x.PreferredSupplierId,
            x.PreferredSupplierName,
            x.ActualSupplierId,
            x.ActualSupplierName,
            x.PurchaseOrderId,
            x.PurchaseOrderCode,
            x.SentAt,
            x.EffectiveExpectedDeliveryDate,
            includeCosts ? x.UnitPrice : null,
            includeCosts ? x.ActualUnitCost : null,
            x.LastReceiptDate,
            status,
            delay,
            x.RequiresProduction);
    }

    private static CustomerDemandTrackingStatus DeriveStatus(DemandRow x, DateOnly today)
    {
        if (x.AcceptedReceivedQuantity >= x.RequestedQuantity)
            return CustomerDemandTrackingStatus.Received;
        if (x.AcceptedReceivedQuantity > 0m)
            return CustomerDemandTrackingStatus.PartiallyReceived;
        if (x.HasOrderedPurchaseOrder && x.EffectiveExpectedDeliveryDate.HasValue && x.EffectiveExpectedDeliveryDate.Value < today)
            return CustomerDemandTrackingStatus.Overdue;
        if (x.HasOrderedPurchaseOrder && x.EffectiveExpectedDeliveryDate == today)
            return CustomerDemandTrackingStatus.DueToday;
        if (x.HasOrderedPurchaseOrder)
            return CustomerDemandTrackingStatus.Ordered;
        if (x.ScheduledOrderAtUtc.HasValue)
            return CustomerDemandTrackingStatus.Scheduled;
        if (x.PreferredSupplierId is null && x.RequestStatus is PurchaseRequestStatus.Draft or PurchaseRequestStatus.PendingApproval)
            return CustomerDemandTrackingStatus.New;
        return CustomerDemandTrackingStatus.AwaitingSupplier;
    }

    private static string? FormatOpticalSummary(DemandRow x)
    {
        var parts = new List<string>();
        if (x.SPH.HasValue) parts.Add($"SPH {x.SPH:0.##}");
        if (x.CYL.HasValue) parts.Add($"CYL {x.CYL:0.##}");
        if (x.Axis.HasValue) parts.Add($"Axis {x.Axis}");
        if (x.ADD.HasValue) parts.Add($"ADD {x.ADD:0.##}");
        if (!string.IsNullOrWhiteSpace(x.Material)) parts.Add(x.Material!);
        if (!string.IsNullOrWhiteSpace(x.Coating)) parts.Add(x.Coating!);
        return parts.Count == 0 ? null : string.Join(" / ", parts);
    }

    private sealed class DemandRow
    {
        public Guid PurchaseRequestLineId { get; init; }
        public Guid PurchaseRequestId { get; init; }
        public string RequestCode { get; init; } = string.Empty;
        public DateOnly RequestDate { get; init; }
        public PurchaseRequestStatus RequestStatus { get; init; }
        public DateTimeOffset? ScheduledOrderAtUtc { get; init; }
        public Guid CustomerOrderId { get; init; }
        public string CustomerOrderCode { get; init; } = string.Empty;
        public DateOnly? CustomerOrderRequiredDate { get; init; }
        public Guid CustomerOrderLineId { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerCode { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string? Mobile { get; init; }
        public OAS.Domain.Sales.Enums.SalesLineType LineType { get; init; }
        public Guid ProductVariantId { get; init; }
        public string ProductCode { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public string? VariantDescription { get; init; }
        public OAS.Domain.Sales.Enums.EyeSide? Eye { get; init; }
        public OAS.Domain.Sales.Enums.OpticalMeasurementSource? MeasurementSource { get; init; }
        public decimal? SPH { get; init; }
        public decimal? CYL { get; init; }
        public short? Axis { get; init; }
        public decimal? ADD { get; init; }
        public decimal? Prism { get; init; }
        public OAS.Domain.Sales.Enums.PrismBaseDirection? PrismBase { get; init; }
        public decimal? PD { get; init; }
        public decimal? MonocularPD { get; init; }
        public decimal? FittingHeight { get; init; }
        public string? LensType { get; init; }
        public string? Material { get; init; }
        public string? Coating { get; init; }
        public decimal RequestedQuantity { get; init; }
        public decimal AllocatedToPurchaseOrderQuantity { get; init; }
        public decimal AcceptedReceivedQuantity { get; init; }
        public Guid WarehouseId { get; init; }
        public string? WarehouseName { get; init; }
        public Guid? PreferredSupplierId { get; init; }
        public string? PreferredSupplierName { get; init; }
        public Guid? ActualSupplierId { get; init; }
        public string? ActualSupplierName { get; init; }
        public Guid? PurchaseOrderId { get; init; }
        public string? PurchaseOrderCode { get; init; }
        public DateTimeOffset? SentAt { get; init; }
        public DateOnly? EffectiveExpectedDeliveryDate { get; init; }
        public PurchaseOrderStatus? PurchaseOrderStatus { get; init; }
        public decimal OrderedQuantity { get; init; }
        public decimal? UnitPrice { get; init; }
        public DateOnly? LastReceiptDate { get; init; }
        public PurchaseReceiptStatus? ReceiptStatus { get; init; }
        public decimal? ActualUnitCost { get; init; }
        public bool RequiresProduction { get; init; }
        public string? Notes { get; init; }
        public bool HasOrderedPurchaseOrder { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }
}
