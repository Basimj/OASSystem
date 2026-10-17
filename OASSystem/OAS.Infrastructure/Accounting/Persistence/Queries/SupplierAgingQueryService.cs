using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class SupplierAgingQueryService(OasDbContext db) : ISupplierAgingQueryService
{
    public async Task<SupplierAgingReportDto> GetAsync(
        SupplierAgingFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, PageRequest.MaximumPageSize);
        var cutoffUtc = DateTime.SpecifyKind(filter.AsOfDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var supplierQuery = db.Set<Supplier>().AsNoTracking();
        if (filter.SupplierId.HasValue)
            supplierQuery = supplierQuery.Where(x => x.Id == filter.SupplierId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            supplierQuery = supplierQuery.Where(x => x.SupplierCode.Contains(term) || x.NameAr.Contains(term));
        }
        var suppliers = await supplierQuery
            .Select(x => new SupplierMeta(x.Id, x.SupplierCode, x.NameAr))
            .ToListAsync(cancellationToken);
        var supplierIds = suppliers.Select(x => x.Id).ToArray();
        var supplierById = suppliers.ToDictionary(x => x.Id);

        if (supplierIds.Length == 0)
            return Empty(filter.AsOfDate, pageNumber, pageSize);

        var paymentByInvoice = await db.Set<PaymentAllocation>().AsNoTracking()
            .Where(x => x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice &&
                        supplierIds.Contains(db.Set<PurchaseInvoice>().Where(i => i.Id == x.TargetDocumentId).Select(i => i.SupplierId).FirstOrDefault()) &&
                        x.AllocatedAtUtc < cutoffUtc &&
                        (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.TargetDocumentId)
            .Select(g => new
            {
                InvoiceId = g.Key,
                Amount = g.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ?? x.AllocatedAmount * (x.ExchangeRate ?? 1m))
            })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, cancellationToken);

        var creditByInvoice = await db.Set<SupplierCreditApplication>().AsNoTracking()
            .Where(x => supplierIds.Contains(x.SupplierId) &&
                        x.AppliedAtUtc < cutoffUtc &&
                        (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.PurchaseInvoiceId)
            .Select(g => new { InvoiceId = g.Key, Amount = g.Sum(x => x.AppliedTargetBaseAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, cancellationToken);

        var invoiceRows = await db.Set<PurchaseInvoice>().AsNoTracking()
            .Where(x => supplierIds.Contains(x.SupplierId) &&
                        x.Status == PurchaseInvoiceStatus.Posted &&
                        x.PostingDate <= filter.AsOfDate)
            .Select(x => new
            {
                x.Id,
                x.PurchaseInvoiceCode,
                x.SupplierInvoiceCode,
                x.SupplierId,
                x.InvoiceDate,
                x.PostingDate,
                x.DueDate,
                x.DueDateProvenance,
                x.BaseTotalAmount,
                x.TotalAmount,
                CurrencyCode = x.CurrencyCodeSnapshot
            })
            .ToListAsync(cancellationToken);

        var openingApplied = await db.Set<SupplierOpeningApplication>().AsNoTracking()
            .Where(x => supplierIds.Contains(x.SupplierId) &&
                        x.AppliedAtUtc < cutoffUtc &&
                        (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.SupplierOpeningItemId)
            .Select(g => new { ItemId = g.Key, Amount = g.Sum(x => x.AppliedTargetBaseAmount) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Amount, cancellationToken);

        var openingRows = await (
            from item in db.Set<SupplierOpeningItem>().AsNoTracking()
            join batch in db.Set<SupplierOpeningBatch>().AsNoTracking() on item.BatchId equals batch.Id
            where supplierIds.Contains(item.SupplierId)
                  && batch.AsOfDate <= filter.AsOfDate
                  && (batch.Status == SupplierOpeningBatchStatus.Posted || batch.Status == SupplierOpeningBatchStatus.LinkedExistingJournal)
            select new
            {
                item.Id,
                batch.BatchCode,
                item.SupplierId,
                item.LegacyReference,
                item.DocumentDate,
                PostingDate = batch.AsOfDate,
                item.DueDate,
                item.DueDateProvenance,
                item.BaseOpenAmount,
                item.OriginalOpenAmount,
                item.CurrencyCodeSnapshot
            }).ToListAsync(cancellationToken);

        var lines = new List<SupplierAgingLineDto>(invoiceRows.Count + openingRows.Count);
        foreach (var invoice in invoiceRows)
        {
            paymentByInvoice.TryGetValue(invoice.Id, out var payments);
            creditByInvoice.TryGetValue(invoice.Id, out var credits);
            var outstanding = Round(Math.Max(0m, invoice.BaseTotalAmount - payments - credits));
            if (outstanding <= 0m) continue;
            var supplier = supplierById[invoice.SupplierId];
            lines.Add(BuildLine(
                "PurchaseInvoice", invoice.Id,
                string.IsNullOrWhiteSpace(invoice.SupplierInvoiceCode) ? invoice.PurchaseInvoiceCode : invoice.SupplierInvoiceCode!,
                supplier, invoice.InvoiceDate, invoice.PostingDate, invoice.DueDate,
                invoice.DueDateProvenance == PurchaseInvoiceDueDateProvenance.Unknown ? "Unknown" : invoice.DueDateProvenance.ToString(),
                invoice.BaseTotalAmount, outstanding, invoice.CurrencyCode, invoice.TotalAmount, filter.AsOfDate));
        }

        foreach (var opening in openingRows)
        {
            openingApplied.TryGetValue(opening.Id, out var applied);
            var outstanding = Round(Math.Max(0m, opening.BaseOpenAmount - applied));
            if (outstanding <= 0m) continue;
            var supplier = supplierById[opening.SupplierId];
            lines.Add(BuildLine(
                "SupplierOpening", opening.Id,
                opening.LegacyReference ?? opening.BatchCode,
                supplier, opening.DocumentDate, opening.PostingDate, opening.DueDate,
                opening.DueDateProvenance == SupplierOpeningDueDateProvenance.Unknown ? "Unknown" : opening.DueDateProvenance.ToString(),
                opening.BaseOpenAmount, outstanding, opening.CurrencyCodeSnapshot, opening.OriginalOpenAmount, filter.AsOfDate));
        }

        var unappliedPayments = await BuildUnappliedPaymentsAsync(supplierIds, cutoffUtc, filter.AsOfDate, cancellationToken);
        var unappliedCredits = await BuildUnappliedCreditsAsync(supplierIds, cutoffUtc, filter.AsOfDate, cancellationToken);

        var supplierSummaries = suppliers.Select(s =>
        {
            var own = lines.Where(x => x.SupplierId == s.Id).ToArray();
            unappliedPayments.TryGetValue(s.Id, out var payments);
            unappliedCredits.TryGetValue(s.Id, out var credits);
            return new SupplierAgingSupplierSummaryDto(
                s.Id, s.Code, s.Name,
                Sum(own, "NotDue"), Sum(own, "0-30"), Sum(own, "31-60"),
                Sum(own, "61-90"), Sum(own, "+90"), Sum(own, "Unknown"),
                Round(own.Sum(x => x.OutstandingBaseAmount)), Round(payments), Round(credits));
        }).Where(x => x.TotalOutstanding != 0m || x.UnappliedPayments != 0m || x.UnappliedCredits != 0m)
          .OrderBy(x => x.SupplierCode)
          .ToArray();

        var ordered = lines
            .OrderBy(x => x.SupplierCode)
            .ThenBy(x => x.DueDate ?? DateOnly.MaxValue)
            .ThenBy(x => x.PostingDate)
            .ThenBy(x => x.Reference)
            .ToArray();
        var totalCount = ordered.LongLength;
        var page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray();

        return new SupplierAgingReportDto(
            filter.AsOfDate, page, supplierSummaries,
            Sum(ordered, "NotDue"), Sum(ordered, "0-30"), Sum(ordered, "31-60"),
            Sum(ordered, "61-90"), Sum(ordered, "+90"), Sum(ordered, "Unknown"),
            Round(ordered.Sum(x => x.OutstandingBaseAmount)),
            Round(unappliedPayments.Values.Sum()), Round(unappliedCredits.Values.Sum()),
            totalCount, pageNumber, pageSize);
    }

    private async Task<Dictionary<Guid, decimal>> BuildUnappliedPaymentsAsync(
        Guid[] supplierIds, DateTime cutoffUtc, DateOnly asOfDate, CancellationToken ct)
    {
        var sourceRows = await (
            from line in db.Set<PaymentVoucherLine>().AsNoTracking()
            join voucher in db.Set<PaymentVoucher>().AsNoTracking() on line.PaymentVoucherId equals voucher.Id
            where line.SupplierId.HasValue && supplierIds.Contains(line.SupplierId.Value)
                  && voucher.Status == PaymentVoucherStatus.Posted && voucher.VoucherDate <= asOfDate
            select new
            {
                line.Id,
                SupplierId = line.SupplierId!.Value,
                BaseAmount = line.CounterpartyBaseAmount ?? line.BaseAmount ?? line.Amount * (line.ExchangeRate ?? 1m)
            }).ToListAsync(ct);

        var lineIds = sourceRows.Select(x => x.Id).ToArray();
        var regularApplied = await db.Set<PaymentAllocation>().AsNoTracking()
            .Where(x => x.PaymentVoucherLineId.HasValue && lineIds.Contains(x.PaymentVoucherLineId.Value)
                        && x.AllocatedAtUtc < cutoffUtc
                        && (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.PaymentVoucherLineId!.Value)
            .Select(g => new { LineId = g.Key, Amount = g.Sum(x => x.BaseAllocatedAmount ?? x.AllocatedAmount * (x.ExchangeRate ?? 1m)) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, ct);
        var openingApplied = await db.Set<SupplierOpeningApplication>().AsNoTracking()
            .Where(x => lineIds.Contains(x.PaymentVoucherLineId)
                        && x.AppliedAtUtc < cutoffUtc
                        && (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.PaymentVoucherLineId)
            .Select(g => new { LineId = g.Key, Amount = g.Sum(x => x.AppliedSourceBaseAmount) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, ct);

        return sourceRows.GroupBy(x => x.SupplierId).ToDictionary(g => g.Key, g =>
            Round(g.Sum(x =>
            {
                regularApplied.TryGetValue(x.Id, out var regular);
                openingApplied.TryGetValue(x.Id, out var opening);
                return Math.Max(0m, x.BaseAmount - regular - opening);
            })));
    }

    private async Task<Dictionary<Guid, decimal>> BuildUnappliedCreditsAsync(
        Guid[] supplierIds, DateTime cutoffUtc, DateOnly asOfDate, CancellationToken ct)
    {
        var returns = await db.Set<PurchaseReturn>().AsNoTracking()
            .Where(x => supplierIds.Contains(x.SupplierId)
                        && x.Status == PurchaseReturnStatus.Posted
                        && x.PostingDate <= asOfDate
                        && x.PurchaseInvoiceId.HasValue
                        && x.SupplierGrossBaseAmount > 0m)
            .Select(x => new { x.Id, x.SupplierId, x.SupplierGrossBaseAmount })
            .ToListAsync(ct);
        var returnIds = returns.Select(x => x.Id).ToArray();
        var applied = await db.Set<SupplierCreditApplication>().AsNoTracking()
            .Where(x => returnIds.Contains(x.PurchaseReturnId)
                        && x.AppliedAtUtc < cutoffUtc
                        && (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.PurchaseReturnId)
            .Select(g => new { ReturnId = g.Key, Amount = g.Sum(x => x.AppliedSourceBaseAmount) })
            .ToDictionaryAsync(x => x.ReturnId, x => x.Amount, ct);
        return returns.GroupBy(x => x.SupplierId).ToDictionary(g => g.Key, g =>
            Round(g.Sum(x =>
            {
                applied.TryGetValue(x.Id, out var used);
                return Math.Max(0m, x.SupplierGrossBaseAmount - used);
            })));
    }

    private static SupplierAgingLineDto BuildLine(
        string sourceType, Guid sourceId, string reference, SupplierMeta supplier,
        DateOnly documentDate, DateOnly postingDate, DateOnly? dueDate, string provenance,
        decimal originalBase, decimal outstanding, string? currencyCode, decimal? documentAmount, DateOnly asOfDate)
    {
        var needsReview = !dueDate.HasValue || string.Equals(provenance, "Unknown", StringComparison.OrdinalIgnoreCase);
        int? days = dueDate.HasValue ? asOfDate.DayNumber - dueDate.Value.DayNumber : null;
        var bucket = needsReview ? "Unknown"
            : days < 0 ? "NotDue"
            : days <= 30 ? "0-30"
            : days <= 60 ? "31-60"
            : days <= 90 ? "61-90"
            : "+90";
        return new SupplierAgingLineDto(
            sourceType, sourceId, reference, supplier.Id, supplier.Code, supplier.Name,
            documentDate, postingDate, dueDate, provenance, needsReview, days, bucket,
            Round(originalBase), Round(outstanding), currencyCode, documentAmount);
    }

    private static decimal Sum(IEnumerable<SupplierAgingLineDto> rows, string bucket) =>
        Round(rows.Where(x => x.Bucket == bucket).Sum(x => x.OutstandingBaseAmount));
    private static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
    private static SupplierAgingReportDto Empty(DateOnly asOfDate, int pageNumber, int pageSize) =>
        new(asOfDate, [], [], 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0, pageNumber, pageSize);
    private sealed record SupplierMeta(Guid Id, string Code, string Name);
}
