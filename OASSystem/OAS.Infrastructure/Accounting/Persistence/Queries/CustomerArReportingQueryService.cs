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
        var sums = from l in db.Set<JournalEntryLine>().AsNoTracking()
                   join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
                   where j.Status == JournalEntryStatus.Posted && j.PostingDate <= asOf
                   group l by l.AccountId into g
                   select new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) };
        var query = from c in db.Set<Customer>().AsNoTracking()
                    join a in db.Set<Account>().AsNoTracking() on c.AccountId equals a.Id
                    join g in sums on a.Id equals g.AccountId into ag
                    from g in ag.DefaultIfEmpty()
                    select new {
                        c.Id, c.CustomerCode, c.NameAr, c.AccountId, a.Code,
                        Debit = g == null ? 0m : g.Debit,
                        Credit = g == null ? 0m : g.Credit
                    };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.CustomerCode.Contains(term) || x.NameAr.Contains(term) || x.Code.Contains(term));
        }
        var count = await query.LongCountAsync(ct);
        var totals = await query.GroupBy(x => 1).Select(g => new {
            DebitBalances = g.Sum(x => x.Debit > x.Credit ? x.Debit - x.Credit : 0m),
            CreditBalances = g.Sum(x => x.Credit > x.Debit ? x.Credit - x.Debit : 0m),
            Net = g.Sum(x => x.Debit - x.Credit)
        }).SingleOrDefaultAsync(ct);
        var page = await query.OrderBy(x => x.CustomerCode)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new CustomerBalancesReportDto(asOf, await BaseCurrencyAsync(ct),
            page.Select(x => new CustomerBalanceRowDto(x.Id, x.CustomerCode,
                x.NameAr, x.AccountId, x.Code, R(x.Debit), R(x.Credit), R(x.Debit - x.Credit))).ToArray(),
            R(totals?.DebitBalances ?? 0m), R(totals?.CreditBalances ?? 0m), R(totals?.Net ?? 0m),
            count, pageNumber, pageSize);
    }

    public async Task<CustomerAgingReportDto> AgingAsync(DateOnly asOf, Guid? customerId = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var cutoff = DateTime.SpecifyKind(asOf.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var payments = db.Set<PaymentAllocation>().AsNoTracking()
            .Where(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice
                && x.AllocatedAtUtc < cutoff
                && (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoff))
            .GroupBy(x => x.TargetDocumentId).Select(g => new {
                InvoiceId = g.Key,
                Amount = g.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ??
                                     (x.AllocatedAmount * (x.ExchangeRate ?? 1m)))
            });
        var returns = db.Set<SalesReturn>().AsNoTracking()
            .Where(x => x.Status == SalesReturnStatus.Posted && x.PostingDate <= asOf)
            .GroupBy(x => x.SalesInvoiceId).Select(g => new {
                InvoiceId = g.Key, Amount = g.Sum(x => x.BaseTotalAmount)
            });
        // The originating invoice is already reduced by its posted return.
        // Only credit applications to OTHER invoices reduce their outstanding.
        var applications = db.Set<CustomerCreditApplication>().AsNoTracking()
            .Join(db.Set<SalesReturn>().AsNoTracking(), a => a.SalesReturnId, r => r.Id,
                (a, r) => new { a, r })
            .Where(x => x.a.SettlementKind == CustomerCreditSettlementKind.ApplyToInvoice &&
                x.a.TargetSalesInvoiceId.HasValue && x.a.TargetSalesInvoiceId.Value != x.r.SalesInvoiceId &&
                x.r.Status == SalesReturnStatus.Posted && x.r.PostingDate <= asOf &&
                x.a.AppliedAtUtc < cutoff &&
                (!x.a.ReversedAtUtc.HasValue || x.a.ReversedAtUtc.Value >= cutoff))
            .GroupBy(x => x.a.TargetSalesInvoiceId!.Value).Select(g => new {
                InvoiceId = g.Key, Amount = g.Sum(x => x.a.TargetBaseAmount ?? 0m)
            });

        var invoices = from i in db.Set<SalesInvoice>().AsNoTracking()
                       join c in db.Set<Customer>().AsNoTracking() on i.CustomerId equals c.Id
                       join p in payments on i.Id equals p.InvoiceId into gp
                       from p in gp.DefaultIfEmpty()
                       join r in returns on i.Id equals r.InvoiceId into gr
                       from r in gr.DefaultIfEmpty()
                       join a in applications on i.Id equals a.InvoiceId into ga
                       from a in ga.DefaultIfEmpty()
                       where i.Status == SalesInvoiceStatus.Posted && i.PostingDate <= asOf
                         && (!customerId.HasValue || i.CustomerId == customerId.Value)
                       select new {
                           i.Id, i.CustomerId, c.CustomerCode, c.NameAr, i.InvoiceCode,
                           i.InvoiceDate, i.PostingDate, i.DueDate, i.CurrencyCodeSnapshot,
                           Original = i.BaseTotalAmount,
                           Paid = p == null ? 0m : p.Amount,
                           Returned = r == null ? 0m : r.Amount,
                           Applied = a == null ? 0m : a.Amount
                       };
        var due = invoices.Select(x => new {
            x.Id, x.CustomerId, x.CustomerCode, x.NameAr, x.InvoiceCode,
            x.InvoiceDate, x.PostingDate, x.DueDate, x.CurrencyCodeSnapshot,
            x.Original, x.Paid, x.Returned, x.Applied,
            Remaining = x.Original > x.Paid + x.Returned + x.Applied
                ? x.Original - x.Paid - x.Returned - x.Applied : 0m
        }).Where(x => x.Remaining > 0m);

        var count = await due.LongCountAsync(ct);
        var notDue = await due.Where(x => x.DueDate.HasValue && x.DueDate > asOf).SumAsync(x => x.Remaining, ct);
        var days30 = await due.Where(x => x.DueDate.HasValue && x.DueDate <= asOf && x.DueDate >= asOf.AddDays(-30)).SumAsync(x => x.Remaining, ct);
        var days60 = await due.Where(x => x.DueDate.HasValue && x.DueDate < asOf.AddDays(-30) && x.DueDate >= asOf.AddDays(-60)).SumAsync(x => x.Remaining, ct);
        var days90 = await due.Where(x => x.DueDate.HasValue && x.DueDate < asOf.AddDays(-60) && x.DueDate >= asOf.AddDays(-90)).SumAsync(x => x.Remaining, ct);
        var over90 = await due.Where(x => x.DueDate.HasValue && x.DueDate < asOf.AddDays(-90)).SumAsync(x => x.Remaining, ct);
        var unknown = await due.Where(x => !x.DueDate.HasValue).SumAsync(x => x.Remaining, ct);
        var items = await due.OrderBy(x => x.DueDate).ThenBy(x => x.InvoiceCode)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var rows = items.Select(x => new CustomerAgingLineDto(x.Id, x.CustomerId,
            x.CustomerCode, x.NameAr, x.InvoiceCode, x.InvoiceDate,
            x.PostingDate, x.DueDate, CustomerArMath.Bucket(x.DueDate, asOf),
            x.DueDate.HasValue ? asOf.DayNumber - x.DueDate.Value.DayNumber : null,
            R(x.Original), R(x.Paid), R(x.Returned), R(x.Applied),
            R(x.Remaining), x.CurrencyCodeSnapshot)).ToArray();

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
        // Advance receipts belong to a LIABILITY account, not unallocated AR cash.
        var settled = db.Set<PaymentAllocation>().AsNoTracking()
            .Where(x => x.ReceiptVoucherLineId.HasValue && x.AllocatedAtUtc < cutoff &&
                (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoff))
            .GroupBy(x => x.ReceiptVoucherLineId!.Value)
            .Select(g => new { LineId = g.Key,
                Amount = g.Sum(x => x.BaseAllocatedAmount ?? x.AllocatedAmount * (x.ExchangeRate ?? 1m)) });
        var lines = from l in db.Set<ReceiptVoucherLine>().AsNoTracking()
                    join v in db.Set<ReceiptVoucher>().AsNoTracking() on l.ReceiptVoucherId equals v.Id
                    join used in settled on l.Id equals used.LineId into joined
                    from used in joined.DefaultIfEmpty()
                    where l.CustomerId.HasValue && v.Status == ReceiptVoucherStatus.Posted &&
                        v.VoucherDate <= asOf && (!customerId.HasValue || l.CustomerId == customerId.Value) &&
                        !db.Set<CustomerAdvance>().Any(a => a.ReceiptVoucherLineId == l.Id && a.IsActive)
                    select new {
                        Amount = l.BaseAmount ??
                            (l.Amount * (l.ExchangeRate ?? 1m)),
                        Used = used == null ? 0m : used.Amount
                    };
        var total = await lines.Select(x => x.Amount > x.Used ? x.Amount - x.Used : 0m)
            .SumAsync(ct);
        return total;
    }

    private async Task<decimal> UnappliedReturnCreditsAsync(DateOnly asOf, DateTime cutoff,
        Guid? customerId, CancellationToken ct)
    {
        // At the invoice level, only payments exceeding original minus posted returns
        // produce a refundable credit. The source return may have been applied elsewhere.
        var paid = db.Set<PaymentAllocation>().AsNoTracking()
            .Where(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice &&
                        x.AllocatedAtUtc < cutoff && (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoff))
            .GroupBy(x => x.TargetDocumentId).Select(g => new {
                Id = g.Key, Amount = g.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ??
                                                    x.AllocatedAmount * (x.ExchangeRate ?? 1m))
            });
        var credited = db.Set<CustomerCreditApplication>().AsNoTracking()
            .Join(db.Set<SalesReturn>().AsNoTracking(), a => a.SalesReturnId, r => r.Id,
                  (a, r) => new { a, r })
            .Where(x => x.r.Status == SalesReturnStatus.Posted && x.r.PostingDate <= asOf &&
                    x.a.AppliedAtUtc < cutoff &&
                    (!x.a.ReversedAtUtc.HasValue || x.a.ReversedAtUtc.Value >= cutoff))
            .GroupBy(x => x.r.SalesInvoiceId).Select(g => new {
                Id = g.Key, Amount = g.Sum(x => x.a.AppliedSourceBaseAmount)
            });
        var returned = db.Set<SalesReturn>().AsNoTracking()
            .Where(x => x.Status == SalesReturnStatus.Posted && x.PostingDate <= asOf)
            .GroupBy(x => x.SalesInvoiceId)
            .Select(g => new { Id = g.Key, Amount = g.Sum(x => x.BaseTotalAmount) });
        var available = from i in db.Set<SalesInvoice>().AsNoTracking()
                        join r in returned on i.Id equals r.Id
                        join p in paid on i.Id equals p.Id into ps
                        from p in ps.DefaultIfEmpty()
                        join a in credited on i.Id equals a.Id into apps
                        from a in apps.DefaultIfEmpty()
                        where i.Status == SalesInvoiceStatus.Posted && i.PostingDate <= asOf &&
                            (!customerId.HasValue || i.CustomerId == customerId.Value)
                        select new {
                            Return = r.Amount,
                            Paid = p == null ? 0m : p.Amount,
                            Used = a == null ? 0m : a.Amount,
                            Original = i.BaseTotalAmount
                        };
        return await available.Select(x =>
            x.Return > x.Used && x.Paid + x.Return > x.Original + x.Used
                ? (x.Paid + x.Return - x.Original - x.Used > x.Return - x.Used
                    ? x.Return - x.Used : x.Paid + x.Return - x.Original - x.Used)
                : 0m).SumAsync(ct);
    }

    private async Task<decimal> CustomerAdvancesAsync(DateOnly asOf, Guid? customerId, CancellationToken ct)
    {
        // Advance outstanding is separated explicitly; it must never be double
        // subtracted from invoice aging or AR ledger.
        var advances = db.Set<CustomerAdvance>().AsNoTracking()
            .Where(x => x.ReceivedAtUtc <
                DateTime.SpecifyKind(asOf.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) &&
                (!customerId.HasValue || x.CustomerId == customerId.Value));
        // BaseAppliedAmount is a current-state field; use dated applications for historical as-of.
        var cutoff = DateTime.SpecifyKind(asOf.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var used = db.Set<CustomerAdvanceApplication>().AsNoTracking()
            .Where(x => x.AppliedAtUtc < cutoff)
            .GroupBy(x => x.CustomerAdvanceId).Select(g => new {
                Id = g.Key, Amount = g.Sum(x => x.BaseAmount)
            });
        var amounts = from a in advances
                      join b in used on a.Id equals b.Id into bg
                      from b in bg.DefaultIfEmpty()
                      select new { a.BaseAmount, Used = b == null ? 0m : b.Amount };
        return await amounts.Select(x => x.BaseAmount > x.Used ? x.BaseAmount - x.Used : 0m).SumAsync(ct);
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
