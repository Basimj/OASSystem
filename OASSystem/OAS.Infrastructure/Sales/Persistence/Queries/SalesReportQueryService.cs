using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Reports;
using OAS.Contracts.Sales.Reports;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Identity.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

/// <summary>Reconcile complete posted documents before filtering their lines. No payment/return join multiplies revenue.</summary>
public sealed class SalesReportQueryService(OasDbContext db) : ISalesReportQueryService
{
    private const int MaxLines = 100_000;
    private const int MaxRows = 50_000;
    private static decimal Round(decimal n) => Math.Round(n, 4, MidpointRounding.AwayFromZero);

    public async Task<SalesReportDto> GetAsync(SalesReportFilterDto filter, CancellationToken ct = default)
    {
        if (filter.FromDate == default || filter.ToDate == default || filter.ToDate < filter.FromDate || filter.ToDate == DateOnly.MaxValue)
            throw new ArgumentException("حدد فترة صحيحة للتقرير.");
        if (!Enum.IsDefined(filter.GroupBy)) throw new ArgumentException("نوع التقرير غير معروف.");
        if (filter.GroupBy == SalesReportGroupBy.Collections && (filter.ProductVariantId.HasValue || filter.CategoryId.HasValue || filter.WarehouseId.HasValue || filter.BrandId.HasValue || filter.ProductTypeId.HasValue))
            throw new ArgumentException("تقرير التحصيلات على مستوى الفاتورة؛ لا يدعم فلاتر البنود.");
        filter = filter with { PageNumber = Math.Max(1, filter.PageNumber), PageSize = Math.Clamp(filter.PageSize, 1, 100) };
        var invoices = db.Set<SalesInvoice>().AsNoTracking().Where(x => x.Status == SalesInvoiceStatus.Posted && x.PostingDate >= filter.FromDate && x.PostingDate <= filter.ToDate);
        var returns = db.Set<SalesReturn>().AsNoTracking().Where(x => x.Status == SalesReturnStatus.Posted && x.PostingDate >= filter.FromDate && x.PostingDate <= filter.ToDate);
        var saleQuery = (from l in db.Set<SalesInvoiceLine>().AsNoTracking()
                          join d in invoices on l.SalesInvoiceId equals d.Id
                          where l.IsActive
                          select new Fact { Kind = 1, Id = l.Id, DocId = d.Id, Code = d.InvoiceCode, Date = d.PostingDate, Number = l.LineNumber,
                              Customer = d.CustomerId, Employee = d.SalesEmployeeId, Product = l.ProductVariantId, Warehouse = l.WarehouseId,
                              Currency = d.CurrencyCodeSnapshot, BaseCurrency = d.BaseCurrencyCodeSnapshot, Operator = d.PostedBy, Plan = (int)d.PaymentPlan,
                              Item = l.ProductNameSnapshot, Unit = l.UnitSnapshot, Quantity = l.Quantity, Net = l.BaseNetAmount, Tax = l.BaseTaxAmount,
                              Discount = l.BaseHeaderDiscountAllocated + l.DiscountAmount * d.ExchangeRate,
                              HeaderNet = d.BaseTotalAmount - d.BaseTaxAmount, HeaderTax = d.BaseTaxAmount, HeaderDiscount = d.BaseDiscountAmount + d.BaseHeaderDiscountAmount,
                              Inventory = l.LineType != SalesLineType.Service && l.ProductVariantId.HasValue && l.WarehouseId.HasValue,
                              CostSnapshot = l.TotalCostSnapshot, Journal = d.JournalEntryId });
        var returnQuery = (from l in db.Set<SalesReturnLine>().AsNoTracking()
                              join d in returns on l.SalesReturnId equals d.Id
                              join i in db.Set<SalesInvoice>().AsNoTracking() on d.SalesInvoiceId equals i.Id
                              join original in db.Set<SalesInvoiceLine>().AsNoTracking() on l.SalesInvoiceLineId equals original.Id
                              where l.IsActive
                              select new Fact { Kind = 2, Id = l.Id, DocId = d.Id, Code = d.ReturnCode, Date = d.PostingDate, Number = l.LineNumber,
                                  Customer = d.CustomerId, Employee = i.SalesEmployeeId, Product = l.ProductVariantId, Warehouse = original.WarehouseId,
                                  Currency = d.CurrencyCodeSnapshot, BaseCurrency = d.BaseCurrencyCodeSnapshot, Operator = d.PostedBy, Plan = (int)i.PaymentPlan,
                                  Item = l.ProductNameSnapshot, Unit = original.UnitSnapshot, Quantity = l.Quantity, Net = l.BaseNetAmount, Tax = l.BaseTaxAmount,
                                  HeaderNet = d.BaseNetAmount, HeaderTax = d.BaseTaxAmount, CostSnapshot = l.TotalCostSnapshot,
                                  Inventory = l.LineType != SalesLineType.Service && l.ProductVariantId.HasValue && l.WarehouseId.HasValue &&
                                     (l.StockDisposition == SalesReturnStockDisposition.Restock || l.StockDisposition == SalesReturnStockDisposition.Quarantine),
                                  Journal = d.JournalEntryId });
        // Currency scope is defined over the whole period, independent of the selected customer or item.
        // Count documents, not lines, without loading monetary facts.
        var saleCurrencies = await saleQuery.Select(x => new { x.DocId, x.BaseCurrency }).Distinct()
            .GroupBy(x => x.BaseCurrency).Select(g => new { Code = g.Key, Count = g.Count() }).ToListAsync(ct);
        var returnCurrencies = await returnQuery.Select(x => new { x.DocId, x.BaseCurrency }).Distinct()
            .GroupBy(x => x.BaseCurrency).Select(g => new { Code = g.Key, Count = g.Count() }).ToListAsync(ct);
        var currencyCounts = saleCurrencies.Concat(returnCurrencies).ToArray();
        var baseCode = await (from s in db.Set<AccountingSettings>().AsNoTracking()
                              join c in db.Set<Currency>().AsNoTracking() on s.BaseCurrencyId equals c.Id select c.Code).FirstOrDefaultAsync(ct) ?? "BASE";
        baseCode = SalesReportCurrencyScope.Normalize(baseCode);
        var baseCodes = SalesReportCurrencyScope.Available(currencyCounts.Select(x => x.Code));
        var warnings = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.HistoricalBaseCurrencyCode))
        {
            baseCode = SalesReportCurrencyScope.Select(filter.HistoricalBaseCurrencyCode, baseCode, baseCodes);
            var otherDocuments = currencyCounts.Where(x => !string.IsNullOrWhiteSpace(x.Code) && SalesReportCurrencyScope.Normalize(x.Code) != baseCode).Sum(x => x.Count);
            var unknownDocuments = currencyCounts.Where(x => string.IsNullOrWhiteSpace(x.Code)).Sum(x => x.Count);
            if (otherDocuments > 0)
                warnings.Add($"النتائج تخص العملة الأساسية التاريخية {baseCode} فقط؛ استُبعد {otherDocuments} مستند بعملات أساسية أخرى من كامل الفترة قبل باقي الفلاتر. اختر عملة أخرى من الفلاتر لعرضها. العملات الموجودة: {string.Join("، ", baseCodes)}.");
            if (unknownDocuments > 0)
                warnings.Add($"استُبعد {unknownDocuments} مستند بلا عملة أساسية تاريخية محفوظة؛ لم تُفترض له عملة أو قيمة بديلة.");
            saleQuery = saleQuery.Where(x => x.BaseCurrency.Trim().ToUpper() == baseCode);
            returnQuery = returnQuery.Where(x => x.BaseCurrency.Trim().ToUpper() == baseCode);
            if (!baseCodes.Contains(baseCode) && baseCodes.Length > 0)
                warnings.Add($"لا توجد مستندات بالعملة الأساسية التاريخية {baseCode} في الفترة المحددة.");
        }
        else
        {
            if (baseCodes.Length > 1) throw new ArgumentException("تحتوي الفترة على أكثر من عملة أساسية تاريخية؛ حدد فترة متجانسة.");
            if (baseCodes.Length == 1) baseCode = baseCodes[0];
        }

        // Keep selectors independent of the applied customer/item filters. Only lightweight distinct metadata is loaded here.
        async Task<T[]> Keys<T>(Expression<Func<Fact,T>> selector)
        {
            var sold = await saleQuery.Select(selector).Distinct().ToListAsync(ct);
            var returned = await returnQuery.Select(selector).Distinct().ToListAsync(ct);
            return sold.Concat(returned).Distinct().ToArray();
        }
        var optionCustomers = await Keys(x => (Guid?)x.Customer);
        var optionEmployees = await Keys(x => x.Employee);
        var optionProducts = await Keys(x => x.Product);
        var optionWarehouses = await Keys(x => x.Warehouse);
        var optionCurrencies = await Keys(x => x.Currency);
        var optionOperators = await Keys(x => x.Operator);

        // Select documents with matching lines, then fetch ALL their active lines.
        // Reconciliation of header amounts below must happen before the final line filter.
        var matchingVariants = from v in db.Set<ProductVariant>().AsNoTracking()
                               join p in db.Set<Product>().AsNoTracking() on v.ProductId equals p.Id
                               join c in db.Set<ProductCategory>().AsNoTracking() on p.CategoryId equals c.Id
                               where (!filter.ProductVariantId.HasValue || v.Id == filter.ProductVariantId)
                                  && (!filter.CategoryId.HasValue || c.Id == filter.CategoryId)
                                  && (!filter.ProductTypeId.HasValue || c.ProductTypeId == filter.ProductTypeId)
                                  && (!filter.BrandId.HasValue || p.BrandId == filter.BrandId)
                               select v.Id;
        var itemFilter = filter.ProductVariantId.HasValue || filter.CategoryId.HasValue || filter.ProductTypeId.HasValue || filter.BrandId.HasValue;
        IQueryable<Fact> SelectDocuments(IQueryable<Fact> source)
        {
            var scoped = source.Where(x => (!filter.CustomerId.HasValue || x.Customer == filter.CustomerId)
                && (!filter.EmployeeId.HasValue || x.Employee == filter.EmployeeId)
                && (string.IsNullOrWhiteSpace(filter.Operator) || x.Operator == filter.Operator)
                && (string.IsNullOrWhiteSpace(filter.CurrencyCode) || x.Currency == filter.CurrencyCode)
                && (!filter.PaymentPlan.HasValue || x.Plan == filter.PaymentPlan));
            if (!itemFilter && !filter.WarehouseId.HasValue) return scoped;
            var matching = scoped.Where(x => (!filter.WarehouseId.HasValue || x.Warehouse == filter.WarehouseId)
                && (!itemFilter || (x.Product.HasValue && matchingVariants.Contains(x.Product.Value)))).Select(x => x.DocId);
            return scoped.Where(x => matching.Contains(x.DocId));
        }
        var sale = filter.GroupBy == SalesReportGroupBy.Return ? new List<Fact>() : await SelectDocuments(saleQuery).Take(MaxLines + 1).ToListAsync(ct);
        var returned = filter.GroupBy is SalesReportGroupBy.Invoice or SalesReportGroupBy.Collections ? new List<Fact>() : await SelectDocuments(returnQuery).Take(MaxLines + 1).ToListAsync(ct);
        if (sale.Count + returned.Count > MaxLines) throw new ArgumentException("التقرير يتجاوز 100,000 بند في المستندات المطابقة؛ ضيق الفترة أو اختر عميلاً أو صنفاً أو مخزناً.");
        var all = sale.Concat(returned).ToList();
        if (!string.IsNullOrWhiteSpace(filter.HistoricalBaseCurrencyCode))
            all = all.Where(x => SalesReportCurrencyScope.Normalize(x.BaseCurrency) == baseCode).ToList();
        // Use stable semantic posting roles, not descriptions or the current stock disposition.
        var journalIds = all.Where(x => x.Journal.HasValue).Select(x => x.Journal!.Value).Distinct().ToArray();
        var ledger = await (from l in db.Set<JournalEntryLine>().AsNoTracking()
                            join j in db.Set<JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
                            where journalIds.Contains(j.Id) && (j.Status == JournalEntryStatus.Posted || j.Status == JournalEntryStatus.Reversed)
                            && (l.PostingRole == AccountingPostingRoles.SalesRevenue || l.PostingRole == AccountingPostingRoles.SalesTax || l.PostingRole == AccountingPostingRoles.SalesCOGS)
                            select new { l.JournalEntryId, l.SourceDocumentLineId, l.PostingRole, l.DebitAmount, l.CreditAmount, l.WarehouseId, j.Status }).ToListAsync(ct);
        var byJournal = ledger.ToLookup(x => x.JournalEntryId);
        int fallback = 0, differences = 0, reversedJournals = 0;
        foreach (var group in all.GroupBy(x => (x.Kind, x.DocId)))
        {
            ct.ThrowIfCancellationRequested();
            var lines = group.OrderBy(x => x.Number).ThenBy(x => x.Id).ToArray();
            var first = lines[0];
            var entries = byJournal[first.Journal.GetValueOrDefault()].ToArray();
            var revenues = entries.Where(x => x.PostingRole == AccountingPostingRoles.SalesRevenue).ToArray();
            var sign = first.Kind == 1 ? 1m : -1m;
            var known = revenues.Length > 0;
            if (!known) fallback++;
            if (entries.Any(x => x.Status == JournalEntryStatus.Reversed)) reversedJournals++;
            var net = known ? revenues.Sum(x => (x.CreditAmount - x.DebitAmount) * sign) : first.HeaderNet;
            var tax = known ? entries.Where(x => x.PostingRole == AccountingPostingRoles.SalesTax).Sum(x => (x.CreditAmount - x.DebitAmount) * sign) : first.HeaderTax;
            if (net != first.HeaderNet || tax != first.HeaderTax) differences++;
            var nets = SalesReportArithmetic.ReconcileHeader(lines.Select(x => x.Net).ToArray(), net);
            var taxes = SalesReportArithmetic.ReconcileHeader(lines.Select(x => x.Tax).ToArray(), tax);
            var discounts = SalesReportArithmetic.ReconcileHeader(lines.Select(x => Round(x.Discount)).ToArray(), first.HeaderDiscount);
            var costs = entries.Where(x => x.PostingRole == AccountingPostingRoles.SalesCOGS && x.SourceDocumentLineId.HasValue)
                .GroupBy(x => x.SourceDocumentLineId!.Value).ToDictionary(g => g.Key, g => g.Sum(x => x.DebitAmount - x.CreditAmount));
            for (var n = 0; n < lines.Length; n++)
            {
                var l = lines[n]; l.Net = nets[n]; l.Tax = taxes[n]; l.Discount = discounts[n];
                if (costs.TryGetValue(l.Id, out var cost))
                {
                    l.Cost = cost;
                    if (l.Kind == 2) l.ReturnWarehouse = entries.FirstOrDefault(x => x.SourceDocumentLineId == l.Id && x.PostingRole == AccountingPostingRoles.SalesCOGS)?.WarehouseId;
                }
                else if (l.Inventory)
                {
                    l.Cost = (l.CostSnapshot ?? 0m) * sign;
                    l.MissingCost = !l.CostSnapshot.HasValue || (known && l.CostSnapshot != 0m);
                }
                // A posted no-stock return has no COGS reversal. Historical role takes precedence.
            }
        }
        if (fallback > 0) warnings.Add($"{fallback} مستند بلا أدوار إيراد تاريخية قابلة للتحقق؛ استخدمت قيم الرأس المحفوظة.");
        if (differences > 0) warnings.Add($"{differences} مستند يختلف رأسه عن القيد؛ استخدمت الإيراد والضريبة من القيد.");
        if (reversedJournals > 0) warnings.Add("توجد قيود مبيعات معكوسة خارج دورة المرتجعات؛ التقرير يعرض المستندات الأصلية ولا يمثل تلك القيود العكسية المستقلة.");
        var productIds = optionProducts.OfType<Guid>().ToArray();
        var catalog = await (from v in db.Set<ProductVariant>().AsNoTracking()
                             join p in db.Set<Product>().AsNoTracking() on v.ProductId equals p.Id
                             join c in db.Set<ProductCategory>().AsNoTracking() on p.CategoryId equals c.Id
                             where productIds.Contains(v.Id)
                             select new { v.Id, v.SKU, v.VariantName, p.Name, Category = c.Id, CategoryName = c.Name, Type = c.ProductTypeId, p.BrandId }).ToDictionaryAsync(x => x.Id, ct);
        foreach (var f in all) if (f.Product is Guid id && catalog.TryGetValue(id, out var p)) { f.Category = p.Category; f.ProductType = p.Type; f.Brand = p.BrandId; }
        var customers = await db.Set<Customer>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.NameAr, ct);
        var employees = await db.Set<Employee>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, ct);
        var warehouses = await db.Set<Warehouse>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var brands = await db.Set<Brand>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var types = await db.Set<ProductType>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var users = await db.Set<UserAccount>().AsNoTracking().Select(x => new { x.Id, DisplayName = x.FirstName + " " + x.LastName }).ToListAsync(ct);
        string UserLabel(string? key) => Guid.TryParse(key, out var id) ? users.FirstOrDefault(x => x.Id == id)?.DisplayName ?? key! : key ?? "غير محدد";
        var options = new List<SalesReportOptionDto>();
        options.AddRange(baseCodes.Select(code => new SalesReportOptionDto("historicalBaseCurrency", code, code)));
        void AddOptions(string kind, IEnumerable<Guid?> keys, Func<Guid, string> label) => options.AddRange(keys.Where(x => x.HasValue).Select(x => x!.Value).Distinct().Select(x => new SalesReportOptionDto(kind, x.ToString("D"), label(x))));
        AddOptions("customer", optionCustomers, id => customers.GetValueOrDefault(id, id.ToString()));
        AddOptions("employee", optionEmployees, id => employees.GetValueOrDefault(id, id.ToString()));
        AddOptions("warehouse", optionWarehouses, id => warehouses.GetValueOrDefault(id, id.ToString()));
        AddOptions("brand", catalog.Values.Select(x => x.BrandId), id => brands.GetValueOrDefault(id, id.ToString()));
        AddOptions("type", catalog.Values.Select(x => (Guid?)x.Type), id => types.GetValueOrDefault(id, id.ToString()));
        options.AddRange(catalog.Values.GroupBy(x => x.Category).Select(g => new SalesReportOptionDto("category", g.Key.ToString("D"), g.First().CategoryName, TypeKey: g.First().Type.ToString("D"))));
        options.AddRange(catalog.Values.Select(x => new SalesReportOptionDto("product", x.Id.ToString("D"), $"{x.Name} - {x.SKU} {x.VariantName}", x.Category.ToString("D"), x.Type.ToString("D"))));
        options.AddRange(optionCurrencies.Select(x => new SalesReportOptionDto("currency", x, x)));
        options.AddRange(optionOperators.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Select(x => new SalesReportOptionDto("operator", x!, UserLabel(x))));
        var facts = all.Where(x => (!filter.CustomerId.HasValue || x.Customer == filter.CustomerId) && (!filter.EmployeeId.HasValue || x.Employee == filter.EmployeeId)
            && (!filter.ProductVariantId.HasValue || x.Product == filter.ProductVariantId) && (!filter.CategoryId.HasValue || x.Category == filter.CategoryId)
            && (!filter.WarehouseId.HasValue || x.Warehouse == filter.WarehouseId) && (!filter.ProductTypeId.HasValue || x.ProductType == filter.ProductTypeId)
            && (!filter.BrandId.HasValue || x.Brand == filter.BrandId) && (string.IsNullOrWhiteSpace(filter.Operator) || x.Operator == filter.Operator)
            && (string.IsNullOrWhiteSpace(filter.CurrencyCode) || x.Currency == filter.CurrencyCode) && (!filter.PaymentPlan.HasValue || x.Plan == filter.PaymentPlan)).ToList();
        if (filter.GroupBy is SalesReportGroupBy.Invoice or SalesReportGroupBy.Collections) facts = facts.Where(x => x.Kind == 1).ToList();
        if (filter.GroupBy == SalesReportGroupBy.Return) facts = facts.Where(x => x.Kind == 2).ToList();
        var optionLabels = options.ToDictionary(o => (o.Kind, o.Key), o => o.Label);
        string Label(string kind, string key) => optionLabels.GetValueOrDefault((kind, key), key == "" ? "غير محدد" : key);
        static string G(Guid? id) => id?.ToString("D") ?? "";
        string Group(Fact f) => filter.GroupBy switch {
            SalesReportGroupBy.Summary => "كامل الفترة", SalesReportGroupBy.Day => f.Date.ToString("yyyy-MM-dd"), SalesReportGroupBy.Month => f.Date.ToString("yyyy-MM"), SalesReportGroupBy.Year => f.Date.Year.ToString(),
            SalesReportGroupBy.Customer => G(f.Customer), SalesReportGroupBy.Employee => G(f.Employee), SalesReportGroupBy.Product => G(f.Product),
            SalesReportGroupBy.Category => G(f.Category), SalesReportGroupBy.Warehouse => G(f.Warehouse), SalesReportGroupBy.ProductType => G(f.ProductType), SalesReportGroupBy.Brand => G(f.Brand),
            SalesReportGroupBy.Operator => f.Operator ?? "", SalesReportGroupBy.Currency => f.Currency, SalesReportGroupBy.PaymentPlan => f.Plan.ToString(),
            SalesReportGroupBy.Line => f.Kind + ":" + f.Id.ToString("D"), SalesReportGroupBy.Invoice or SalesReportGroupBy.Return or SalesReportGroupBy.Collections or SalesReportGroupBy.Discounts or SalesReportGroupBy.Taxes => f.Kind + ":" + f.DocId.ToString("D"),
            SalesReportGroupBy.Cost or SalesReportGroupBy.Profit => G(f.Product), _ => f.Date.ToString("yyyy-MM-dd") };
        bool detail = filter.GroupBy is SalesReportGroupBy.Line or SalesReportGroupBy.Invoice or SalesReportGroupBy.Return or SalesReportGroupBy.Collections or SalesReportGroupBy.Discounts or SalesReportGroupBy.Taxes;
        var rows = facts.GroupBy(Group).Select(g => {
            var f = g.First(); var key = g.Key; var label = filter.GroupBy switch {
                SalesReportGroupBy.Customer => Label("customer", key), SalesReportGroupBy.Employee => Label("employee", key), SalesReportGroupBy.Product or SalesReportGroupBy.Cost or SalesReportGroupBy.Profit => Label("product", key),
                SalesReportGroupBy.Category => Label("category", key), SalesReportGroupBy.Warehouse => Label("warehouse", key), SalesReportGroupBy.ProductType => Label("type", key), SalesReportGroupBy.Brand => Label("brand", key), SalesReportGroupBy.Operator => Label("operator", key),
                SalesReportGroupBy.PaymentPlan => key switch { "1" => "فوري كامل", "2" => "جزئي", "4" => "آجل", _ => "غير محدد" }, _ => detail ? f.Code : key };
            return new SalesReportBreakdownDto(key, label, Total(g)) { DocumentId = detail ? f.DocId : null, DocumentKind = detail ? (f.Kind == 1 ? "SalesInvoice" : "SalesReturn") : null,
                JournalId = detail ? f.Journal : null, PostingDate = detail ? f.Date : null, CustomerName = detail ? customers.GetValueOrDefault(f.Customer) : null,
                ItemName = filter.GroupBy == SalesReportGroupBy.Line ? f.Item : null,
                UnitName = filter.GroupBy == SalesReportGroupBy.Line ? f.Unit : filter.GroupBy == SalesReportGroupBy.Product && g.Select(x => x.Unit).Distinct().Count() == 1 ? f.Unit : null,
                CurrencyCode = detail ? f.Currency : null, LineNumber = filter.GroupBy == SalesReportGroupBy.Line ? f.Number : null,
                SaleWarehouseName = filter.GroupBy == SalesReportGroupBy.Line && f.Warehouse.HasValue ? warehouses.GetValueOrDefault(f.Warehouse.Value) : null,
                ReturnWarehouseName = filter.GroupBy == SalesReportGroupBy.Line && f.ReturnWarehouse.HasValue ? warehouses.GetValueOrDefault(f.ReturnWarehouse.Value) : null };
        }).ToList();
        if (filter.GroupBy == SalesReportGroupBy.Collections)
        {
            rows = await CollectionsAsync(rows, facts, filter, ct);
            var included = rows.Select(x => x.DocumentId).ToHashSet(); facts = facts.Where(x => included.Contains(x.DocId)).ToList();
        }
        var docs = facts.GroupBy(x => (x.Kind, x.DocId)).Select(g => (First: g.First(), Totals: Total(g))).OrderByDescending(x => x.First.Date).ThenBy(x => x.First.Code).ThenBy(x => x.First.DocId).ToList();
        var documentRows = docs.Skip((int)Math.Min((long)(filter.PageNumber - 1) * filter.PageSize, int.MaxValue)).Take(filter.PageSize)
            .Select(x => new SalesReportDocumentDto(x.First.Kind == 1 ? "SalesInvoice" : "SalesReturn", x.First.DocId, x.First.Code, x.First.Date, x.First.Customer, x.First.Employee, x.First.Currency, x.Totals) { CustomerName = customers.GetValueOrDefault(x.First.Customer) }).ToArray();
        if (rows.Count > MaxRows) throw new ArgumentException("التقرير يتجاوز 50,000 صف؛ ضيق نطاق التاريخ أو الفلاتر.");
        var totals = Total(facts);
        if (totals.MissingCostCount > 0) warnings.Add($"{totals.MissingCostCount} بند بتكلفة غير مكتملة أو غير مطابقة للقيد؛ الربح والهامش غير متاحين للمجاميع المتأثرة.");
        if (totals.NetRevenueBase <= 0 && filter.GroupBy != SalesReportGroupBy.Collections) warnings.Add("هامش الربح غير معبّر عندما يكون صافي المبيعات صفرًا أو سالبًا.");
        if (filter.GroupBy == SalesReportGroupBy.Collections) warnings.Add("التحصيلات هي تسويات الفواتير الصادرة خلال الفترة حتى تاريخ نهايتها (UTC للتخصيصات)، وليست حركة الصندوق خلال الفترة.");
        if (filter.GroupBy is SalesReportGroupBy.Category or SalesReportGroupBy.ProductType or SalesReportGroupBy.Brand || filter.CategoryId.HasValue || filter.ProductTypeId.HasValue || filter.BrandId.HasValue) warnings.Add("تصنيف المنتج ونوعه وعلامته من الكتالوج الحالي؛ قد يتغير التجميع التاريخي عند إعادة تصنيف المنتج.");
        rows = (filter.GroupBy is SalesReportGroupBy.Day or SalesReportGroupBy.Month or SalesReportGroupBy.Year or SalesReportGroupBy.Summary ? rows.OrderBy(x => x.Key) : detail ? rows.OrderByDescending(x => x.PostingDate).ThenBy(x => x.Label).ThenBy(x => x.Key) : rows.OrderByDescending(x => x.Totals.NetRevenueBase).ThenBy(x => x.Key)).ToList();
        return new(filter, baseCode, totals, rows, documentRows, docs.Count, filter.PageNumber, filter.PageSize, false) { Options = options.OrderBy(x => x.Label).ToArray(), Warnings = warnings };
    }

    private async Task<List<SalesReportBreakdownDto>> CollectionsAsync(List<SalesReportBreakdownDto> rows, List<Fact> facts, SalesReportFilterDto filter, CancellationToken ct)
    {
        var ids = rows.Select(x => x.DocumentId!.Value).ToArray();
        var cutoff = filter.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var payments = await db.Set<PaymentAllocation>().AsNoTracking().Where(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice && ids.Contains(x.TargetDocumentId) && x.AllocatedAtUtc < cutoff && (!x.ReversedAtUtc.HasValue || x.ReversedAtUtc.Value >= cutoff))
            .GroupBy(x => x.TargetDocumentId).Select(g => new { Id = g.Key, Amount = g.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount ?? (x.AllocatedAmount * (x.ExchangeRate ?? 1m))) }).ToDictionaryAsync(x => x.Id, x => x.Amount, ct);
        var returns = await db.Set<SalesReturn>().AsNoTracking().Where(x => ids.Contains(x.SalesInvoiceId) && x.Status == SalesReturnStatus.Posted && x.PostingDate <= filter.ToDate)
            .GroupBy(x => x.SalesInvoiceId).Select(g => new { Id = g.Key, Amount = g.Sum(x => x.BaseTotalAmount) }).ToDictionaryAsync(x => x.Id, x => x.Amount, ct);
        var credits = await (from a in db.Set<CustomerCreditApplication>().AsNoTracking() join r in db.Set<SalesReturn>().AsNoTracking() on a.SalesReturnId equals r.Id
                             where a.SettlementKind == CustomerCreditSettlementKind.ApplyToInvoice && a.TargetSalesInvoiceId.HasValue && ids.Contains(a.TargetSalesInvoiceId.Value) && a.TargetSalesInvoiceId.Value != r.SalesInvoiceId && a.CustomerId == r.CustomerId && r.Status == SalesReturnStatus.Posted && r.PostingDate <= filter.ToDate && a.AppliedAtUtc < cutoff && (!a.ReversedAtUtc.HasValue || a.ReversedAtUtc.Value >= cutoff)
                             group a by a.TargetSalesInvoiceId!.Value into g select new { Id = g.Key, Amount = g.Sum(x => x.TargetBaseAmount ?? 0m) }).ToDictionaryAsync(x => x.Id, x => x.Amount, ct);
        var headers = facts.GroupBy(x => x.DocId).ToDictionary(g => g.Key, g => g.First().HeaderNet + g.First().HeaderTax);
        return rows.Select(r => { var id = r.DocumentId!.Value; var total = headers[id]; var returned = Math.Min(total, Math.Max(0, returns.GetValueOrDefault(id))); var paid = payments.GetValueOrDefault(id); var credit = credits.GetValueOrDefault(id); return r with { InvoiceTotalBase = total, ReturnedTotalBase = returned, AllocatedBase = paid, CreditAppliedBase = credit, OutstandingBase = Math.Max(0, total - returned - paid - credit) }; })
            .Where(r => filter.SettlementStatus switch { "paid" => r.OutstandingBase == 0, "partial" => r.OutstandingBase > 0 && (r.AllocatedBase + r.CreditAppliedBase) > 0, "unpaid" => r.OutstandingBase > 0 && (r.AllocatedBase + r.CreditAppliedBase) <= 0, _ => true }).ToList();
    }
    private static SalesReportTotalsDto Total(IEnumerable<Fact> source)
    {
        var f = source.ToArray(); return SalesReportArithmetic.Compose(f.Where(x => x.Kind == 1).Sum(x => x.Net), f.Where(x => x.Kind == 2).Sum(x => x.Net), f.Sum(x => x.Discount), f.Sum(x => x.Kind == 1 ? x.Tax : -x.Tax), f.Sum(x => x.Cost), f.Where(x => x.Kind == 1).Select(x => x.DocId).Distinct().LongCount(), f.Where(x => x.Kind == 2).Select(x => x.DocId).Distinct().LongCount()) with { MissingCostCount = f.Count(x => x.MissingCost), SoldQuantity = f.Where(x => x.Kind == 1).Sum(x => x.Quantity), ReturnedQuantity = f.Where(x => x.Kind == 2).Sum(x => x.Quantity) };
    }
    private sealed class Fact
    {
        public int Kind { get; set; } public Guid Id { get; set; } public Guid DocId { get; set; } public string Code { get; set; } = ""; public DateOnly Date { get; set; } public int Number { get; set; }
        public Guid Customer { get; set; } public Guid? Employee { get; set; } public Guid? Product { get; set; } public Guid? Warehouse { get; set; } public Guid? ReturnWarehouse { get; set; } public Guid? Category { get; set; } public Guid? ProductType { get; set; } public Guid? Brand { get; set; }
        public string Currency { get; set; } = ""; public string BaseCurrency { get; set; } = ""; public string? Operator { get; set; } public int Plan { get; set; } public string? Item { get; set; } public string? Unit { get; set; }
        public decimal Quantity { get; set; } public decimal Net { get; set; } public decimal Tax { get; set; } public decimal Discount { get; set; } public decimal HeaderNet { get; set; } public decimal HeaderTax { get; set; } public decimal HeaderDiscount { get; set; }
        public bool Inventory { get; set; } public decimal? CostSnapshot { get; set; } public decimal Cost { get; set; } public bool MissingCost { get; set; } public Guid? Journal { get; set; }
    }
}
