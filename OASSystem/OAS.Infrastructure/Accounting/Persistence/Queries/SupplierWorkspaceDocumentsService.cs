using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

// Read-only drill-down projections; never creates or changes accounting entries.
public sealed class SupplierWorkspaceDocumentsService(OasDbContext db) : ISupplierWorkspaceDocumentsService
{
    public async Task<SupplierWorkspaceDocumentsDto> GetAsync(
        SupplierWorkspaceDocumentsFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.SupplierId == Guid.Empty)
            throw new ArgumentException("Supplier id is required.", nameof(filter));
        if (filter.FromDate > filter.ToDate)
            throw new ConflictException("supplier_workspace_invalid_dates", "تاريخ البداية لا يمكن أن يلي النهاية.");

        var exists = await db.Set<Supplier>().AsNoTracking()
            .AnyAsync(x => x.Id == filter.SupplierId, cancellationToken);
        if (!exists)
            throw new NotFoundException(nameof(Supplier), filter.SupplierId);

        var page = Math.Max(1, filter.PageNumber);
        var size = Math.Clamp(filter.PageSize, 1, PageRequest.MaximumPageSize);
        var type = filter.DocumentType?.Trim() ?? "";
        var search = filter.Search?.Trim();
        var currency = filter.CurrencyCode?.Trim().ToUpperInvariant();
        var id = filter.SupplierId;
        var from = filter.FromDate;
        var to = filter.ToDate;

        IQueryable<SupplierWorkspaceDocumentRowDto> query;
        switch (type)
        {
            case "PurchaseOrder":
            {
                var orders = db.Set<PurchaseOrder>().AsNoTracking()
                    .Where(x => x.SupplierId == id && x.OrderDate >= from && x.OrderDate <= to);
                if (!string.IsNullOrWhiteSpace(search))
                    orders = orders.Where(x => x.PurchaseOrderCode.Contains(search));
                query = from o in orders
                        join c in db.Set<Currency>().AsNoTracking() on o.CurrencyId equals c.Id
                        where currency == null || currency == "" || c.Code == currency
                        select new SupplierWorkspaceDocumentRowDto(o.Id, type, o.PurchaseOrderCode,
                            o.OrderDate, (byte)o.Status, o.TotalAmount, null, c.Code, null);
                break;
            }
            case "PurchaseReceipt":
            {
                var receipts = db.Set<PurchaseReceipt>().AsNoTracking()
                    .Where(x => x.SupplierId == id && x.ReceiptDate >= from && x.ReceiptDate <= to);
                if (!string.IsNullOrWhiteSpace(search))
                    receipts = receipts.Where(x => x.ReceiptCode.Contains(search));
                // Receipt values use the purchase-order currency and are not an AP/GL balance.
                query = from r in receipts
                        join o in db.Set<PurchaseOrder>().AsNoTracking()
                            on r.PurchaseOrderId equals o.Id
                        join c in db.Set<Currency>().AsNoTracking() on o.CurrencyId equals c.Id
                        where currency == null || currency == "" || c.Code == currency
                        select new SupplierWorkspaceDocumentRowDto(r.Id, type, r.ReceiptCode,
                            r.ReceiptDate, (byte)r.Status, null, null, c.Code, r.JournalEntryId);
                break;
            }
            case "PurchaseInvoice":
            {
                var invoices = db.Set<PurchaseInvoice>().AsNoTracking()
                    .Where(x => x.SupplierId == id && x.InvoiceDate >= from && x.InvoiceDate <= to);
                if (!string.IsNullOrWhiteSpace(search))
                    invoices = invoices.Where(x => x.PurchaseInvoiceCode.Contains(search) ||
                        (x.SupplierInvoiceCode != null && x.SupplierInvoiceCode.Contains(search)));
                if (!string.IsNullOrWhiteSpace(currency))
                    invoices = invoices.Where(x => x.CurrencyCodeSnapshot == currency);
                query = invoices.Select(x => new SupplierWorkspaceDocumentRowDto(
                    x.Id, type, x.PurchaseInvoiceCode, x.InvoiceDate, (byte)x.Status,
                    x.TotalAmount, x.BaseTotalAmount, x.CurrencyCodeSnapshot, x.JournalEntryId));
                break;
            }
            case "PurchaseReturn":
            {
                var returns = db.Set<PurchaseReturn>().AsNoTracking()
                    .Where(x => x.SupplierId == id && x.ReturnDate >= from && x.ReturnDate <= to);
                if (!string.IsNullOrWhiteSpace(search))
                    returns = returns.Where(x => x.ReturnCode.Contains(search));
                // Return has verified base totals, but those are not outstanding AP.
                query = returns.Select(x => new SupplierWorkspaceDocumentRowDto(
                    x.Id, type, x.ReturnCode, x.ReturnDate, (byte)x.Status, null,
                    x.SupplierGrossBaseAmount, null, x.JournalEntryId));
                if (!string.IsNullOrWhiteSpace(currency))
                    query = query.Where(_ => false); // No original-currency snapshot in return.
                break;
            }
            case "PaymentVoucher":
            {
                var lines =
                    from l in db.Set<PaymentVoucherLine>().AsNoTracking()
                    join v in db.Set<PaymentVoucher>().AsNoTracking() on l.PaymentVoucherId equals v.Id
                    where l.SupplierId == id && v.VoucherDate >= from && v.VoucherDate <= to
                    select new { Line = l, Voucher = v };
                if (!string.IsNullOrWhiteSpace(search))
                    lines = lines.Where(x => x.Voucher.VoucherNumber.Contains(search) ||
                        (x.Line.ReferenceNumber != null && x.Line.ReferenceNumber.Contains(search)));
                if (!string.IsNullOrWhiteSpace(currency))
                    lines = lines.Where(x => x.Line.CurrencyCodeSnapshot == currency);
                // Scope to voucher LINES. A voucher may contain several suppliers.
                query = lines.Select(x => new SupplierWorkspaceDocumentRowDto(
                    x.Voucher.Id, type, x.Voucher.VoucherNumber, x.Voucher.VoucherDate,
                    (byte)x.Voucher.Status, x.Line.Amount,
                    x.Line.CounterpartyBaseAmount ?? x.Line.BaseAmount, x.Line.CurrencyCodeSnapshot,
                    x.Voucher.JournalEntryId, x.Line.Id));
                break;
            }
            default:
                throw new ConflictException("supplier_workspace_document_type_invalid",
                    "اختر نوع مستند صحيحًا.");
        }

        var count = await query.LongCountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.DocumentDate)
            .ThenBy(x => x.Reference).ThenBy(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);

        if (type == "PaymentVoucher" && rows.Count > 0)
            rows = await AddPaymentUnappliedAsync(rows, filter.ToDate, cancellationToken);

        return new SupplierWorkspaceDocumentsDto(id, type, rows, count, page, size);
    }

    private async Task<List<SupplierWorkspaceDocumentRowDto>> AddPaymentUnappliedAsync(
        List<SupplierWorkspaceDocumentRowDto> rows, DateOnly asOf, CancellationToken ct)
    {
        var ids = rows.Where(x => x.PaymentVoucherLineId.HasValue)
            .Select(x => x.PaymentVoucherLineId!.Value).ToArray();
        var cutoffUtc = DateTime.SpecifyKind(asOf.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var allocations = await db.Set<PaymentAllocation>().AsNoTracking()
            .Where(x => x.PaymentVoucherLineId.HasValue && ids.Contains(x.PaymentVoucherLineId.Value) &&
                x.AllocatedAtUtc < cutoffUtc &&
                (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.PaymentVoucherLineId!.Value)
            .Select(g => new { Id = g.Key, Amount = g.Sum(x =>
                x.BaseAllocatedAmount ?? x.AllocatedAmount * (x.ExchangeRate ?? 1m)) })
            .ToDictionaryAsync(x => x.Id, x => x.Amount, ct);
        var opening = await db.Set<SupplierOpeningApplication>().AsNoTracking()
            .Where(x => ids.Contains(x.PaymentVoucherLineId) && x.AppliedAtUtc < cutoffUtc &&
                (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoffUtc))
            .GroupBy(x => x.PaymentVoucherLineId)
            .Select(g => new { Id = g.Key, Amount = g.Sum(x => x.AppliedSourceBaseAmount) })
            .ToDictionaryAsync(x => x.Id, x => x.Amount, ct);

        return rows.Select(x =>
        {
            if (!x.PaymentVoucherLineId.HasValue || !x.BaseAmount.HasValue ||
                x.Status != (byte)PaymentVoucherStatus.Posted)
                return x;
            allocations.TryGetValue(x.PaymentVoucherLineId.Value, out var regular);
            opening.TryGetValue(x.PaymentVoucherLineId.Value, out var openingAmount);
            return x with { UnappliedPaymentBaseAmount = Math.Max(0m,
                Math.Round(x.BaseAmount.Value - regular - openingAmount, 4)) };
        }).ToList();
    }
}
