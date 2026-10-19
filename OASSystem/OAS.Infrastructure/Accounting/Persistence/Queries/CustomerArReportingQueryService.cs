using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Reports.Services;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

/// <summary>
/// SA-04 read-only projections. Posted GL is the authority for customer statements,
/// NEVER a union of documents or all lines carrying CustomerId (cash/bank can carry it).
/// As-of invoice balances are read from immutable source snapshots and effective
/// settlement timestamps; no posted journal or invoice is modified.
/// </summary>
public sealed class CustomerArReportingQueryService(OasDbContext db) : ICustomerArReportingQueryService
{
    private static decimal R(decimal amount) => CustomerArMath.Round(amount);

    private async Task<string> BaseCurrencyAsync(CancellationToken ct)
    {
        var id = await db.Set<AccountingSettings>().AsNoTracking()
            .Where(x => x.Id == AccountingSettings.SingletonId)
            .Select(x => x.BaseCurrencyId).SingleOrDefaultAsync(ct);
        return await db.Set<Currency>().AsNoTracking()
            .Where(x => x.Id == id).Select(x => x.Code).SingleOrDefaultAsync(ct) ?? "BASE";
    }

    public async Task<CustomerStatementReportDto> StatementAsync(
        Guid customerId, DateOnly from, DateOnly to, int pageNumber = 1,
        int pageSize = 50, CancellationToken ct = default)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer required", nameof(customerId));
        if (to < from) throw new ArgumentException("To date must not precede from date.");
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var customer = await (from c in db.Set<Customer>().AsNoTracking()
                              join a in db.Set<Account>().AsNoTracking() on c.AccountId equals a.Id
                              where c.Id == customerId
                              select new { c.Id, c.CustomerCode, c.NameAr, c.AccountId, a.Code })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(Customer), customerId);

        var ledger = from l in db.Set<JournalEntryLine>().AsNoTracking()
                     join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
                     where l.AccountId == customer.AccountId && j.Status == JournalEntryStatus.Posted
                          && j.PostingDate <= to
                     select new
                     {
                         LineId = l.Id, l.LineNumber, l.DebitAmount, l.CreditAmount,
                         LineDescription = l.Description, l.TransactionCurrencyCodeSnapshot,
                         l.TransactionDebitAmount, l.TransactionCreditAmount,
                         JournalId = j.Id, j.JournalNumber, j.PostingDate, j.DocumentDate,
                         JournalDescription = j.Description, j.SourceDocumentType,
                         j.SourceDocumentId, j.ReversedJournalId
                     };

        var opening = await ledger.Where(x => x.PostingDate < from)
            .SumAsync(x => x.DebitAmount - x.CreditAmount, ct);
        var movementsQuery = ledger.Where(x => x.PostingDate >= from);
        var debit = await movementsQuery.SumAsync(x => x.DebitAmount, ct);
        var credit = await movementsQuery.SumAsync(x => x.CreditAmount, ct);
        var total = await movementsQuery.LongCountAsync(ct);

        var chronological = movementsQuery.OrderBy(x => x.PostingDate)
            .ThenBy(x => x.JournalNumber).ThenBy(x => x.LineNumber).ThenBy(x => x.LineId);
        // Running balance starts at the real chronological offset, not the filtered page total.
        var skip = (long)(pageNumber - 1) * pageSize;
        var before = skip == 0 ? 0m : await chronological.Take((int)Math.Min(skip, int.MaxValue))
            .SumAsync(x => x.DebitAmount - x.CreditAmount, ct);
        var rows = await chronological.Skip((int)Math.Min(skip, int.MaxValue)).Take(pageSize).ToListAsync(ct);
        var balance = R(opening + before);
        var page = new List<CustomerStatementMovementDto>(rows.Count);
        foreach (var row in rows)
        {
            balance = R(balance + row.DebitAmount - row.CreditAmount);
            page.Add(new CustomerStatementMovementDto(
                row.JournalId, row.JournalNumber, row.LineNumber,
                row.PostingDate, row.DocumentDate,
                row.LineDescription ?? row.JournalDescription,
                row.SourceDocumentType, row.SourceDocumentId,
                R(row.DebitAmount), R(row.CreditAmount), balance,
                row.TransactionCurrencyCodeSnapshot, row.TransactionDebitAmount,
                row.TransactionCreditAmount, row.ReversedJournalId.HasValue));
        }
        return new CustomerStatementReportDto(customer.Id, customer.CustomerCode,
            customer.NameAr, customer.AccountId, customer.Code, from, to,
            await BaseCurrencyAsync(ct), R(opening), R(debit), R(credit),
            R(opening + debit - credit), page, total, pageNumber, pageSize);
    }

    public async Task<CustomerBalancesReportDto> BalancesAsync(DateOnly asOf,
        string? search = null, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        // Query customer metadata independently of the ledger. In particular, avoid
        // materializing a non-nullable decimal from an aggregate LEFT JOIN whose
        // debit/credit columns are NULL when the customer has no posted entries.
        var customers = from c in db.Set<Customer>().AsNoTracking()
                        join a in db.Set<Account>().AsNoTracking() on c.AccountId equals a.Id
                        select new { c.Id, c.CustomerCode, c.NameAr, c.AccountId, a.Code };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            customers = customers.Where(x =>
                x.CustomerCode.Contains(term) || x.NameAr.Contains(term) || x.Code.Contains(term));
        }

        var count = await customers.LongCountAsync(ct);
        var page = await customers.OrderBy(x => x.CustomerCode).ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var visibleIds = page.Select(x => x.Id).ToHashSet();
        var pageBalances = new Dictionary<Guid, (decimal Debit, decimal Credit)>();

        // One simple server-side GROUP BY, without nesting GROUP BY over a LEFT JOIN.
        // Customers without journal entries are absent from this query and have a
        // zero balance. Stream the per-customer aggregates without buffering all
        // customers or ledger lines in memory; cache only the current display page.
        var postedCustomerBalances =
            from c in customers
            join l in db.Set<JournalEntryLine>().AsNoTracking() on c.AccountId equals l.AccountId
            join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
            where j.Status == JournalEntryStatus.Posted && j.PostingDate <= asOf
            group l by c.Id into g
            select new
            {
                CustomerId = g.Key,
                Debit = g.Sum(x => x.DebitAmount),
                Credit = g.Sum(x => x.CreditAmount)
            };

        decimal debitBalances = 0m;
        decimal creditBalances = 0m;
        await foreach (var balance in postedCustomerBalances.AsAsyncEnumerable().WithCancellation(ct))
        {
            var signed = balance.Debit - balance.Credit;
            if (signed >= 0m)
                debitBalances += signed;
            else
                creditBalances -= signed;

            if (visibleIds.Contains(balance.CustomerId))
                pageBalances[balance.CustomerId] = (balance.Debit, balance.Credit);
        }

        return new CustomerBalancesReportDto(asOf, await BaseCurrencyAsync(ct),
            page.Select(x =>
            {
                // No GL entries = zero debit and credit, not NULL.
                var balance = pageBalances.GetValueOrDefault(x.Id);
                return new CustomerBalanceRowDto(x.Id, x.CustomerCode, x.NameAr,
                    x.AccountId, x.Code, R(balance.Debit), R(balance.Credit),
                    R(balance.Debit - balance.Credit));
            }).ToArray(),
            R(debitBalances), R(creditBalances), R(debitBalances - creditBalances),
            count, pageNumber, pageSize);
    }

    /// <summary>
    /// Compute as-of AR aging in bounded batches. Avoid composing an EF GroupBy/LeftJoin
    /// projection into additional WHERE/SUM/COUNT queries: EF Core 9 cannot translate
    /// those predicates for every SQL Server provider/version combination.
    /// Each aggregate below remains server-side SQL; only the final arithmetic on
    /// a 500-invoice batch runs in memory. No tracking, no mutable balances.
    /// </summary>
    public async Task<CustomerAgingReportDto> AgingAsync(DateOnly asOf, Guid? customerId = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var cutoff = DateTime.SpecifyKind(asOf.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        const int batchSize = 500;
        var pageOffset = ((long)pageNumber - 1L) * pageSize;
        var invoiceOffset = 0;
        long count = 0;
        decimal notDue = 0m, days30 = 0m, days60 = 0m, days90 = 0m, over90 = 0m, unknown = 0m;
        var rows = new List<CustomerAgingLineDto>(pageSize);

        // Stable ordering is essential: page N must contain the same set of *open*
        // invoices regardless of how many settled invoices precede it.
        var baseInvoices =
            from i in db.Set<SalesInvoice>().AsNoTracking()
            join c in db.Set<Customer>().AsNoTracking() on i.CustomerId equals c.Id
            where i.Status == SalesInvoiceStatus.Posted && i.PostingDate <= asOf &&
                (!customerId.HasValue || i.CustomerId == customerId.Value)
            orderby i.DueDate, i.InvoiceCode, i.Id
            select new
            {
                i.Id, i.CustomerId, c.CustomerCode, c.NameAr, i.InvoiceCode,
                i.InvoiceDate, i.PostingDate, i.DueDate, i.CurrencyCodeSnapshot,
                Original = i.BaseTotalAmount
            };

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var batch = await baseInvoices.Skip(invoiceOffset).Take(batchSize).ToListAsync(ct);
            if (batch.Count == 0) break;
            invoiceOffset = checked(invoiceOffset + batch.Count);
            var invoiceIds = batch.Select(x => x.Id).ToArray();

            // Independent SQL GROUP BYs are fully translatable. In particular we do
            // not LeftJoin these groups back into an IQueryable then filter by a
            // computed field (the source of the InvalidOperationException).
            var payments = await db.Set<PaymentAllocation>().AsNoTracking()
                .Where(x => invoiceIds.Contains(x.TargetDocumentId) &&
                    x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice &&
                    x.AllocatedAtUtc < cutoff &&
                    (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoff))
                .GroupBy(x => x.TargetDocumentId)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    Amount = g.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ??
                                        (x.AllocatedAmount * (x.ExchangeRate ?? 1m)))
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, ct);

            var postedReturns = await db.Set<SalesReturn>().AsNoTracking()
                .Where(x => invoiceIds.Contains(x.SalesInvoiceId) &&
                    x.Status == SalesReturnStatus.Posted && x.PostingDate <= asOf)
                .GroupBy(x => x.SalesInvoiceId)
                .Select(g => new { InvoiceId = g.Key, Amount = g.Sum(x => x.BaseTotalAmount) })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, ct);

            // Never subtract an application to the originating invoice a second
            // time: its Posted sales return has already reversed that invoice's AR.
            var crossInvoiceCredits = await db.Set<CustomerCreditApplication>().AsNoTracking()
                .Join(db.Set<SalesReturn>().AsNoTracking(), a => a.SalesReturnId,
                    r => r.Id, (a, r) => new { a, r })
                .Where(x => x.a.SettlementKind == CustomerCreditSettlementKind.ApplyToInvoice &&
                    x.a.TargetSalesInvoiceId.HasValue &&
                    invoiceIds.Contains(x.a.TargetSalesInvoiceId.Value) &&
                    x.a.TargetSalesInvoiceId.Value != x.r.SalesInvoiceId &&
                    x.a.CustomerId == x.r.CustomerId &&
                    x.r.Status == SalesReturnStatus.Posted && x.r.PostingDate <= asOf &&
                    x.a.AppliedAtUtc < cutoff &&
                    (!x.a.ReversedAtUtc.HasValue || x.a.ReversedAtUtc.Value >= cutoff))
                .GroupBy(x => x.a.TargetSalesInvoiceId!.Value)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    Amount = g.Sum(x => x.a.TargetBaseAmount ?? 0m)
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, ct);

            foreach (var invoice in batch)
            {
                payments.TryGetValue(invoice.Id, out var paid);
                postedReturns.TryGetValue(invoice.Id, out var returned);
                crossInvoiceCredits.TryGetValue(invoice.Id, out var applied);
                var remaining = CustomerArMath.OpenInvoice(invoice.Original, paid, returned, applied);
                if (remaining <= 0m) continue;

                var bucket = CustomerArMath.Bucket(invoice.DueDate, asOf);
                switch (bucket)
                {
                    case "NotDue": notDue += remaining; break;
                    case "0-30": days30 += remaining; break;
                    case "31-60": days60 += remaining; break;
                    case "61-90": days90 += remaining; break;
                    case "+90": over90 += remaining; break;
                    default: unknown += remaining; break;
                }
                if (count >= pageOffset && rows.Count < pageSize)
                {
                    rows.Add(new CustomerAgingLineDto(invoice.Id, invoice.CustomerId,
                        invoice.CustomerCode, invoice.NameAr, invoice.InvoiceCode,
                        invoice.InvoiceDate, invoice.PostingDate, invoice.DueDate, bucket,
                        invoice.DueDate.HasValue ? asOf.DayNumber - invoice.DueDate.Value.DayNumber : null,
                        R(invoice.Original), R(paid), R(returned), R(applied),
                        remaining, invoice.CurrencyCodeSnapshot));
                }
                count++;
            }
        }

        var unappliedReceipts = await UnappliedReceiptsAsync(asOf, cutoff, customerId, ct);
        var returnCredits = await UnappliedReturnCreditsAsync(asOf, cutoff, customerId, ct);
        var advances = await CustomerAdvancesAsync(asOf, customerId, ct);
        return new CustomerAgingReportDto(asOf, await BaseCurrencyAsync(ct), rows,
            R(notDue), R(days30), R(days60), R(days90), R(over90), R(unknown),
            R(notDue + days30 + days60 + days90 + over90 + unknown),
            R(unappliedReceipts), R(returnCredits), R(advances),
            count, pageNumber, pageSize);
    }

    private async Task<decimal> UnappliedReceiptsAsync(DateOnly asOf, DateTime cutoff,
        Guid? customerId, CancellationToken ct)
    {
        // Exclude genuine customer advances from AR unapplied receipts. Gather only
        // line IDs/amounts; avoid an aggregate LEFT JOIN in a subsequent SUM.
        var receiptLines = await (
            from l in db.Set<ReceiptVoucherLine>().AsNoTracking()
            join v in db.Set<ReceiptVoucher>().AsNoTracking() on l.ReceiptVoucherId equals v.Id
            where l.CustomerId.HasValue && v.Status == ReceiptVoucherStatus.Posted &&
                v.VoucherDate <= asOf &&
                (!customerId.HasValue || l.CustomerId == customerId.Value) &&
                !db.Set<CustomerAdvance>().Any(a => a.ReceiptVoucherLineId == l.Id && a.IsActive)
            select new
            {
                l.Id,
                Amount = l.BaseAmount ?? (l.Amount * (l.ExchangeRate ?? 1m))
            }).ToListAsync(ct);

        decimal total = 0m;
        const int batchSize = 500;
        for (var start = 0; start < receiptLines.Count; start += batchSize)
        {
            var batch = receiptLines.Skip(start).Take(batchSize).ToArray();
            var ids = batch.Select(x => x.Id).ToArray();
            var used = await db.Set<PaymentAllocation>().AsNoTracking()
                .Where(x => x.ReceiptVoucherLineId.HasValue &&
                    ids.Contains(x.ReceiptVoucherLineId.Value) &&
                    x.AllocatedAtUtc < cutoff &&
                    (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoff))
                .GroupBy(x => x.ReceiptVoucherLineId!.Value)
                .Select(g => new
                {
                    LineId = g.Key,
                    Amount = g.Sum(x => x.BaseAllocatedAmount ??
                                        (x.AllocatedAmount * (x.ExchangeRate ?? 1m)))
                })
                .ToDictionaryAsync(x => x.LineId, x => x.Amount, ct);
            foreach (var line in batch)
            {
                used.TryGetValue(line.Id, out var allocated);
                total += Math.Max(0m, line.Amount - allocated);
            }
        }
        return R(total);
    }

    private async Task<decimal> UnappliedReturnCreditsAsync(DateOnly asOf, DateTime cutoff,
        Guid? customerId, CancellationToken ct)
    {
        // Return entitlement is the *paid* part of a posted return not yet used;
        // the source return must not create credit merely for reducing unpaid AR.
        var returned = await db.Set<SalesReturn>().AsNoTracking()
            .Where(x => x.Status == SalesReturnStatus.Posted &&
                x.PostingDate <= asOf &&
                (!customerId.HasValue || x.CustomerId == customerId.Value))
            .GroupBy(x => x.SalesInvoiceId)
            .Select(g => new { InvoiceId = g.Key, Amount = g.Sum(x => x.BaseTotalAmount) })
            .ToListAsync(ct);

        decimal total = 0m;
        const int batchSize = 500;
        for (var start = 0; start < returned.Count; start += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var batch = returned.Skip(start).Take(batchSize).ToArray();
            var ids = batch.Select(x => x.InvoiceId).ToArray();
            var invoices = await db.Set<SalesInvoice>().AsNoTracking()
                .Where(x => ids.Contains(x.Id) &&
                    x.Status == SalesInvoiceStatus.Posted && x.PostingDate <= asOf)
                .Select(x => new { x.Id, Original = x.BaseTotalAmount })
                .ToDictionaryAsync(x => x.Id, x => x.Original, ct);
            var payments = await db.Set<PaymentAllocation>().AsNoTracking()
                .Where(x => ids.Contains(x.TargetDocumentId) &&
                    x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice &&
                    x.AllocatedAtUtc < cutoff &&
                    (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoff))
                .GroupBy(x => x.TargetDocumentId)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    Amount = g.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ??
                                        (x.AllocatedAmount * (x.ExchangeRate ?? 1m)))
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, ct);
            var applied = await db.Set<CustomerCreditApplication>().AsNoTracking()
                .Join(db.Set<SalesReturn>().AsNoTracking(), a => a.SalesReturnId,
                    r => r.Id, (a, r) => new { a, r })
                .Where(x => ids.Contains(x.r.SalesInvoiceId) &&
                    x.r.Status == SalesReturnStatus.Posted && x.r.PostingDate <= asOf &&
                    x.a.AppliedAtUtc < cutoff &&
                    (!x.a.ReversedAtUtc.HasValue || x.a.ReversedAtUtc.Value >= cutoff))
                .GroupBy(x => x.r.SalesInvoiceId)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    Amount = g.Sum(x => x.a.AppliedSourceBaseAmount)
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, ct);

            foreach (var item in batch)
            {
                if (!invoices.TryGetValue(item.InvoiceId, out var original)) continue;
                payments.TryGetValue(item.InvoiceId, out var paid);
                applied.TryGetValue(item.InvoiceId, out var used);
                // Keep the legacy formula's financial intent, with explicit caps.
                var unusedReturn = Math.Max(0m, item.Amount - used);
                var paidExcess = Math.Max(0m, paid + item.Amount - original - used);
                total += Math.Min(unusedReturn, paidExcess);
            }
        }
        return R(total);
    }

    private async Task<decimal> CustomerAdvancesAsync(DateOnly asOf, Guid? customerId, CancellationToken ct)
    {
        var cutoff = DateTime.SpecifyKind(asOf.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var advances = await db.Set<CustomerAdvance>().AsNoTracking()
            .Where(x => x.ReceivedAtUtc < cutoff &&
                (!customerId.HasValue || x.CustomerId == customerId.Value))
            .Select(x => new { x.Id, x.BaseAmount })
            .ToListAsync(ct);
        decimal total = 0m;
        const int batchSize = 500;
        for (var start = 0; start < advances.Count; start += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var batch = advances.Skip(start).Take(batchSize).ToArray();
            var ids = batch.Select(x => x.Id).ToArray();
            var used = await db.Set<CustomerAdvanceApplication>().AsNoTracking()
                .Where(x => ids.Contains(x.CustomerAdvanceId) && x.AppliedAtUtc < cutoff)
                .GroupBy(x => x.CustomerAdvanceId)
                .Select(g => new { Id = g.Key, Amount = g.Sum(x => x.BaseAmount) })
                .ToDictionaryAsync(x => x.Id, x => x.Amount, ct);
            foreach (var advance in batch)
            {
                used.TryGetValue(advance.Id, out var applied);
                total += Math.Max(0m, advance.BaseAmount - applied);
            }
        }
        return R(total);
    }

    public async Task<CustomerArReconciliationDto> ReconcileAsync(DateOnly asOf,
        Guid? controlAccountId = null, CancellationToken ct = default)
    {
        var allAccounts = await db.Set<Account>().AsNoTracking()
            .Select(x => new { x.Id, x.Code, x.ParentAccountId, x.IsControlAccount, x.Level })
            .ToListAsync(ct);
        var customerAccounts = await db.Set<Customer>().AsNoTracking()
            .Select(x => x.AccountId).ToArrayAsync(ct);
        var byId = allAccounts.ToDictionary(x => x.Id);
        Guid? control = controlAccountId;
        if (!control.HasValue && customerAccounts.Length > 0)
        {
            HashSet<Guid>? common = null;
            foreach (var accountId in customerAccounts)
            {
                var ancestors = new HashSet<Guid>();
                Guid? pointer = accountId;
                while (pointer.HasValue && byId.TryGetValue(pointer.Value, out var a))
                {
                    if (a.IsControlAccount) ancestors.Add(a.Id);
                    pointer = a.ParentAccountId;
                }
                common = common is null ? ancestors : new HashSet<Guid>(common.Intersect(ancestors));
                if (common.Count == 0) break;
            }
            control = allAccounts.Where(x => common != null && common.Contains(x.Id))
                .OrderByDescending(x => x.Level).Select(x => (Guid?)x.Id).FirstOrDefault();
        }
        if (!control.HasValue || !byId.TryGetValue(control.Value, out var selected) || !selected.IsControlAccount)
            return new CustomerArReconciliationDto(asOf, await BaseCurrencyAsync(ct), null, null,
                0m, 0m, 0m, false, true, "حدد حساب رقابة عملاء صالحًا للمصالحة؛ لا يمكن استنتاجه بأمان.");

        var ids = new HashSet<Guid> { control.Value };
        var frontier = new Queue<Guid>(); frontier.Enqueue(control.Value);
        var children = allAccounts.Where(x => x.ParentAccountId.HasValue)
            .GroupBy(x => x.ParentAccountId!.Value).ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToArray());
        while (frontier.TryDequeue(out var id))
            if (children.TryGetValue(id, out var direct))
                foreach (var child in direct)
                    if (ids.Add(child)) frontier.Enqueue(child);
        var subtree = ids.ToArray();
        var ownAccounts = customerAccounts.Where(ids.Contains).Distinct().ToArray();
        var glRows = from l in db.Set<JournalEntryLine>().AsNoTracking()
                     join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
                     where j.Status == JournalEntryStatus.Posted && j.PostingDate <= asOf
                     select new { l.AccountId, Delta = l.DebitAmount - l.CreditAmount };
        var controlBalance = await glRows.Where(x => subtree.Contains(x.AccountId)).SumAsync(x => x.Delta, ct);
        var customerBalance = await glRows.Where(x => ownAccounts.Contains(x.AccountId)).SumAsync(x => x.Delta, ct);
        var difference = R(controlBalance - customerBalance);
        return new CustomerArReconciliationDto(asOf, await BaseCurrencyAsync(ct),
            control, selected.Code, R(controlBalance), R(customerBalance), difference,
            difference == 0m, false, difference == 0m ? "مطابقة" : "توجد حركة في حساب الرقابة خارج الحسابات الفرعية للعملاء.");
    }

    public async Task<CustomerArDocumentPageDto> DocumentsAsync(Guid customerId,
        string kind, DateOnly asOf, int page = 1, int size = 50,
        CancellationToken ct = default)
    {
        if (!await db.Set<Customer>().AnyAsync(x => x.Id == customerId, ct))
            throw new NotFoundException(nameof(Customer), customerId);
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 200);
        var skip = (page - 1) * size;
        var cutoffUtc = asOf.AddDays(1).ToDateTime(TimeOnly.MinValue);
        kind = (kind ?? string.Empty).Trim().ToLowerInvariant();

        if (kind == "invoices")
        {
            var query = db.Set<SalesInvoice>().AsNoTracking().Where(x =>
                x.CustomerId == customerId && x.PostingDate <= asOf &&
                x.Status == SalesInvoiceStatus.Posted);
            var count = await query.LongCountAsync(ct);
            var rows = await query.OrderByDescending(x => x.PostingDate).ThenBy(x => x.InvoiceCode)
                .Skip(skip).Take(size).Select(x => new CustomerArDocumentRowDto(
                    x.Id, "SalesInvoice", x.InvoiceCode, x.PostingDate,
                    x.BaseTotalAmount, x.CurrencyCodeSnapshot, "Posted", x.JournalEntryId)).ToListAsync(ct);
            return new(kind, rows, count, page, size);
        }
        if (kind == "returns")
        {
            var query = db.Set<SalesReturn>().AsNoTracking().Where(x =>
                x.CustomerId == customerId && x.PostingDate <= asOf && x.Status == SalesReturnStatus.Posted);
            var count = await query.LongCountAsync(ct);
            var rows = await query.OrderByDescending(x => x.PostingDate).ThenBy(x => x.ReturnCode)
                .Skip(skip).Take(size).Select(x => new CustomerArDocumentRowDto(
                    x.Id, "SalesReturn", x.ReturnCode, x.PostingDate, x.BaseTotalAmount,
                    x.CurrencyCodeSnapshot, "Posted", x.JournalEntryId)).ToListAsync(ct);
            return new(kind, rows, count, page, size);
        }
        if (kind == "receipts")
        {
            var query = from line in db.Set<ReceiptVoucherLine>().AsNoTracking()
                        join voucher in db.Set<ReceiptVoucher>().AsNoTracking()
                             on line.ReceiptVoucherId equals voucher.Id
                        where line.CustomerId == customerId &&
                              voucher.Status == ReceiptVoucherStatus.Posted && voucher.VoucherDate <= asOf
                        select new { line, voucher };
            var count = await query.LongCountAsync(ct);
            var source = await query.OrderByDescending(x => x.voucher.VoucherDate)
                .ThenBy(x => x.voucher.VoucherNumber).ThenBy(x => x.line.LineNumber)
                .Skip(skip).Take(size).Select(x => new {
                    x.line.Id, x.voucher.VoucherNumber, x.voucher.VoucherDate,
                    x.line.BaseAmount, x.line.Amount, x.line.ExchangeRate,
                    x.line.CurrencyCodeSnapshot, x.voucher.JournalEntryId
                }).ToListAsync(ct);
            return new(kind, source.Select(x => new CustomerArDocumentRowDto(
                x.Id, "ReceiptVoucherLine", x.VoucherNumber, x.VoucherDate,
                R(x.BaseAmount ?? x.Amount * (x.ExchangeRate ?? 1m)),
                x.CurrencyCodeSnapshot ?? "", "Posted", x.JournalEntryId)).ToArray(), count, page, size);
        }
        if (kind == "advances")
        {
            var query = db.Set<CustomerAdvance>().AsNoTracking().Where(x =>
                x.CustomerId == customerId && x.ReceivedAtUtc < cutoffUtc);
            var count = await query.LongCountAsync(ct);
            var source = await query.OrderByDescending(x => x.ReceivedAtUtc)
                .Skip(skip).Take(size).Select(x => new {
                    x.Id, x.AdvanceNumber, x.ReceivedAtUtc, x.BaseAmount,
                    x.CurrencyCodeSnapshot, x.Status
                }).ToListAsync(ct);
            return new(kind, source.Select(x => new CustomerArDocumentRowDto(
                x.Id, "CustomerAdvance", x.AdvanceNumber,
                DateOnly.FromDateTime(x.ReceivedAtUtc), x.BaseAmount,
                x.CurrencyCodeSnapshot, x.Status.ToString(), null)).ToArray(), count, page, size);
        }
        if (kind == "credits")
        {
            var query = db.Set<CustomerCreditApplication>().AsNoTracking().Where(x =>
                x.CustomerId == customerId && x.AppliedAtUtc < cutoffUtc);
            var count = await query.LongCountAsync(ct);
            var source = await query.OrderByDescending(x => x.AppliedAtUtc).ThenBy(x => x.Id)
                .Skip(skip).Take(size).Select(x => new {
                    x.Id, x.SalesReturnId, x.AppliedAtUtc,
                    x.AppliedSourceBaseAmount, x.SettlementKind,
                    x.ReversedAtUtc
                }).ToListAsync(ct);
            return new(kind, source.Select(x => new CustomerArDocumentRowDto(
                x.Id, "CustomerCreditApplication", x.SalesReturnId.ToString("D"),
                DateOnly.FromDateTime(x.AppliedAtUtc), x.AppliedSourceBaseAmount,
                "BASE", x.ReversedAtUtc.HasValue && x.ReversedAtUtc.Value < cutoffUtc
                    ? "Reversed" : x.SettlementKind.ToString(), null)).ToArray(), count, page, size);
        }
        throw new ArgumentException("Unsupported customer document type.", nameof(kind));
    }

    public async Task<CustomerArWorkspaceDto> WorkspaceAsync(Guid customerId,
        DateOnly from, DateOnly to, int pageNumber = 1, int pageSize = 50,
        CancellationToken ct = default)
    {
        var statement = await StatementAsync(customerId, from, to, pageNumber, pageSize, ct);
        var aging = await AgingAsync(to, customerId, pageNumber, pageSize, ct);
        return new CustomerArWorkspaceDto(customerId, statement.CustomerCode, statement.CustomerName,
            statement, aging, aging.UnappliedReceiptsBase, aging.UnappliedReturnCreditsBase,
            aging.CustomerAdvancesLiabilityBase);
    }
}
