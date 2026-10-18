using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Persistence.Queries;

public sealed partial class SupplierPerformanceReportingQueryService
{
    private IQueryable<LineSeed> BuildSeedQuery(SupplierPerformanceQueryRequest request)
    {
        var query =
            from line in _db.Set<PurchaseOrderLine>().AsNoTracking()
            join order in _db.Set<PurchaseOrder>().AsNoTracking() on line.PurchaseOrderId equals order.Id
            join supplier in _db.Set<Supplier>().AsNoTracking() on order.SupplierId equals supplier.Id
            join warehouse in _db.Set<Warehouse>().AsNoTracking() on order.DestinationWarehouseId equals warehouse.Id
            join currency in _db.Set<Currency>().AsNoTracking() on order.CurrencyId equals currency.Id
            where ReportableOrderStatuses.Contains(order.Status)
                  && (!request.SupplierId.HasValue || order.SupplierId == request.SupplierId.Value)
                  && (!request.WarehouseId.HasValue || order.DestinationWarehouseId == request.WarehouseId.Value)
                  && (!request.ProductVariantId.HasValue || line.ProductVariantId == request.ProductVariantId.Value)
                  && (!request.CurrencyId.HasValue || order.CurrencyId == request.CurrencyId.Value)
            select new LineSeed
            {
                SupplierId = supplier.Id,
                SupplierCode = supplier.SupplierCode,
                SupplierName = supplier.NameAr,
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                CurrencyId = currency.Id,
                CurrencyCode = currency.Code,
                ProductVariantId = line.ProductVariantId,
                ProductCode = line.ProductCodeSnapshot,
                ProductName = line.ProductNameSnapshot,
                PurchaseOrderId = order.Id,
                PurchaseOrderLineId = line.Id,
                PurchaseOrderCode = order.PurchaseOrderCode,
                Status = order.Status,
                OrderDate = order.OrderDate,
                ExpectedDeliveryDate = line.ExpectedDeliveryDate ?? order.ExpectedDeliveryDate,
                OrderedQuantity = line.OrderedQuantity,
                BaseQuantity = line.BaseQuantity,
                NetAmount = line.NetAmount,
                ExchangeRate = order.ExchangeRate
            };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.PurchaseOrderCode.Contains(search)
                || x.SupplierCode.Contains(search)
                || x.SupplierName.Contains(search)
                || (x.ProductCode != null && x.ProductCode.Contains(search))
                || x.ProductName.Contains(search));
        }
        return query;
    }

    private IQueryable<LineSeed> ApplyActivityWindowFilter(
        IQueryable<LineSeed> query,
        SupplierPerformanceQueryRequest request)
    {
        if (!request.FromDate.HasValue && !request.ToDate.HasValue) return query;

        return query.Where(x =>
            ((!request.FromDate.HasValue || x.OrderDate >= request.FromDate.Value) &&
             (!request.ToDate.HasValue || x.OrderDate <= request.ToDate.Value)) ||
            (from rl in _db.Set<PurchaseReceiptLine>()
             join r in _db.Set<PurchaseReceipt>() on rl.PurchaseReceiptId equals r.Id
             where rl.PurchaseOrderLineId == x.PurchaseOrderLineId
                   && r.Status == PurchaseReceiptStatus.Posted
                   && (!request.FromDate.HasValue || r.PostingDate >= request.FromDate.Value)
                   && (!request.ToDate.HasValue || r.PostingDate <= request.ToDate.Value)
             select rl.Id).Any() ||
            (from allocation in _db.Set<PurchaseInvoiceReceiptAllocation>()
             join rl in _db.Set<PurchaseReceiptLine>() on allocation.PurchaseReceiptLineId equals rl.Id
             join il in _db.Set<PurchaseInvoiceLine>() on allocation.PurchaseInvoiceLineId equals il.Id
             join invoice in _db.Set<PurchaseInvoice>() on il.PurchaseInvoiceId equals invoice.Id
             where rl.PurchaseOrderLineId == x.PurchaseOrderLineId
                   && invoice.Status == PurchaseInvoiceStatus.Posted
                   && (!request.FromDate.HasValue || invoice.PostingDate >= request.FromDate.Value)
                   && (!request.ToDate.HasValue || invoice.PostingDate <= request.ToDate.Value)
             select allocation.Id).Any() ||
            (from returnLine in _db.Set<PurchaseReturnLine>()
             join rl in _db.Set<PurchaseReceiptLine>() on returnLine.PurchaseReceiptLineId equals rl.Id
             join pr in _db.Set<PurchaseReturn>() on returnLine.PurchaseReturnId equals pr.Id
             where rl.PurchaseOrderLineId == x.PurchaseOrderLineId
                   && pr.Status == PurchaseReturnStatus.Posted
                   && returnLine.IsActive
                   && (!request.FromDate.HasValue || pr.PostingDate >= request.FromDate.Value)
                   && (!request.ToDate.HasValue || pr.PostingDate <= request.ToDate.Value)
             select returnLine.Id).Any());
    }

    private static IQueryable<LineSeed> ApplyOrderDateWindow(
        IQueryable<LineSeed> query,
        SupplierPerformanceQueryRequest request)
    {
        if (request.FromDate.HasValue) query = query.Where(x => x.OrderDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(x => x.OrderDate <= request.ToDate.Value);
        return query;
    }

    private static bool IsInWindow(DateOnly date, SupplierPerformanceQueryRequest request) =>
        (!request.FromDate.HasValue || date >= request.FromDate.Value) &&
        (!request.ToDate.HasValue || date <= request.ToDate.Value);

    private IQueryable<LineSeed> ApplyOpenLateFilter(
        IQueryable<LineSeed> query,
        SupplierPerformanceQueryRequest request,
        DateOnly asOf)
    {
        if (!request.OnlyOpen && !request.OnlyLate) return query;
        return query.Where(x =>
            (x.Status == PurchaseOrderStatus.Sent || x.Status == PurchaseOrderStatus.PartiallyReceived) &&
            x.BaseQuantity > ((
                from rl in _db.Set<PurchaseReceiptLine>()
                join r in _db.Set<PurchaseReceipt>() on rl.PurchaseReceiptId equals r.Id
                where rl.PurchaseOrderLineId == x.PurchaseOrderLineId
                      && r.Status == PurchaseReceiptStatus.Posted
                      && r.PostingDate <= asOf
                select (decimal?)rl.BaseAcceptedQuantity).Sum() ?? 0m) &&
            (!request.OnlyLate || (x.ExpectedDeliveryDate != null && x.ExpectedDeliveryDate < asOf)));
    }

    private IQueryable<ReceiptActivity> BuildReceiptActivityQuery(SupplierPerformanceQueryRequest request, IQueryable<LineSeed> baseQuery) =>
        from rl in _db.Set<PurchaseReceiptLine>().AsNoTracking()
        join receipt in _db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals receipt.Id
        join seed in baseQuery on rl.PurchaseOrderLineId equals seed.PurchaseOrderLineId
        where receipt.Status == PurchaseReceiptStatus.Posted
              && (!request.FromDate.HasValue || receipt.PostingDate >= request.FromDate.Value)
              && (!request.ToDate.HasValue || receipt.PostingDate <= request.ToDate.Value)
        select new ReceiptActivity
        {
            PurchaseOrderLineId = rl.PurchaseOrderLineId,
            PurchaseReceiptId = receipt.Id,
            ReceiptCode = receipt.ReceiptCode,
            ReceiptDate = receipt.ReceiptDate,
            PostingDate = receipt.PostingDate,
            BaseAcceptedQuantity = rl.BaseAcceptedQuantity,
            AcceptedQuantity = rl.AcceptedQuantity,
            ActualUnitCost = rl.ActualUnitCost,
            TotalAcceptedCost = rl.TotalAcceptedCost
        };

    private IQueryable<ReturnActivity> BuildReturnActivityQuery(SupplierPerformanceQueryRequest request, IQueryable<LineSeed> baseQuery) =>
        from returnLine in _db.Set<PurchaseReturnLine>().AsNoTracking()
        join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on returnLine.PurchaseReceiptLineId equals receiptLine.Id
        join purchaseReturn in _db.Set<PurchaseReturn>().AsNoTracking() on returnLine.PurchaseReturnId equals purchaseReturn.Id
        join seed in baseQuery on receiptLine.PurchaseOrderLineId equals seed.PurchaseOrderLineId
        where purchaseReturn.Status == PurchaseReturnStatus.Posted && returnLine.IsActive
              && (!request.FromDate.HasValue || purchaseReturn.PostingDate >= request.FromDate.Value)
              && (!request.ToDate.HasValue || purchaseReturn.PostingDate <= request.ToDate.Value)
        select new ReturnActivity
        {
            PurchaseOrderLineId = receiptLine.PurchaseOrderLineId,
            PurchaseReturnId = purchaseReturn.Id,
            ReturnCode = purchaseReturn.ReturnCode,
            PostingDate = purchaseReturn.PostingDate,
            PurchaseInvoiceLineId = returnLine.PurchaseInvoiceLineId,
            BaseQuantity = returnLine.BaseQuantity,
            ReceiptCostBaseAmount = returnLine.ReceiptCostBaseAmount,
            SupplierGrossBaseAmount = returnLine.SupplierGrossBaseAmount
        };

    private IQueryable<InvoiceActivity> BuildPostedInvoiceActivityQuery(SupplierPerformanceQueryRequest request, IQueryable<LineSeed> baseQuery) =>
        from allocation in _db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
        join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on allocation.PurchaseReceiptLineId equals receiptLine.Id
        join invoiceLine in _db.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
        join invoice in _db.Set<PurchaseInvoice>().AsNoTracking() on invoiceLine.PurchaseInvoiceId equals invoice.Id
        join seed in baseQuery on receiptLine.PurchaseOrderLineId equals seed.PurchaseOrderLineId
        where invoice.Status == PurchaseInvoiceStatus.Posted
              && (!request.FromDate.HasValue || invoice.PostingDate >= request.FromDate.Value)
              && (!request.ToDate.HasValue || invoice.PostingDate <= request.ToDate.Value)
        select new InvoiceActivity
        {
            PurchaseOrderLineId = receiptLine.PurchaseOrderLineId,
            PurchaseInvoiceId = invoice.Id,
            PurchaseInvoiceCode = invoice.PurchaseInvoiceCode,
            PostingDate = invoice.PostingDate,
            MatchedQuantity = allocation.MatchedQuantity,
            MatchedNetAmount = allocation.MatchedNetAmount,
            ExchangeRate = invoice.ExchangeRate,
            BaseFinalAmount = invoiceLine.BaseFinalAmount,
            InvoiceLineQuantity = invoiceLine.Quantity,
            ReceiptAcceptedQuantity = receiptLine.AcceptedQuantity,
            ReceiptBaseAcceptedQuantity = receiptLine.BaseAcceptedQuantity,
            ReceiptActualUnitCost = receiptLine.ActualUnitCost
        };
}
