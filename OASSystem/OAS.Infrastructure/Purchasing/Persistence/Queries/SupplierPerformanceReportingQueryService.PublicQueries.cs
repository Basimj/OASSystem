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
    public async Task<PagedResult<SupplierPerformanceLineDto>> GetLinesAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        ValidateDates(request);
        var page = request.ToPageRequest();
        var asOf = ResolveAsOf(request);
        var baseQuery = ApplyOpenLateFilter(ApplyActivityWindowFilter(BuildSeedQuery(request), request), request, asOf);

        var total = await baseQuery.LongCountAsync(cancellationToken);
        var seeds = await ApplySort(baseQuery, page.SortBy, page.SortDirection)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var rows = await LoadRowsAsync(seeds, request, asOf, includeCosts, cancellationToken);

        return new PagedResult<SupplierPerformanceLineDto>
        {
            Items = rows,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = total
        };
    }

    public async Task<SupplierPerformanceDrillDownDto> GetDrillDownAsync(
        Guid purchaseOrderLineId,
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        var line = await (
            from poLine in _db.Set<PurchaseOrderLine>().AsNoTracking()
            join po in _db.Set<PurchaseOrder>().AsNoTracking() on poLine.PurchaseOrderId equals po.Id
            where poLine.Id == purchaseOrderLineId
            select new { PurchaseOrderId = po.Id, po.PurchaseOrderCode, PurchaseOrderLineId = poLine.Id }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrderLine), purchaseOrderLineId);

        ValidateDates(request);

        var receiptRows = await (
            from rl in _db.Set<PurchaseReceiptLine>().AsNoTracking()
            join r in _db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals r.Id
            where rl.PurchaseOrderLineId == purchaseOrderLineId && r.Status == PurchaseReceiptStatus.Posted
            orderby r.PostingDate descending, r.ReceiptCode descending
            select new { r.Id, r.ReceiptCode, r.PostingDate, r.Status, rl.BaseAcceptedQuantity, rl.TotalAcceptedCost, LineId = rl.Id })
            .ToListAsync(cancellationToken);
        var receipts = receiptRows.Select(x => new SupplierPerformanceDocumentDto(
            "PurchaseReceipt", x.Id, x.ReceiptCode, x.PostingDate, x.Status.ToString(), x.BaseAcceptedQuantity,
            includeCosts ? x.TotalAcceptedCost : null, x.LineId)).ToArray();

        var invoiceRows = await (
            from allocation in _db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
            join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on allocation.PurchaseReceiptLineId equals receiptLine.Id
            join invoiceLine in _db.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
            join invoice in _db.Set<PurchaseInvoice>().AsNoTracking() on invoiceLine.PurchaseInvoiceId equals invoice.Id
            where receiptLine.PurchaseOrderLineId == purchaseOrderLineId && invoice.Status == PurchaseInvoiceStatus.Posted
            orderby invoice.PostingDate descending, invoice.PurchaseInvoiceCode descending
            select new
            {
                invoice.Id, invoice.PurchaseInvoiceCode, invoice.PostingDate, invoice.Status,
                BaseQuantity = receiptLine.AcceptedQuantity <= 0m ? (decimal?)null
                    : allocation.MatchedQuantity * receiptLine.BaseAcceptedQuantity / receiptLine.AcceptedQuantity,
                BaseAmount = allocation.MatchedNetAmount * invoice.ExchangeRate,
                LineId = invoiceLine.Id
            }).ToListAsync(cancellationToken);
        var invoices = invoiceRows.Select(x => new SupplierPerformanceDocumentDto(
            "PurchaseInvoice", x.Id, x.PurchaseInvoiceCode, x.PostingDate, x.Status.ToString(), x.BaseQuantity,
            includeCosts ? x.BaseAmount : null, x.LineId)).ToArray();

        var returnRows = await (
            from returnLine in _db.Set<PurchaseReturnLine>().AsNoTracking()
            join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on returnLine.PurchaseReceiptLineId equals receiptLine.Id
            join purchaseReturn in _db.Set<PurchaseReturn>().AsNoTracking() on returnLine.PurchaseReturnId equals purchaseReturn.Id
            where receiptLine.PurchaseOrderLineId == purchaseOrderLineId
                  && purchaseReturn.Status == PurchaseReturnStatus.Posted
                  && returnLine.IsActive
            orderby purchaseReturn.PostingDate descending, purchaseReturn.ReturnCode descending
            select new
            {
                purchaseReturn.Id, purchaseReturn.ReturnCode, purchaseReturn.PostingDate, purchaseReturn.Status,
                returnLine.BaseQuantity, returnLine.SupplierGrossBaseAmount, LineId = returnLine.Id
            }).ToListAsync(cancellationToken);
        var returns = returnRows.Select(x => new SupplierPerformanceDocumentDto(
            "PurchaseReturn", x.Id, x.ReturnCode, x.PostingDate, x.Status.ToString(), x.BaseQuantity,
            includeCosts ? x.SupplierGrossBaseAmount : null, x.LineId)).ToArray();

        return new SupplierPerformanceDrillDownDto(
            line.PurchaseOrderId, line.PurchaseOrderLineId, line.PurchaseOrderCode, receipts, invoices, returns);
    }

    public async Task<PagedResult<SupplierPriceHistoryItemDto>> GetPriceHistoryAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        ValidateDates(request);
        var page = request.ToPageRequest();
        var query =
            from history in _db.Set<SupplierPriceHistory>().AsNoTracking()
            join catalog in _db.Set<SupplierCatalogItem>().AsNoTracking() on history.SupplierCatalogItemId equals catalog.Id
            join supplier in _db.Set<Supplier>().AsNoTracking() on catalog.SupplierId equals supplier.Id
            join currency in _db.Set<Currency>().AsNoTracking() on history.CurrencyId equals currency.Id
            where (!request.SupplierId.HasValue || catalog.SupplierId == request.SupplierId.Value)
                  && (!request.ProductVariantId.HasValue || catalog.ProductVariantId == request.ProductVariantId.Value)
                  && (!request.CurrencyId.HasValue || history.CurrencyId == request.CurrencyId.Value)
                  && (!request.FromDate.HasValue || history.EffectiveTo == null || history.EffectiveTo >= request.FromDate.Value)
                  && (!request.ToDate.HasValue || history.EffectiveFrom <= request.ToDate.Value)
            select new SupplierPriceHistoryItemDto(
                catalog.Id, supplier.Id, supplier.SupplierCode, supplier.NameAr, catalog.ProductVariantId,
                catalog.SupplierProductCode, catalog.SupplierProductName, currency.Id, currency.Code,
                includeCosts ? (decimal?)history.UnitPrice : null, history.EffectiveFrom, history.EffectiveTo, history.IsCurrent, catalog.LeadTimeDays);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.SupplierCode.Contains(search) || x.SupplierName.Contains(search)
                || (x.SupplierProductCode != null && x.SupplierProductCode.Contains(search))
                || (x.SupplierProductName != null && x.SupplierProductName.Contains(search)));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.SupplierCode)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<SupplierPriceHistoryItemDto>
        {
            Items = rows,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<SupplierPerformanceLineDto>> GetExportRowsAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        int maximumRows,
        CancellationToken cancellationToken = default)
    {
        if (maximumRows <= 0) throw new ArgumentOutOfRangeException(nameof(maximumRows));
        var first = await GetLinesAsync(request with { PageNumber = 1, PageSize = PageRequest.MaximumPageSize }, includeCosts, cancellationToken);
        if (first.TotalCount > maximumRows)
            throw new ConflictException("supplier_performance_export_too_large", $"التقرير يحتوي أكثر من {maximumRows} سجل. ضيّق نطاق المرشحات قبل التصدير.");

        var rows = new List<SupplierPerformanceLineDto>(checked((int)Math.Min(first.TotalCount, maximumRows)));
        rows.AddRange(first.Items);
        var pages = (int)Math.Ceiling(first.TotalCount / (double)PageRequest.MaximumPageSize);
        for (var page = 2; page <= pages; page++)
        {
            var batch = await GetLinesAsync(request with { PageNumber = page, PageSize = PageRequest.MaximumPageSize }, includeCosts, cancellationToken);
            rows.AddRange(batch.Items);
        }
        return rows;
    }
}
