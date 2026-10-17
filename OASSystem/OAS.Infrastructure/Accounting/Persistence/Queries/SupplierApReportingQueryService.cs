using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class SupplierApReportingQueryService(OasDbContext db) : ISupplierApReportingQueryService
{
    private const string PurchasingModule = "Purchasing";
    private const string PurchaseReceiptDocumentType = "PurchaseReceipt";
    private const string PurchaseInvoiceDocumentType = "PurchaseInvoice";
    private const string PurchaseReturnDocumentType = "PurchaseReturn";
    private const string GrniPostingRole = "GoodsReceivedNotInvoiced";

    public async Task<SupplierStatementDto> GetSupplierStatementAsync(
        SupplierStatementFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        if (filter.SupplierId == Guid.Empty)
            throw new ArgumentException("Supplier id is required.", nameof(filter));
        if (filter.ToDate < filter.FromDate)
            throw new ConflictException("supplier_statement_date_range_invalid", "تاريخ النهاية يجب ألا يسبق تاريخ البداية.");

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, PageRequest.MaximumPageSize);

        var supplier = await (
            from s in db.Set<Supplier>().AsNoTracking()
            join a in db.Set<Account>().AsNoTracking() on s.AccountId equals a.Id
            where s.Id == filter.SupplierId
            select new SupplierAccountMeta(
                s.Id,
                s.SupplierCode,
                s.NameAr,
                a.Id,
                a.Code,
                a.NameAr))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Supplier), filter.SupplierId);

        var apLines =
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where line.AccountId == supplier.AccountId
                  && journal.Status == JournalEntryStatus.Posted
                  && journal.PostingDate <= filter.ToDate
            select new
            {
                LineId = line.Id,
                line.LineNumber,
                line.DebitAmount,
                line.CreditAmount,
                line.Description,
                line.TransactionCurrencyCodeSnapshot,
                line.TransactionDebitAmount,
                line.TransactionCreditAmount,
                line.ExchangeRate,
                line.SourceDocumentLineId,
                line.PostingRole,
                JournalId = journal.Id,
                journal.JournalNumber,
                journal.JournalType,
                journal.PostingDate,
                journal.DocumentDate,
                JournalDescription = journal.Description,
                journal.SourceModule,
                journal.SourceDocumentType,
                journal.SourceDocumentId,
                journal.ReversedJournalId
            };

        var opening = await apLines
            .Where(x => x.PostingDate < filter.FromDate)
            .SumAsync(x => x.CreditAmount - x.DebitAmount, cancellationToken);

        var periodAll = apLines.Where(x => x.PostingDate >= filter.FromDate);
        var periodDebit = await periodAll.SumAsync(x => x.DebitAmount, cancellationToken);
        var periodCredit = await periodAll.SumAsync(x => x.CreditAmount, cancellationToken);
        var closing = opening + periodCredit - periodDebit;
        var distinctDocuments = await periodAll
            .Select(x => x.JournalId)
            .Distinct()
            .LongCountAsync(cancellationToken);

        var filtered = periodAll;
        if (!string.IsNullOrWhiteSpace(filter.Reference))
        {
            var term = filter.Reference.Trim();
            filtered = filtered.Where(x =>
                x.JournalNumber.Contains(term) ||
                (x.Description != null && x.Description.Contains(term)) ||
                x.JournalDescription.Contains(term) ||
                (x.SourceDocumentType != null && x.SourceDocumentType.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(filter.SourceDocumentType))
        {
            var documentType = filter.SourceDocumentType.Trim();
            filtered = filtered.Where(x => x.SourceDocumentType == documentType);
        }
        if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
        {
            var currencyCode = filter.CurrencyCode.Trim().ToUpperInvariant();
            filtered = filtered.Where(x => x.TransactionCurrencyCodeSnapshot == currencyCode);
        }

        var totalCount = await filtered.LongCountAsync(cancellationToken);
        var descending = filter.SortDirection == SortDirection.Descending;
        var ordered = descending
            ? filtered.OrderByDescending(x => x.PostingDate)
                .ThenByDescending(x => x.JournalNumber)
                .ThenByDescending(x => x.LineNumber)
            : filtered.OrderBy(x => x.PostingDate)
                .ThenBy(x => x.JournalNumber)
                .ThenBy(x => x.LineNumber);

        var page = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageStartBalance = opening;
        if (page.Count > 0)
        {
            var first = page[0];
            if (!descending)
            {
                var beforePage = await periodAll
                    .Where(x =>
                        x.PostingDate < first.PostingDate ||
                        (x.PostingDate == first.PostingDate && string.Compare(x.JournalNumber, first.JournalNumber) < 0) ||
                        (x.PostingDate == first.PostingDate && x.JournalNumber == first.JournalNumber && x.LineNumber < first.LineNumber))
                    .SumAsync(x => x.CreditAmount - x.DebitAmount, cancellationToken);
                pageStartBalance = opening + beforePage;
            }
            else
            {
                var afterPageStart = await periodAll
                    .Where(x =>
                        x.PostingDate > first.PostingDate ||
                        (x.PostingDate == first.PostingDate && string.Compare(x.JournalNumber, first.JournalNumber) > 0) ||
                        (x.PostingDate == first.PostingDate && x.JournalNumber == first.JournalNumber && x.LineNumber > first.LineNumber))
                    .SumAsync(x => x.CreditAmount - x.DebitAmount, cancellationToken);
                pageStartBalance = closing - afterPageStart;
            }
        }

        var movements = new List<SupplierStatementMovementDto>(page.Count);
        var running = pageStartBalance;
        foreach (var row in page)
        {
            var delta = Round(row.CreditAmount - row.DebitAmount);
            if (!descending)
            {
                running = Round(running + delta);
                movements.Add(MapMovement(row, running));
            }
            else
            {
                movements.Add(MapMovement(row, running));
                running = Round(running - delta);
            }
        }

        var monthlyRaw = await periodAll
            .GroupBy(x => new { x.PostingDate.Year, x.PostingDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Debit = g.Sum(x => x.DebitAmount),
                Credit = g.Sum(x => x.CreditAmount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var monthOpening = opening;
        var monthly = new List<SupplierMonthlyBalanceDto>(monthlyRaw.Count);
        foreach (var month in monthlyRaw)
        {
            var monthClosing = Round(monthOpening + month.Credit - month.Debit);
            monthly.Add(new SupplierMonthlyBalanceDto(
                month.Year,
                month.Month,
                monthOpening,
                Round(month.Debit),
                Round(month.Credit),
                monthClosing));
            monthOpening = monthClosing;
        }

        var operational = await BuildOperationalExposureAsync(
            supplier.SupplierId,
            filter.FromDate,
            filter.ToDate,
            cancellationToken);

        return new SupplierStatementDto(
            supplier.SupplierId,
            supplier.SupplierCode,
            supplier.SupplierName,
            supplier.AccountId,
            supplier.AccountCode,
            supplier.AccountName,
            filter.FromDate,
            filter.ToDate,
            Round(opening),
            Round(periodDebit),
            Round(periodCredit),
            Round(closing),
            distinctDocuments,
            movements,
            monthly,
            operational,
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<SupplierBalancesDto> GetSupplierBalancesAsync(
        SupplierBalancesFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, PageRequest.MaximumPageSize);

        var aggregates =
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where journal.Status == JournalEntryStatus.Posted
                  && journal.PostingDate <= filter.AsOfDate
            group line by line.AccountId
            into g
            select new
            {
                AccountId = g.Key,
                Debit = g.Sum(x => x.DebitAmount),
                Credit = g.Sum(x => x.CreditAmount)
            };

        var query =
            from supplier in db.Set<Supplier>().AsNoTracking()
            join account in db.Set<Account>().AsNoTracking()
                on supplier.AccountId equals account.Id
            join aggregate in aggregates
                on account.Id equals aggregate.AccountId into aggregateJoin
            from aggregate in aggregateJoin.DefaultIfEmpty()
            select new
            {
                SupplierId = supplier.Id,
                supplier.SupplierCode,
                SupplierName = supplier.NameAr,
                AccountId = account.Id,
                AccountCode = account.Code,
                AccountName = account.NameAr,
                Debit = aggregate == null ? 0m : aggregate.Debit,
                Credit = aggregate == null ? 0m : aggregate.Credit
            };

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x =>
                x.SupplierCode.Contains(term) ||
                x.SupplierName.Contains(term) ||
                x.AccountCode.Contains(term) ||
                x.AccountName.Contains(term));
        }
        if (!filter.IncludeZeroBalance)
            query = query.Where(x => x.Credit != x.Debit);

        var totalCount = await query.LongCountAsync(cancellationToken);
        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Liability = g.Sum(x => x.Credit - x.Debit),
                DebitBalance = g.Sum(x => x.Debit > x.Credit ? x.Debit - x.Credit : 0m),
                CreditBalance = g.Sum(x => x.Credit > x.Debit ? x.Credit - x.Debit : 0m)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var descending = filter.SortDirection == SortDirection.Descending;
        var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
        var ordered = sortBy switch
        {
            "balance" => descending
                ? query.OrderByDescending(x => x.Credit - x.Debit).ThenBy(x => x.SupplierCode)
                : query.OrderBy(x => x.Credit - x.Debit).ThenBy(x => x.SupplierCode),
            "accountcode" => descending
                ? query.OrderByDescending(x => x.AccountCode).ThenBy(x => x.SupplierCode)
                : query.OrderBy(x => x.AccountCode).ThenBy(x => x.SupplierCode),
            "name" => descending
                ? query.OrderByDescending(x => x.SupplierName).ThenBy(x => x.SupplierCode)
                : query.OrderBy(x => x.SupplierName).ThenBy(x => x.SupplierCode),
            _ => descending
                ? query.OrderByDescending(x => x.SupplierCode)
                : query.OrderBy(x => x.SupplierCode)
        };

        var page = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = page.Select(x =>
        {
            var liability = Round(x.Credit - x.Debit);
            return new SupplierBalanceRowDto(
                x.SupplierId,
                x.SupplierCode,
                x.SupplierName,
                x.AccountId,
                x.AccountCode,
                x.AccountName,
                Round(x.Debit),
                Round(x.Credit),
                liability,
                liability < 0m ? Math.Abs(liability) : 0m,
                liability > 0m ? liability : 0m);
        }).ToArray();

        return new SupplierBalancesDto(
            filter.AsOfDate,
            rows,
            Round(totals?.Liability ?? 0m),
            Round(totals?.DebitBalance ?? 0m),
            Round(totals?.CreditBalance ?? 0m),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<ApReconciliationDto> GetApReconciliationAsync(
        DateOnly asOfDate,
        Guid? controlAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var supplierAccountIds = await db.Set<Supplier>()
            .AsNoTracking()
            .Select(x => x.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var supplierCount = supplierAccountIds.Count;
        var supplierSubledger = await (
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            join supplier in db.Set<Supplier>().AsNoTracking()
                on line.AccountId equals supplier.AccountId
            where journal.Status == JournalEntryStatus.Posted
                  && journal.PostingDate <= asOfDate
            select line.CreditAmount - line.DebitAmount)
            .SumAsync(cancellationToken);

        var accounts = await db.Set<Account>()
            .AsNoTracking()
            .Select(x => new AccountTreeMeta(
                x.Id,
                x.Code,
                x.NameAr,
                x.ParentAccountId,
                x.Level,
                x.IsControlAccount))
            .ToListAsync(cancellationToken);

        AccountTreeMeta? control;
        var derived = !controlAccountId.HasValue;
        if (controlAccountId.HasValue)
        {
            control = accounts.SingleOrDefault(x => x.Id == controlAccountId.Value)
                ?? throw new NotFoundException(nameof(Account), controlAccountId.Value);
        }
        else
        {
            control = ResolveCommonControlAccount(supplierAccountIds, accounts);
            if (control is null && supplierAccountIds.Count > 0)
            {
                throw new ConflictException(
                    "ap_control_account_not_resolved",
                    "تعذر تحديد حساب رقابة موحد للموردين. حدد controlAccountId صراحةً للمصالحة.");
            }
        }

        decimal controlLiability = 0m;
        Guid? resolvedControlId = null;
        string? controlCode = null;
        string? controlName = null;
        if (control is not null)
        {
            resolvedControlId = control.Id;
            controlCode = control.Code;
            controlName = control.Name;
            var subtree = ResolveSubtree(control.Id, accounts).ToArray();
            controlLiability = await (
                from line in db.Set<JournalEntryLine>().AsNoTracking()
                join journal in db.Set<JournalEntry>().AsNoTracking()
                    on line.JournalEntryId equals journal.Id
                where journal.Status == JournalEntryStatus.Posted
                      && journal.PostingDate <= asOfDate
                      && subtree.Contains(line.AccountId)
                select line.CreditAmount - line.DebitAmount)
                .SumAsync(cancellationToken);
        }

        var grniBalance = await (
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where journal.Status == JournalEntryStatus.Posted
                  && journal.PostingDate <= asOfDate
                  && line.PostingRole == GrniPostingRole
            select line.CreditAmount - line.DebitAmount)
            .SumAsync(cancellationToken);

        var difference = Round(controlLiability - supplierSubledger);
        return new ApReconciliationDto(
            asOfDate,
            resolvedControlId,
            controlCode,
            controlName,
            derived,
            Round(supplierSubledger),
            Round(controlLiability),
            difference,
            Math.Abs(difference) < 0.0001m,
            supplierCount,
            Round(grniBalance));
    }

    private async Task<SupplierOperationalExposureDto> BuildOperationalExposureAsync(
        Guid supplierId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct)
    {
        var purchaseOrders = db.Set<PurchaseOrder>()
            .AsNoTracking()
            .Where(x =>
                x.SupplierId == supplierId &&
                x.OrderDate >= fromDate &&
                x.OrderDate <= toDate &&
                x.Status != PurchaseOrderStatus.Draft &&
                x.Status != PurchaseOrderStatus.Rejected &&
                x.Status != PurchaseOrderStatus.Cancelled);

        var orderCount = await purchaseOrders.LongCountAsync(ct);
        var orderBase = await purchaseOrders.SumAsync(x => x.TotalAmount * x.ExchangeRate, ct);

        var grni =
            from line in db.Set<JournalEntryLine>().AsNoTracking()
            join journal in db.Set<JournalEntry>().AsNoTracking()
                on line.JournalEntryId equals journal.Id
            where journal.Status == JournalEntryStatus.Posted
                  && journal.PostingDate <= toDate
                  && journal.SourceModule == PurchasingModule
                  && line.SupplierId == supplierId
                  && line.PostingRole == GrniPostingRole
            select new
            {
                line.DebitAmount,
                line.CreditAmount,
                journal.PostingDate,
                journal.SourceDocumentType,
                journal.SourceDocumentId,
                JournalId = journal.Id
            };

        var openGrni = await grni.SumAsync(x => x.CreditAmount - x.DebitAmount, ct);
        var periodGrni = grni.Where(x => x.PostingDate >= fromDate);
        var receiptCreated = await periodGrni
            .Where(x => x.SourceDocumentType == PurchaseReceiptDocumentType)
            .SumAsync(x => x.CreditAmount, ct);
        var receiptCount = await periodGrni
            .Where(x => x.SourceDocumentType == PurchaseReceiptDocumentType)
            .Select(x => x.SourceDocumentId ?? x.JournalId)
            .Distinct()
            .LongCountAsync(ct);
        var grniCleared = await periodGrni.SumAsync(x => x.DebitAmount, ct);

        return new SupplierOperationalExposureDto(
            Round(orderBase),
            orderCount,
            Round(receiptCreated),
            receiptCount,
            Round(grniCleared),
            Round(openGrni));
    }

    private static SupplierStatementMovementDto MapMovement(dynamic row, decimal running)
    {
        var movementType = ClassifyMovement((string?)row.SourceModule, (string?)row.SourceDocumentType);
        var description = string.IsNullOrWhiteSpace((string?)row.Description)
            ? (string)row.JournalDescription
            : (string)row.Description;
        var reference = FormatReference((string?)row.SourceDocumentType, (Guid?)row.SourceDocumentId, (string)row.JournalNumber);

        return new SupplierStatementMovementDto(
            (Guid)row.JournalId,
            (string)row.JournalNumber,
            (int)row.LineNumber,
            (DateOnly)row.PostingDate,
            (DateOnly)row.DocumentDate,
            description,
            (string?)row.SourceModule,
            (string?)row.SourceDocumentType,
            (Guid?)row.SourceDocumentId,
            (Guid?)row.SourceDocumentLineId,
            (string?)row.PostingRole,
            Round((decimal)row.DebitAmount),
            Round((decimal)row.CreditAmount),
            Round((decimal)row.CreditAmount - (decimal)row.DebitAmount),
            Round(running),
            (string?)row.TransactionCurrencyCodeSnapshot,
            (decimal?)row.TransactionDebitAmount,
            (decimal?)row.TransactionCreditAmount,
            (decimal?)row.ExchangeRate,
            movementType,
            reference,
            ((Guid?)row.ReversedJournalId).HasValue);
    }

    private static string ClassifyMovement(string? sourceModule, string? sourceDocumentType)
    {
        if (string.Equals(sourceModule, PurchasingModule, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(sourceDocumentType, PurchaseInvoiceDocumentType, StringComparison.OrdinalIgnoreCase)) return "Invoice";
            if (string.Equals(sourceDocumentType, PurchaseReturnDocumentType, StringComparison.OrdinalIgnoreCase)) return "Return";
        }
        if (string.Equals(sourceDocumentType, "PaymentVoucher", StringComparison.OrdinalIgnoreCase)) return "Payment";
        if (string.Equals(sourceDocumentType, "SupplierSettlementFx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sourceDocumentType, "SupplierSettlementFxReversal", StringComparison.OrdinalIgnoreCase)) return "RealizedFx";
        return "Adjustment";
    }

    private static string FormatReference(string? sourceDocumentType, Guid? sourceDocumentId, string journalNumber)
        => sourceDocumentId.HasValue && !string.IsNullOrWhiteSpace(sourceDocumentType)
            ? $"{sourceDocumentType}:{sourceDocumentId.Value:D}"
            : journalNumber;

    private static AccountTreeMeta? ResolveCommonControlAccount(
        IReadOnlyCollection<Guid> supplierAccountIds,
        IReadOnlyList<AccountTreeMeta> accounts)
    {
        if (supplierAccountIds.Count == 0) return null;
        var byId = accounts.ToDictionary(x => x.Id);
        HashSet<Guid>? common = null;

        foreach (var supplierAccountId in supplierAccountIds)
        {
            var controls = new HashSet<Guid>();
            var currentId = (Guid?)supplierAccountId;
            while (currentId.HasValue && byId.TryGetValue(currentId.Value, out var current))
            {
                if (current.IsControlAccount) controls.Add(current.Id);
                currentId = current.ParentAccountId;
            }

            common = common is null ? controls : new HashSet<Guid>(common.Intersect(controls));
            if (common.Count == 0) return null;
        }

        return accounts
            .Where(x => common!.Contains(x.Id))
            .OrderByDescending(x => x.Level)
            .FirstOrDefault();
    }

    private static IReadOnlySet<Guid> ResolveSubtree(Guid rootId, IReadOnlyList<AccountTreeMeta> accounts)
    {
        var children = accounts
            .Where(x => x.ParentAccountId.HasValue)
            .GroupBy(x => x.ParentAccountId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(a => a.Id).ToArray());
        var result = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(rootId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!result.Add(id)) continue;
            if (!children.TryGetValue(id, out var nested)) continue;
            foreach (var child in nested) stack.Push(child);
        }
        return result;
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record SupplierAccountMeta(
        Guid SupplierId,
        string SupplierCode,
        string SupplierName,
        Guid AccountId,
        string AccountCode,
        string AccountName);

    private sealed record AccountTreeMeta(
        Guid Id,
        string Code,
        string Name,
        Guid? ParentAccountId,
        byte Level,
        bool IsControlAccount);
}
