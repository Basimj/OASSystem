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
    private async Task<List<SupplierPerformanceLineDto>> LoadRowsAsync(
        IReadOnlyList<LineSeed> seeds,
        SupplierPerformanceQueryRequest request,
        DateOnly asOf,
        bool includeCosts,
        CancellationToken ct)
    {
        if (seeds.Count == 0) return [];
        var ids = seeds.Select(x => x.PurchaseOrderLineId).ToArray();

        var periodReceipts = await (
            from rl in _db.Set<PurchaseReceiptLine>().AsNoTracking()
            join r in _db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals r.Id
            where ids.Contains(rl.PurchaseOrderLineId) && r.Status == PurchaseReceiptStatus.Posted
                  && (!request.FromDate.HasValue || r.PostingDate >= request.FromDate.Value)
                  && (!request.ToDate.HasValue || r.PostingDate <= request.ToDate.Value)
            select new ReceiptActivity
            {
                PurchaseOrderLineId = rl.PurchaseOrderLineId,
                PurchaseReceiptId = r.Id,
                ReceiptCode = r.ReceiptCode,
                ReceiptDate = r.ReceiptDate,
                PostingDate = r.PostingDate,
                BaseAcceptedQuantity = rl.BaseAcceptedQuantity,
                AcceptedQuantity = rl.AcceptedQuantity,
                ActualUnitCost = rl.ActualUnitCost,
                TotalAcceptedCost = rl.TotalAcceptedCost
            }).ToListAsync(ct);

        var acceptedAsOf = await (
            from rl in _db.Set<PurchaseReceiptLine>().AsNoTracking()
            join r in _db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals r.Id
            where ids.Contains(rl.PurchaseOrderLineId) && r.Status == PurchaseReceiptStatus.Posted && r.PostingDate <= asOf
            group rl by rl.PurchaseOrderLineId into g
            select new { Id = g.Key, Quantity = g.Sum(x => x.BaseAcceptedQuantity) }).ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);

        var returns = await (
            from returnLine in _db.Set<PurchaseReturnLine>().AsNoTracking()
            join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on returnLine.PurchaseReceiptLineId equals receiptLine.Id
            join pr in _db.Set<PurchaseReturn>().AsNoTracking() on returnLine.PurchaseReturnId equals pr.Id
            where ids.Contains(receiptLine.PurchaseOrderLineId) && pr.Status == PurchaseReturnStatus.Posted && returnLine.IsActive
                  && (!request.FromDate.HasValue || pr.PostingDate >= request.FromDate.Value)
                  && (!request.ToDate.HasValue || pr.PostingDate <= request.ToDate.Value)
            select new ReturnActivity
            {
                PurchaseOrderLineId = receiptLine.PurchaseOrderLineId,
                PurchaseReturnId = pr.Id,
                ReturnCode = pr.ReturnCode,
                PostingDate = pr.PostingDate,
                PurchaseInvoiceLineId = returnLine.PurchaseInvoiceLineId,
                BaseQuantity = returnLine.BaseQuantity,
                ReceiptCostBaseAmount = returnLine.ReceiptCostBaseAmount,
                SupplierGrossBaseAmount = returnLine.SupplierGrossBaseAmount
            }).ToListAsync(ct);

        var invoices = await (
            from allocation in _db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
            join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on allocation.PurchaseReceiptLineId equals receiptLine.Id
            join invoiceLine in _db.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
            join invoice in _db.Set<PurchaseInvoice>().AsNoTracking() on invoiceLine.PurchaseInvoiceId equals invoice.Id
            where ids.Contains(receiptLine.PurchaseOrderLineId) && invoice.Status == PurchaseInvoiceStatus.Posted
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
            }).ToListAsync(ct);

        var grniByLine = includeCosts ? await CalculateGrniByLineAsOfAsync(ids, asOf, ct) : [];
        var result = new List<SupplierPerformanceLineDto>(seeds.Count);
        foreach (var seed in seeds)
        {
            var receipts = periodReceipts.Where(x => x.PurchaseOrderLineId == seed.PurchaseOrderLineId).ToArray();
            var lineReturns = returns.Where(x => x.PurchaseOrderLineId == seed.PurchaseOrderLineId).ToArray();
            var lineInvoices = invoices.Where(x => x.PurchaseOrderLineId == seed.PurchaseOrderLineId).ToArray();
            var receivedQty = receipts.Sum(x => x.BaseAcceptedQuantity);
            var returnedQty = lineReturns.Sum(x => x.BaseQuantity);
            var acceptedTotal = acceptedAsOf.GetValueOrDefault(seed.PurchaseOrderLineId);
            var open = SupplierPerformanceMetrics.OpenBaseQuantity(
                seed.BaseQuantity, acceptedTotal,
                seed.Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived));

            var lead = SupplierPerformanceMetrics.WeightedAverageDays(receipts.Select(x =>
                (x.BaseAcceptedQuantity, (decimal)(x.ReceiptDate.DayNumber - seed.OrderDate.DayNumber))));
            var onTime = seed.ExpectedDeliveryDate.HasValue
                ? SupplierPerformanceMetrics.WeightedPercent(receipts.Select(x =>
                    (x.BaseAcceptedQuantity, x.ReceiptDate <= seed.ExpectedDeliveryDate.Value)))
                : null;
            var matchedReceiptCost = lineInvoices.Sum(x => x.ReceiptAcceptedQuantity <= 0m
                ? 0m
                : x.MatchedQuantity * x.ReceiptBaseAcceptedQuantity / x.ReceiptAcceptedQuantity * x.ReceiptActualUnitCost);
            var invoiceNet = lineInvoices.Sum(x => x.MatchedNetAmount * x.ExchangeRate);

            var latestReceipt = receipts.OrderByDescending(x => x.PostingDate).ThenByDescending(x => x.ReceiptCode).FirstOrDefault();
            var latestInvoice = lineInvoices.OrderByDescending(x => x.PostingDate).ThenByDescending(x => x.PurchaseInvoiceCode).FirstOrDefault();
            var latestReturn = lineReturns.OrderByDescending(x => x.PostingDate).ThenByDescending(x => x.ReturnCode).FirstOrDefault();

            result.Add(new SupplierPerformanceLineDto(
                seed.SupplierId, seed.SupplierCode, seed.SupplierName,
                seed.WarehouseId, seed.WarehouseCode, seed.WarehouseName,
                seed.CurrencyId, seed.CurrencyCode,
                seed.ProductVariantId, seed.ProductCode, seed.ProductName,
                seed.PurchaseOrderId, seed.PurchaseOrderLineId, seed.PurchaseOrderCode, seed.Status.ToString(),
                seed.OrderDate, seed.ExpectedDeliveryDate,
                seed.OrderedQuantity, seed.BaseQuantity, IsInWindow(seed.OrderDate, request) ? seed.BaseQuantity : 0m,
                Math.Round(receivedQty, 3), Math.Round(returnedQty, 3),
                SupplierPerformanceMetrics.NetReceivedBaseQuantity(receivedQty, returnedQty),
                open, SupplierPerformanceMetrics.IsLate(open, seed.ExpectedDeliveryDate, asOf)
                    || SupplierPerformanceMetrics.WasDeliveredLate(seed.ExpectedDeliveryDate, receipts.Select(x => x.ReceiptDate)),
                receipts.Length == 0 ? null : receipts.Min(x => x.ReceiptDate),
                receipts.Length == 0 ? null : receipts.Max(x => x.ReceiptDate),
                lead, onTime,
                includeCosts && IsInWindow(seed.OrderDate, request) ? Math.Round(seed.NetAmount * seed.ExchangeRate, 4) : includeCosts ? 0m : null,
                includeCosts ? Math.Round(receipts.Sum(x => x.TotalAcceptedCost), 4) : null,
                includeCosts ? Math.Round(invoiceNet, 4) : null,
                includeCosts ? Math.Round(lineInvoices.Sum(x => x.InvoiceLineQuantity <= 0m ? 0m : x.BaseFinalAmount * x.MatchedQuantity / x.InvoiceLineQuantity), 4) : null,
                includeCosts ? Math.Round(lineReturns.Sum(x => x.SupplierGrossBaseAmount), 4) : null,
                includeCosts ? grniByLine.GetValueOrDefault(seed.PurchaseOrderLineId) : null,
                includeCosts ? SupplierPerformanceMetrics.PurchasePriceVarianceBase(invoiceNet, matchedReceiptCost) : null,
                latestReceipt?.PurchaseReceiptId, latestReceipt?.ReceiptCode,
                latestInvoice?.PurchaseInvoiceId, latestInvoice?.PurchaseInvoiceCode,
                latestReturn?.PurchaseReturnId, latestReturn?.ReturnCode));
        }
        return result;
    }

    private async Task<decimal> CalculateGrniAsOfAsync(IQueryable<LineSeed> baseQuery, DateOnly asOf, CancellationToken ct)
    {
        var ids = await baseQuery.Select(x => x.PurchaseOrderLineId).ToListAsync(ct);
        if (ids.Count == 0) return 0m;
        decimal total = 0m;
        foreach (var chunk in ids.Chunk(500))
        {
            var map = await CalculateGrniByLineAsOfAsync(chunk, asOf, ct);
            total += map.Values.Sum();
        }
        return Math.Round(total, 4, MidpointRounding.AwayFromZero);
    }

    private async Task<Dictionary<Guid, decimal>> CalculateGrniByLineAsOfAsync(
        IReadOnlyCollection<Guid> lineIds,
        DateOnly asOf,
        CancellationToken ct)
    {
        if (lineIds.Count == 0) return [];
        var ids = lineIds.ToArray();
        var receiptCosts = await (
            from rl in _db.Set<PurchaseReceiptLine>().AsNoTracking()
            join r in _db.Set<PurchaseReceipt>().AsNoTracking() on rl.PurchaseReceiptId equals r.Id
            where ids.Contains(rl.PurchaseOrderLineId) && r.Status == PurchaseReceiptStatus.Posted && r.PostingDate <= asOf
            group rl by rl.PurchaseOrderLineId into g
            select new { Id = g.Key, Amount = g.Sum(x => x.TotalAcceptedCost) }).ToDictionaryAsync(x => x.Id, x => x.Amount, ct);

        var matchedCosts = await (
            from allocation in _db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
            join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on allocation.PurchaseReceiptLineId equals receiptLine.Id
            join invoiceLine in _db.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
            join invoice in _db.Set<PurchaseInvoice>().AsNoTracking() on invoiceLine.PurchaseInvoiceId equals invoice.Id
            where ids.Contains(receiptLine.PurchaseOrderLineId) && invoice.Status == PurchaseInvoiceStatus.Posted && invoice.PostingDate <= asOf
            let amount = receiptLine.AcceptedQuantity <= 0m
                ? 0m
                : allocation.MatchedQuantity * receiptLine.BaseAcceptedQuantity / receiptLine.AcceptedQuantity * receiptLine.ActualUnitCost
            group amount by receiptLine.PurchaseOrderLineId into g
            select new { Id = g.Key, Amount = g.Sum() }).ToDictionaryAsync(x => x.Id, x => x.Amount, ct);

        var unmatchedReturnCosts = await (
            from returnLine in _db.Set<PurchaseReturnLine>().AsNoTracking()
            join receiptLine in _db.Set<PurchaseReceiptLine>().AsNoTracking() on returnLine.PurchaseReceiptLineId equals receiptLine.Id
            join pr in _db.Set<PurchaseReturn>().AsNoTracking() on returnLine.PurchaseReturnId equals pr.Id
            where ids.Contains(receiptLine.PurchaseOrderLineId)
                  && pr.Status == PurchaseReturnStatus.Posted && pr.PostingDate <= asOf
                  && returnLine.IsActive && returnLine.PurchaseInvoiceLineId == null
            group returnLine by receiptLine.PurchaseOrderLineId into g
            select new { Id = g.Key, Amount = g.Sum(x => x.ReceiptCostBaseAmount) }).ToDictionaryAsync(x => x.Id, x => x.Amount, ct);

        return ids.ToDictionary(
            id => id,
            id => SupplierPerformanceMetrics.GrniBaseAmount(
                receiptCosts.GetValueOrDefault(id), matchedCosts.GetValueOrDefault(id), unmatchedReturnCosts.GetValueOrDefault(id)));
    }
}
