using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Reports;
using OAS.Contracts.Sales.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

/// <summary>
/// Read-only sales reporting. Invoice and return line facts are disjoint SQL queries;
/// never join invoice lines to multiple returns or payments (which multiplies totals).
/// Posted snapshots, not today's catalog prices, are the only monetary source.
/// </summary>
public sealed class SalesReportQueryService(OasDbContext db) : ISalesReportQueryService
{
    private const int MaximumBreakdownGroups = 200;
    private static decimal R(decimal x) => Math.Round(x, 4, MidpointRounding.AwayFromZero);

    public async Task<SalesReportDto> GetAsync(SalesReportFilterDto filter, CancellationToken ct = default)
    {
        if (filter.FromDate == default || filter.ToDate == default || filter.ToDate < filter.FromDate)
            throw new ArgumentException("حدد تاريخ بداية ونهاية صحيحين للتقرير.", nameof(filter));
        if (!Enum.IsDefined(filter.GroupBy))
            throw new ArgumentException("نوع تجميع التقرير غير معروف.", nameof(filter));

        var page = Math.Max(1, filter.PageNumber);
        var size = Math.Clamp(filter.PageSize, 1, 100);
        filter = filter with { PageNumber = page, PageSize = size };

        var invoiceFacts =
            from line in db.Set<SalesInvoiceLine>().AsNoTracking()
            join doc in db.Set<SalesInvoice>().AsNoTracking() on line.SalesInvoiceId equals doc.Id
            where doc.Status == SalesInvoiceStatus.Posted && line.IsActive
                  && doc.PostingDate >= filter.FromDate && doc.PostingDate <= filter.ToDate
            select new Fact
            {
                Kind = 1, DocumentId = doc.Id, DocumentCode = doc.InvoiceCode,
                Date = doc.PostingDate, CustomerId = doc.CustomerId,
                EmployeeId = doc.SalesEmployeeId, ProductId = line.ProductVariantId,
                WarehouseId = line.WarehouseId,
                CategoryId = (from variant in db.Set<ProductVariant>()
                              join product in db.Set<Product>() on variant.ProductId equals product.Id
                              where variant.Id == line.ProductVariantId
                              select (Guid?)product.CategoryId).FirstOrDefault(),
                CurrencyCode = doc.CurrencyCodeSnapshot,
                InvoiceRevenue = line.BaseNetAmount, ReturnRevenue = 0m,
                // Invoice-only discount. Return lines keep net monetary snapshots but not
                // an independent original line-discount snapshot; do not double subtract.
                Discount = line.BaseHeaderDiscountAllocated + line.DiscountAmount * doc.ExchangeRate,
                Tax = line.BaseTaxAmount,
                Cogs = line.TotalCostSnapshot ?? 0m
            };

        var returnFacts =
            from line in db.Set<SalesReturnLine>().AsNoTracking()
            join doc in db.Set<SalesReturn>().AsNoTracking() on line.SalesReturnId equals doc.Id
            join invoice in db.Set<SalesInvoice>().AsNoTracking() on doc.SalesInvoiceId equals invoice.Id
            where doc.Status == SalesReturnStatus.Posted && line.IsActive
                  && doc.PostingDate >= filter.FromDate && doc.PostingDate <= filter.ToDate
            select new Fact
            {
                Kind = 2, DocumentId = doc.Id, DocumentCode = doc.ReturnCode,
                Date = doc.PostingDate, CustomerId = doc.CustomerId,
                EmployeeId = invoice.SalesEmployeeId, ProductId = line.ProductVariantId,
                WarehouseId = line.WarehouseId,
                CategoryId = (from variant in db.Set<ProductVariant>()
                              join product in db.Set<Product>() on variant.ProductId equals product.Id
                              where variant.Id == line.ProductVariantId
                              select (Guid?)product.CategoryId).FirstOrDefault(),
                CurrencyCode = doc.CurrencyCodeSnapshot,
                InvoiceRevenue = 0m, ReturnRevenue = line.BaseNetAmount,
                Discount = 0m, Tax = -line.BaseTaxAmount,
                // The posting service reverses COGS only when the return restores
                // an inventory asset. Do not credit COGS for disposal/no-stock.
                Cogs = line.ProductVariantId.HasValue && line.WarehouseId.HasValue &&
                    (line.StockDisposition == SalesReturnStockDisposition.Restock ||
                     line.StockDisposition == SalesReturnStockDisposition.Quarantine)
                    ? -(line.TotalCostSnapshot ?? 0m) : 0m
            };

        invoiceFacts = ApplyFilters(invoiceFacts, filter);
        returnFacts = ApplyFilters(returnFacts, filter);

        // Aggregate each source independently in SQL. This avoids UNION decimal
        // type coercion and EF Core set-operation/GroupBy translation pitfalls.
        var invoiceSums = await SumAsync(invoiceFacts, ct);
        var returnSums = await SumAsync(returnFacts, ct);
        var invoiceCount = await invoiceFacts.Select(x => x.DocumentId).Distinct().LongCountAsync(ct);
        var returnCount = await returnFacts.Select(x => x.DocumentId).Distinct().LongCountAsync(ct);
        var totals = SalesReportArithmetic.Compose(
            invoiceSums.Invoice, returnSums.Returned,
            invoiceSums.Discounts, invoiceSums.Tax + returnSums.Tax,
            invoiceSums.Cogs + returnSums.Cogs, invoiceCount, returnCount);

        // Merge at most page*pageSize independently paged document aggregates,
        // so invoice lines are never multiplied by joined return/payment rows.
        var take = (int)Math.Min((long)page * size, int.MaxValue);
        var invoiceDocs = await DocumentQuery(invoiceFacts).OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.DocumentCode).ThenBy(x => x.DocumentId)
            .Take(take).ToListAsync(ct);
        var returnDocs = await DocumentQuery(returnFacts).OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.DocumentCode).ThenBy(x => x.DocumentId)
            .Take(take).ToListAsync(ct);
        var documentCount = invoiceCount + returnCount;
        var documents = invoiceDocs.Concat(returnDocs)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.DocumentCode)
            .ThenBy(x => x.DocumentId).Skip((page - 1) * size).Take(size)
            .Select(x => new SalesReportDocumentDto(
                x.Kind == 1 ? "SalesInvoice" : "SalesReturn", x.DocumentId, x.DocumentCode,
                x.Date, x.CustomerId, x.EmployeeId, x.CurrencyCode,
                SalesReportArithmetic.Compose(x.Invoice, x.Returned, x.Discounts, x.Tax, x.Cogs,
                    x.Kind == 1 ? 1 : 0, x.Kind == 2 ? 1 : 0))).ToArray();

        var groups = await BreakdownAsync(invoiceFacts, returnFacts, filter.GroupBy, ct);
        var baseCurrency = await (from settings in db.Set<AccountingSettings>().AsNoTracking()
                                  join currency in db.Set<Currency>().AsNoTracking()
                                      on settings.BaseCurrencyId equals currency.Id
                                  select currency.Code).FirstOrDefaultAsync(ct) ?? "BASE";
        var truncated = groups.Count > MaximumBreakdownGroups;
        return new(filter, baseCurrency, totals,
            groups.Take(MaximumBreakdownGroups).ToArray(), documents,
            documentCount, page, size, truncated);
    }

    private static IQueryable<Fact> ApplyFilters(IQueryable<Fact> facts, SalesReportFilterDto filter)
    {
        if (filter.CustomerId.HasValue) facts = facts.Where(x => x.CustomerId == filter.CustomerId.Value);
        if (filter.EmployeeId.HasValue) facts = facts.Where(x => x.EmployeeId == filter.EmployeeId.Value);
        if (filter.ProductVariantId.HasValue) facts = facts.Where(x => x.ProductId == filter.ProductVariantId.Value);
        if (filter.CategoryId.HasValue) facts = facts.Where(x => x.CategoryId == filter.CategoryId.Value);
        if (filter.WarehouseId.HasValue) facts = facts.Where(x => x.WarehouseId == filter.WarehouseId.Value);
        return facts;
    }

    private static async Task<Summary> SumAsync(IQueryable<Fact> facts, CancellationToken ct)
    {
        var data = await facts.GroupBy(x => 1).Select(g => new Summary
        {
            Invoice = g.Sum(x => x.InvoiceRevenue), Returned = g.Sum(x => x.ReturnRevenue),
            Discounts = g.Sum(x => x.Discount), Tax = g.Sum(x => x.Tax), Cogs = g.Sum(x => x.Cogs)
        }).SingleOrDefaultAsync(ct);
        return data ?? new Summary();
    }

    private static IQueryable<DocumentAggregate> DocumentQuery(IQueryable<Fact> facts) =>
        facts.GroupBy(x => new
        {
            x.Kind, x.DocumentId, x.DocumentCode, x.Date,
            x.CustomerId, x.EmployeeId, x.CurrencyCode
        }).Select(g => new DocumentAggregate
        {
            Kind = g.Key.Kind, DocumentId = g.Key.DocumentId, DocumentCode = g.Key.DocumentCode,
            Date = g.Key.Date, CustomerId = g.Key.CustomerId, EmployeeId = g.Key.EmployeeId,
            CurrencyCode = g.Key.CurrencyCode,
            Invoice = g.Sum(x => x.InvoiceRevenue), Returned = g.Sum(x => x.ReturnRevenue),
            Discounts = g.Sum(x => x.Discount), Tax = g.Sum(x => x.Tax), Cogs = g.Sum(x => x.Cogs)
        });

    private async Task<List<SalesReportBreakdownDto>> BreakdownAsync(
        IQueryable<Fact> invoices, IQueryable<Fact> returns,
        SalesReportGroupBy kind, CancellationToken ct)
    {
        if (kind == SalesReportGroupBy.Day)
        {
            var a = await GroupAsync(invoices, x => x.Date, ct);
            var b = await GroupAsync(returns, x => x.Date, ct);
            return Merge(a, b).Select(g => ToRow(g.Key.ToString("yyyy-MM-dd"),
                g.Key.ToString("yyyy-MM-dd"), g)).ToList();
        }
        if (kind == SalesReportGroupBy.Month)
        {
            var a = await GroupAsync(invoices, x => x.Date.Year * 100 + x.Date.Month, ct);
            var b = await GroupAsync(returns, x => x.Date.Year * 100 + x.Date.Month, ct);
            return Merge(a, b).Select(g => ToRow(g.Key.ToString(),
                $"{g.Key / 100:D4}-{g.Key % 100:D2}", g)).ToList();
        }

        Expression<Func<Fact, Guid?>> selector = kind switch
        {
            SalesReportGroupBy.Customer => x => (Guid?)x.CustomerId,
            SalesReportGroupBy.Employee => x => x.EmployeeId,
            SalesReportGroupBy.Product => x => x.ProductId,
            SalesReportGroupBy.Category => x => x.CategoryId,
            SalesReportGroupBy.Warehouse => x => x.WarehouseId,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var list = Merge(await GroupAsync(invoices, selector, ct),
            await GroupAsync(returns, selector, ct));
        var ids = list.Where(x => x.Key.HasValue).Select(x => x.Key!.Value).ToArray();
        var labels = new Dictionary<Guid, string>();
        switch (kind)
        {
            case SalesReportGroupBy.Customer:
                labels = await db.Set<Customer>().AsNoTracking().Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.NameAr, ct); break;
            case SalesReportGroupBy.Employee:
                labels = await db.Set<Employee>().AsNoTracking().Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, ct); break;
            case SalesReportGroupBy.Product:
                labels = await db.Set<ProductVariant>().AsNoTracking().Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.SKU, ct); break;
            case SalesReportGroupBy.Category:
                labels = await db.Set<ProductCategory>().AsNoTracking().Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ct); break;
            case SalesReportGroupBy.Warehouse:
                labels = await db.Set<Warehouse>().AsNoTracking().Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ct); break;
        }
        return list.Select(x => ToRow(x.Key?.ToString("D") ?? "other",
            x.Key.HasValue ? labels.GetValueOrDefault(x.Key.Value, x.Key.Value.ToString("D")) : "غير محدد", x)).ToList();
    }

    private static List<GroupAggregate<TKey>> Merge<TKey>(
        IReadOnlyList<GroupAggregate<TKey>> a, IReadOnlyList<GroupAggregate<TKey>> b) =>
        a.Concat(b).GroupBy(x => x.Key).Select(g => new GroupAggregate<TKey>
        {
            Key = g.Key, Invoice = g.Sum(x => x.Invoice), Returned = g.Sum(x => x.Returned),
            Discounts = g.Sum(x => x.Discounts), Tax = g.Sum(x => x.Tax),
            Cogs = g.Sum(x => x.Cogs), InvoiceCount = g.Sum(x => x.InvoiceCount),
            ReturnCount = g.Sum(x => x.ReturnCount)
        }).OrderByDescending(x => x.Invoice - x.Returned)
          .Take(MaximumBreakdownGroups + 1).ToList();

    private static SalesReportBreakdownDto ToRow<TKey>(string key, string label, GroupAggregate<TKey> row) =>
        new(key, label, SalesReportArithmetic.Compose(row.Invoice, row.Returned,
            row.Discounts, row.Tax, row.Cogs, row.InvoiceCount, row.ReturnCount));

    private static Task<List<GroupAggregate<TKey>>> GroupAsync<TKey>(
        IQueryable<Fact> facts, Expression<Func<Fact, TKey>> key, CancellationToken ct) =>
        facts.GroupBy(key)
            .Select(g => new GroupAggregate<TKey>
            {
                Key = g.Key,
                Invoice = g.Sum(x => x.InvoiceRevenue), Returned = g.Sum(x => x.ReturnRevenue),
                Discounts = g.Sum(x => x.Discount), Tax = g.Sum(x => x.Tax), Cogs = g.Sum(x => x.Cogs),
                InvoiceCount = g.Where(x => x.Kind == 1).Select(x => x.DocumentId).Distinct().LongCount(),
                ReturnCount = g.Where(x => x.Kind == 2).Select(x => x.DocumentId).Distinct().LongCount()
            })
            .OrderByDescending(x => x.Invoice - x.Returned)
            .Take(MaximumBreakdownGroups + 1).ToListAsync(ct);

    private sealed class Summary
    {
        public decimal Invoice { get; init; }
        public decimal Returned { get; init; }
        public decimal Discounts { get; init; }
        public decimal Tax { get; init; }
        public decimal Cogs { get; init; }
    }

    private sealed class DocumentAggregate
    {
        public int Kind { get; init; }
        public Guid DocumentId { get; init; }
        public string DocumentCode { get; init; } = "";
        public DateOnly Date { get; init; }
        public Guid CustomerId { get; init; }
        public Guid? EmployeeId { get; init; }
        public string CurrencyCode { get; init; } = "";
        public decimal Invoice { get; init; }
        public decimal Returned { get; init; }
        public decimal Discounts { get; init; }
        public decimal Tax { get; init; }
        public decimal Cogs { get; init; }
    }

    private sealed class Fact
    {
        public int Kind { get; init; }
        public Guid DocumentId { get; init; }
        public string DocumentCode { get; init; } = "";
        public DateOnly Date { get; init; }
        public Guid CustomerId { get; init; }
        public Guid? EmployeeId { get; init; }
        public Guid? ProductId { get; init; }
        public Guid? CategoryId { get; init; }
        public Guid? WarehouseId { get; init; }
        public string CurrencyCode { get; init; } = "";
        public decimal InvoiceRevenue { get; init; }
        public decimal ReturnRevenue { get; init; }
        public decimal Discount { get; init; }
        public decimal Tax { get; init; }
        public decimal Cogs { get; init; }
    }

    private sealed class GroupAggregate<TKey>
    {
        public TKey Key { get; init; } = default!;
        public decimal Invoice { get; init; }
        public decimal Returned { get; init; }
        public decimal Discounts { get; init; }
        public decimal Tax { get; init; }
        public decimal Cogs { get; init; }
        public long InvoiceCount { get; init; }
        public long ReturnCount { get; init; }
    }
}
