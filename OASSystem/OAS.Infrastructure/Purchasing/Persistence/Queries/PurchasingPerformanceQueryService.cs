using Microsoft.EntityFrameworkCore;
using OAS.Application.Purchasing.Performance;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Performance;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Persistence.Queries;

/// <summary>
/// AP-12 read-model. Never stores balances, never creates AP from orders or receipts.
/// Each source is aggregated independently, and all pagination happens in SQL.
/// Monetary amounts are identified as document currency or base currency explicitly.
/// </summary>
public sealed partial class PurchasingPerformanceQueryService(OasDbContext db) : IPurchasingPerformanceQueryService
{
    public async Task<PurchasingPerformanceReportDto> GetAsync(PurchasingPerformanceFilterDto filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var kind = filter.Kind.Trim().ToLowerInvariant();
        if (filter.FromDate > filter.ToDate) throw new ArgumentException("تاريخ البداية يجب ألا يتجاوز تاريخ النهاية.");
        if (filter.PageNumber < 1 || filter.PageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(filter), "رقم الصفحة وحجمها غير صالحين (1 إلى 100).");
        if (kind is not ("orders" or "receipts" or "invoices" or "returns" or "prices"))
            throw new ArgumentException("نوع تقرير المشتريات غير معروف.");

        var settings = await db.Set<AccountingSettings>().AsNoTracking()
            .Where(x => x.Id == AccountingSettings.SingletonId)
            .Select(x => new { x.BaseCurrencyId }).FirstOrDefaultAsync(ct);
        var baseCurrencyId = settings?.BaseCurrencyId;
        var baseCurrencyCode = baseCurrencyId.HasValue
            ? await db.Set<Currency>().AsNoTracking().Where(x => x.Id == baseCurrencyId.Value)
                .Select(x => x.Code).FirstOrDefaultAsync(ct) ?? "العملة الأساسية"
            : "العملة الأساسية";
        var pageOffset = checked((filter.PageNumber - 1) * filter.PageSize);
        var search = filter.Search?.Trim();

        var orders =
            from line in db.Set<PurchaseOrderLine>().AsNoTracking()
            join order in db.Set<PurchaseOrder>().AsNoTracking() on line.PurchaseOrderId equals order.Id
            join supplier in db.Set<Supplier>().AsNoTracking() on order.SupplierId equals supplier.Id
            join currency in db.Set<Currency>().AsNoTracking() on order.CurrencyId equals currency.Id
            where order.OrderDate >= filter.FromDate && order.OrderDate <= filter.ToDate
                && order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.Rejected
                && order.Status != PurchaseOrderStatus.Cancelled
                && (!filter.SupplierId.HasValue || order.SupplierId == filter.SupplierId.Value)
                && (!filter.WarehouseId.HasValue || order.DestinationWarehouseId == filter.WarehouseId.Value)
                && (!filter.ProductVariantId.HasValue || line.ProductVariantId == filter.ProductVariantId.Value)
                && (!filter.CurrencyId.HasValue || order.CurrencyId == filter.CurrencyId.Value)
                && (search == null || search == "" || order.PurchaseOrderCode.Contains(search) || line.ProductNameSnapshot.Contains(search))
            select new { LineId = line.Id, order.Id, order.PurchaseOrderCode, order.OrderDate, order.SupplierId,
                supplier.NameAr, order.DestinationWarehouseId, line.ProductVariantId, line.ProductNameSnapshot,
                line.UnitNameSnapshot, line.OrderedQuantity, line.UnitPrice, line.FinalAmount, order.Status,
                DueDate = line.ExpectedDeliveryDate ?? order.ExpectedDeliveryDate, currency.Code };

        var receipts =
            from line in db.Set<PurchaseReceiptLine>().AsNoTracking()
            join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on line.PurchaseReceiptId equals receipt.Id
            join orderLine in db.Set<PurchaseOrderLine>().AsNoTracking() on line.PurchaseOrderLineId equals orderLine.Id
            join order in db.Set<PurchaseOrder>().AsNoTracking() on receipt.PurchaseOrderId equals order.Id
            join supplier in db.Set<Supplier>().AsNoTracking() on receipt.SupplierId equals supplier.Id
            where receipt.Status == PurchaseReceiptStatus.Posted && receipt.SupplierId == order.SupplierId
                && receipt.PostingDate >= filter.FromDate && receipt.PostingDate <= filter.ToDate
                && (!filter.SupplierId.HasValue || receipt.SupplierId == filter.SupplierId.Value)
                && (!filter.WarehouseId.HasValue || receipt.WarehouseId == filter.WarehouseId.Value)
                && (!filter.ProductVariantId.HasValue || line.ProductVariantId == filter.ProductVariantId.Value)
                && (!filter.CurrencyId.HasValue || filter.CurrencyId.Value == baseCurrencyId)
                && (search == null || search == "" || receipt.ReceiptCode.Contains(search) || orderLine.ProductNameSnapshot.Contains(search))
            select new { LineId = line.Id, receipt.Id, receipt.ReceiptCode, receipt.PostingDate,
                receipt.ReceiptDate, receipt.SupplierId, supplier.NameAr, receipt.WarehouseId, line.ProductVariantId,
                orderLine.ProductNameSnapshot, orderLine.UnitNameSnapshot, line.AcceptedQuantity, line.TotalAcceptedCost,
                PurchaseOrderId = order.Id, order.OrderDate,
                DueDate = orderLine.ExpectedDeliveryDate ?? order.ExpectedDeliveryDate };

        var invoices =
            from line in db.Set<PurchaseInvoiceLine>().AsNoTracking()
            join invoice in db.Set<PurchaseInvoice>().AsNoTracking() on line.PurchaseInvoiceId equals invoice.Id
            join supplier in db.Set<Supplier>().AsNoTracking() on invoice.SupplierId equals supplier.Id
            join currency in db.Set<Currency>().AsNoTracking() on invoice.CurrencyId equals currency.Id
            where invoice.Status == PurchaseInvoiceStatus.Posted
                && invoice.PostingDate >= filter.FromDate && invoice.PostingDate <= filter.ToDate
                && (!filter.SupplierId.HasValue || invoice.SupplierId == filter.SupplierId.Value)
                && (!filter.ProductVariantId.HasValue || line.ProductVariantId == filter.ProductVariantId.Value)
                && (!filter.CurrencyId.HasValue || invoice.CurrencyId == filter.CurrencyId.Value)
                && (!filter.WarehouseId.HasValue || (
                    from a in db.Set<PurchaseInvoiceReceiptAllocation>()
                    join rl in db.Set<PurchaseReceiptLine>() on a.PurchaseReceiptLineId equals rl.Id
                    join receipt in db.Set<PurchaseReceipt>() on rl.PurchaseReceiptId equals receipt.Id
                    where a.PurchaseInvoiceLineId == line.Id && receipt.Status == PurchaseReceiptStatus.Posted
                        && receipt.SupplierId == invoice.SupplierId && receipt.WarehouseId == filter.WarehouseId.Value
                    select a.Id).Any())
                && (search == null || search == "" || invoice.PurchaseInvoiceCode.Contains(search) || line.DescriptionSnapshot.Contains(search))
            select new { LineId = line.Id, invoice.Id, invoice.PurchaseInvoiceCode, invoice.PostingDate,
                invoice.SupplierId, supplier.NameAr, line.ProductVariantId, line.DescriptionSnapshot, line.Quantity,
                line.UnitPrice, line.FinalAmount, line.BaseFinalAmount, line.PurchaseOrderLineId, currency.Code };

        var returns =
            from line in db.Set<PurchaseReturnLine>().AsNoTracking()
            join purchaseReturn in db.Set<PurchaseReturn>().AsNoTracking() on line.PurchaseReturnId equals purchaseReturn.Id
            join receiptLine in db.Set<PurchaseReceiptLine>().AsNoTracking() on line.PurchaseReceiptLineId equals receiptLine.Id
            join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on receiptLine.PurchaseReceiptId equals receipt.Id
            join orderLine in db.Set<PurchaseOrderLine>().AsNoTracking() on receiptLine.PurchaseOrderLineId equals orderLine.Id
            join supplier in db.Set<Supplier>().AsNoTracking() on purchaseReturn.SupplierId equals supplier.Id
            where purchaseReturn.Status == PurchaseReturnStatus.Posted && receipt.Status == PurchaseReceiptStatus.Posted
                && purchaseReturn.SupplierId == receipt.SupplierId && purchaseReturn.PurchaseReceiptId == receipt.Id
                && purchaseReturn.PostingDate >= filter.FromDate && purchaseReturn.PostingDate <= filter.ToDate
                && (!filter.SupplierId.HasValue || purchaseReturn.SupplierId == filter.SupplierId.Value)
                && (!filter.WarehouseId.HasValue || purchaseReturn.WarehouseId == filter.WarehouseId.Value)
                && (!filter.ProductVariantId.HasValue || line.ProductVariantId == filter.ProductVariantId.Value)
                && (!filter.CurrencyId.HasValue || filter.CurrencyId.Value == baseCurrencyId)
                && (search == null || search == "" || purchaseReturn.ReturnCode.Contains(search) || orderLine.ProductNameSnapshot.Contains(search))
            select new { LineId = line.Id, purchaseReturn.Id, purchaseReturn.ReturnCode,
                purchaseReturn.PostingDate, purchaseReturn.SupplierId, supplier.NameAr, purchaseReturn.WarehouseId,
                line.ProductVariantId, orderLine.ProductNameSnapshot, orderLine.UnitNameSnapshot,
                line.Quantity, line.SupplierGrossBaseAmount, line.ReceiptCostBaseAmount,
                receipt.PurchaseOrderId, PurchaseReceiptId = receipt.Id, purchaseReturn.PurchaseInvoiceId };

        // Independent SQL aggregates. No orders/receipts are ever counted as posted AP liabilities.
        var orderCount = await orders.Select(x => x.Id).Distinct().CountAsync(ct);
        var openOrders = orders.Where(x => x.Status == PurchaseOrderStatus.Sent || x.Status == PurchaseOrderStatus.PartiallyReceived);
        // A sent PO with fully posted accepted quantities has no open delivery even if
        // a delayed state update still marks the header PartiallyReceived.
        var stillOpen = openOrders.Where(x => x.OrderedQuantity >
            ((from rl in db.Set<PurchaseReceiptLine>()
             join receipt in db.Set<PurchaseReceipt>() on rl.PurchaseReceiptId equals receipt.Id
             where rl.PurchaseOrderLineId == x.LineId && receipt.Status == PurchaseReceiptStatus.Posted
                 && receipt.PostingDate <= filter.ToDate
             select (decimal?)rl.AcceptedQuantity).Sum() ?? 0m));
        var openOrderCount = await stillOpen.Select(x => x.Id).Distinct().CountAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var asOf = filter.ToDate < today ? filter.ToDate : today;
        var lateCount = await stillOpen.Where(x => x.DueDate != null && x.DueDate < asOf)
            .Select(x => x.Id).Distinct().CountAsync(ct);
        var receiptCount = await receipts.Select(x => x.Id).Distinct().CountAsync(ct);
        var invoiceCount = await invoices.Select(x => x.Id).Distinct().CountAsync(ct);
        var returnCount = await returns.Select(x => x.Id).Distinct().CountAsync(ct);
        var averageLead = await receipts.AverageAsync(x =>
            (double?)EF.Functions.DateDiffDay(x.OrderDate, x.ReceiptDate), ct);
        var receiptBase = await receipts.SumAsync(x => (decimal?)x.TotalAcceptedCost, ct) ?? 0m;
        var invoiceBase = await invoices.SumAsync(x => (decimal?)x.BaseFinalAmount, ct) ?? 0m;
        var returnBase = await returns.SumAsync(x => (decimal?)x.SupplierGrossBaseAmount, ct) ?? 0m;
        var advanced = await GetAdvancedMetricsAsync(filter, ct);
        var summary = new PurchasingPerformanceSummaryDto(orderCount, openOrderCount, lateCount,
            receiptCount, invoiceCount, returnCount, averageLead, receiptBase, invoiceBase, returnBase, baseCurrencyCode,
            advanced.OnTimeDeliveryRatePercent, advanced.UninvoicedReceiptCostBase, advanced.PurchasePriceVarianceBase);

        var rows = new List<PurchasingPerformanceRowDto>();
        var total = 0;
        if (kind == "orders")
        {
            total = await orders.CountAsync(ct);
            var page = await orders.OrderByDescending(x => x.OrderDate).ThenBy(x => x.PurchaseOrderCode).ThenBy(x => x.LineId)
                .Skip(pageOffset).Take(filter.PageSize).ToListAsync(ct);
            var ids = page.Select(x => x.LineId).ToArray();
            var accepted = await (from rl in db.Set<PurchaseReceiptLine>().AsNoTracking()
                    join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals receipt.Id
                    where ids.Contains(rl.PurchaseOrderLineId) && receipt.Status == PurchaseReceiptStatus.Posted
                        && receipt.PostingDate <= filter.ToDate
                    group rl by rl.PurchaseOrderLineId into g select new { Id = g.Key, Quantity = g.Sum(x => x.AcceptedQuantity) })
                .ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);
            var invoiced = await (from a in db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
                    join rl in db.Set<PurchaseReceiptLine>().AsNoTracking() on a.PurchaseReceiptLineId equals rl.Id
                    join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals receipt.Id
                    join il in db.Set<PurchaseInvoiceLine>().AsNoTracking() on a.PurchaseInvoiceLineId equals il.Id
                    join invoice in db.Set<PurchaseInvoice>().AsNoTracking() on il.PurchaseInvoiceId equals invoice.Id
                    where ids.Contains(rl.PurchaseOrderLineId) && receipt.Status == PurchaseReceiptStatus.Posted
                          && invoice.Status == PurchaseInvoiceStatus.Posted && invoice.SupplierId == receipt.SupplierId
                        && receipt.PostingDate <= filter.ToDate && invoice.PostingDate <= filter.ToDate
                    group a by rl.PurchaseOrderLineId into g select new { Id = g.Key, Quantity = g.Sum(x => x.MatchedQuantity) })
                .ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);
            var returned = await (from rl in db.Set<PurchaseReturnLine>().AsNoTracking()
                    join pr in db.Set<PurchaseReturn>().AsNoTracking() on rl.PurchaseReturnId equals pr.Id
                    join receiptLine in db.Set<PurchaseReceiptLine>().AsNoTracking() on rl.PurchaseReceiptLineId equals receiptLine.Id
                    join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on receiptLine.PurchaseReceiptId equals receipt.Id
                    where ids.Contains(receiptLine.PurchaseOrderLineId) && pr.Status == PurchaseReturnStatus.Posted
                        && receipt.Status == PurchaseReceiptStatus.Posted && pr.SupplierId == receipt.SupplierId && pr.PurchaseReceiptId == receipt.Id
                        && receipt.PostingDate <= filter.ToDate && pr.PostingDate <= filter.ToDate
                    group rl by receiptLine.PurchaseOrderLineId into g select new { Id = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);
            // Only pre-invoice returns reduce the physically accepted but not yet invoiced
            // quantity. Post-invoice returns are credits against AP, not GRNI reversals.
            var preInvoiceReturned = await (from rl in db.Set<PurchaseReturnLine>().AsNoTracking()
                    join pr in db.Set<PurchaseReturn>().AsNoTracking() on rl.PurchaseReturnId equals pr.Id
                    join receiptLine in db.Set<PurchaseReceiptLine>().AsNoTracking() on rl.PurchaseReceiptLineId equals receiptLine.Id
                    join receipt in db.Set<PurchaseReceipt>().AsNoTracking() on receiptLine.PurchaseReceiptId equals receipt.Id
                    where ids.Contains(receiptLine.PurchaseOrderLineId) && pr.Status == PurchaseReturnStatus.Posted
                        && pr.PurchaseInvoiceId == null && receipt.Status == PurchaseReceiptStatus.Posted
                        && pr.SupplierId == receipt.SupplierId && pr.PurchaseReceiptId == receipt.Id
                        && receipt.PostingDate <= filter.ToDate && pr.PostingDate <= filter.ToDate
                    group rl by receiptLine.PurchaseOrderLineId into g select new { Id = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);
            foreach (var x in page)
            {
                var received = accepted.GetValueOrDefault(x.LineId);
                var billed = invoiced.GetValueOrDefault(x.LineId);
                var returnsQty = returned.GetValueOrDefault(x.LineId);
                var open = PurchasingPerformanceMath.OpenQuantity(x.OrderedQuantity, received,
                    x.Status is PurchaseOrderStatus.Closed or PurchaseOrderStatus.FullyReceived);
                rows.Add(new PurchasingPerformanceRowDto {
                    Kind = kind, DocumentId = x.Id, PurchaseOrderId = x.Id, PurchaseOrderLineId = x.LineId, DocumentCode = x.PurchaseOrderCode,
                    DocumentDate = x.OrderDate, SupplierId = x.SupplierId, SupplierName = x.NameAr,
                    WarehouseId = x.DestinationWarehouseId, ProductVariantId = x.ProductVariantId,
                    ProductName = x.ProductNameSnapshot, UnitName = x.UnitNameSnapshot, CurrencyCode = x.Code,
                    Status = x.Status.ToString(), Quantity = x.OrderedQuantity, OrderedQuantity = x.OrderedQuantity,
                    AcceptedQuantity = received, InvoicedQuantity = billed, ReturnedQuantity = returnsQty,
                    OpenQuantity = open, UninvoicedAcceptedQuantity = PurchasingPerformanceMath.UninvoicedAccepted(received, billed, preInvoiceReturned.GetValueOrDefault(x.LineId)),
                    UnitPrice = x.UnitPrice, DocumentAmount = x.FinalAmount, ExpectedDeliveryDate = x.DueDate,
                    IsLate = open > 0m && x.DueDate.HasValue && x.DueDate.Value < asOf
                });
            }
        }
        else if (kind == "receipts")
        {
            total = await receipts.CountAsync(ct);
            var page = await receipts.OrderByDescending(x => x.PostingDate).ThenBy(x => x.ReceiptCode).ThenBy(x => x.LineId)
                .Skip(pageOffset).Take(filter.PageSize).ToListAsync(ct);
            rows.AddRange(page.Select(x => new PurchasingPerformanceRowDto {
                Kind = kind, DocumentId = x.Id, PurchaseReceiptId = x.Id, PurchaseOrderId = x.PurchaseOrderId,
                DocumentCode = x.ReceiptCode, DocumentDate = x.PostingDate, Status = "Posted",
                SupplierId = x.SupplierId, SupplierName = x.NameAr, WarehouseId = x.WarehouseId,
                ProductVariantId = x.ProductVariantId, ProductName = x.ProductNameSnapshot, UnitName = x.UnitNameSnapshot,
                CurrencyCode = baseCurrencyCode, Quantity = x.AcceptedQuantity, AcceptedQuantity = x.AcceptedQuantity,
                BaseAmount = x.TotalAcceptedCost, ExpectedDeliveryDate = x.DueDate,
                LeadTimeDays = x.ReceiptDate.DayNumber - x.OrderDate.DayNumber,
                IsLate = x.DueDate.HasValue && x.ReceiptDate > x.DueDate.Value
            }));
        }
        else if (kind == "invoices")
        {
            total = await invoices.CountAsync(ct);
            var page = await invoices.OrderByDescending(x => x.PostingDate).ThenBy(x => x.PurchaseInvoiceCode).ThenBy(x => x.LineId)
                .Skip(pageOffset).Take(filter.PageSize).ToListAsync(ct);
            var ids = page.Select(x => x.LineId).ToArray();
            var variances = await db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
                .Where(x => ids.Contains(x.PurchaseInvoiceLineId))
                .GroupBy(x => x.PurchaseInvoiceLineId)
                .Select(g => new { Id = g.Key, Value = g.Sum(x => x.PriceVarianceAmount) })
                .ToDictionaryAsync(x => x.Id, x => x.Value, ct);
            rows.AddRange(page.Select(x => new PurchasingPerformanceRowDto {
                Kind = kind, DocumentId = x.Id, PurchaseInvoiceId = x.Id,
                DocumentCode = x.PurchaseInvoiceCode, DocumentDate = x.PostingDate,
                Status = "Posted", SupplierId = x.SupplierId, SupplierName = x.NameAr,
                ProductVariantId = x.ProductVariantId, ProductName = x.DescriptionSnapshot,
                CurrencyCode = x.Code, Quantity = x.Quantity, InvoicedQuantity = x.Quantity,
                UnitPrice = x.UnitPrice, DocumentAmount = x.FinalAmount, BaseAmount = x.BaseFinalAmount,
                PriceVariance = variances.GetValueOrDefault(x.LineId)
            }));
        }
        else if (kind == "returns")
        {
            total = await returns.CountAsync(ct);
            var page = await returns.OrderByDescending(x => x.PostingDate).ThenBy(x => x.ReturnCode).ThenBy(x => x.LineId)
                .Skip(pageOffset).Take(filter.PageSize).ToListAsync(ct);
            rows.AddRange(page.Select(x => new PurchasingPerformanceRowDto {
                Kind = kind, DocumentId = x.Id, PurchaseReturnId = x.Id, PurchaseReceiptId = x.PurchaseReceiptId,
                PurchaseInvoiceId = x.PurchaseInvoiceId, PurchaseOrderId = x.PurchaseOrderId,
                DocumentCode = x.ReturnCode, DocumentDate = x.PostingDate, Status = "Posted",
                SupplierId = x.SupplierId, SupplierName = x.NameAr, WarehouseId = x.WarehouseId,
                ProductVariantId = x.ProductVariantId, ProductName = x.ProductNameSnapshot, UnitName = x.UnitNameSnapshot,
                CurrencyCode = baseCurrencyCode, Quantity = x.Quantity, ReturnedQuantity = x.Quantity,
                BaseAmount = x.SupplierGrossBaseAmount
            }));
        }
        else
        {
            var prices =
                from price in db.Set<SupplierPriceHistory>().AsNoTracking()
                join catalog in db.Set<SupplierCatalogItem>().AsNoTracking() on price.SupplierCatalogItemId equals catalog.Id
                join supplier in db.Set<Supplier>().AsNoTracking() on catalog.SupplierId equals supplier.Id
                join currency in db.Set<Currency>().AsNoTracking() on price.CurrencyId equals currency.Id
                where price.EffectiveFrom <= filter.ToDate && (!price.EffectiveTo.HasValue || price.EffectiveTo.Value >= filter.FromDate)
                    && (!filter.SupplierId.HasValue || catalog.SupplierId == filter.SupplierId.Value)
                    && (!filter.ProductVariantId.HasValue || catalog.ProductVariantId == filter.ProductVariantId.Value)
                    && (!filter.CurrencyId.HasValue || price.CurrencyId == filter.CurrencyId.Value)
                    && !filter.WarehouseId.HasValue // catalog prices are supplier/product scoped, never warehouse scoped
                    && (search == null || search == "" || (catalog.SupplierProductCode != null && catalog.SupplierProductCode.Contains(search))
                        || (catalog.SupplierProductName != null && catalog.SupplierProductName.Contains(search)))
                select new { price.Id, price.EffectiveFrom, price.EffectiveTo, price.UnitPrice, price.IsCurrent,
                    catalog.SupplierId, supplier.NameAr, catalog.ProductVariantId, catalog.SupplierProductName,
                    catalog.SupplierProductCode, currency.Code };
            total = await prices.CountAsync(ct);
            var page = await prices.OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Id)
                .Skip(pageOffset).Take(filter.PageSize).ToListAsync(ct);
            rows.AddRange(page.Select(x => new PurchasingPerformanceRowDto {
                Kind = kind, DocumentId = x.Id, DocumentCode = x.SupplierProductCode ?? x.Id.ToString("N"),
                DocumentDate = x.EffectiveFrom, Status = x.IsCurrent ? "Current" : "Historical",
                SupplierId = x.SupplierId, SupplierName = x.NameAr, ProductVariantId = x.ProductVariantId,
                ProductName = x.SupplierProductName ?? "سعر المورد", CurrencyCode = x.Code,
                UnitPrice = x.UnitPrice
            }));
        }

        return new PurchasingPerformanceReportDto(filter with { Kind = kind }, summary,
            new PagedResult<PurchasingPerformanceRowDto> {
                Items = rows, PageNumber = filter.PageNumber, PageSize = filter.PageSize, TotalCount = total
            });
    }
}

public static class PurchasingPerformanceMath
{
    public static decimal OpenQuantity(decimal ordered, decimal accepted, bool closed) =>
        closed ? 0m : Math.Max(0m, ordered - accepted);

    public static decimal UninvoicedAccepted(decimal accepted, decimal invoiced, decimal preInvoiceReturned) =>
        Math.Max(0m, accepted - invoiced - preInvoiceReturned);

    /// <summary>An operational GRNI estimate, NOT the posted GRNI GL account balance.</summary>
    public static decimal UninvoicedReceiptCost(decimal receiptCost, decimal matchedReceiptCost,
        decimal unmatchedReturnCost) =>
        Math.Max(0m, Math.Round(receiptCost - matchedReceiptCost - unmatchedReturnCost, 4));

    public static decimal PurchasePriceVarianceBase(decimal invoiceMatchedNet, decimal matchedReceiptCost) =>
        Math.Round(invoiceMatchedNet - matchedReceiptCost, 4, MidpointRounding.AwayFromZero);

    public static decimal? OnTimeDeliveryPercent(decimal eligibleBaseQuantity, decimal deliveredOnTimeBaseQuantity) =>
        eligibleBaseQuantity <= 0m ? null :
        Math.Round(deliveredOnTimeBaseQuantity * 100m / eligibleBaseQuantity, 2, MidpointRounding.AwayFromZero);
}
