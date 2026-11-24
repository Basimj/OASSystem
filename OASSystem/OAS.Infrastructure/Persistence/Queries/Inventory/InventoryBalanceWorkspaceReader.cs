using Microsoft.EntityFrameworkCore;
using OAS.Application.Inventory.Balances.Workspace;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Accounting.Entities;
using System.Linq.Expressions;

namespace OAS.Infrastructure.Persistence.Queries.Inventory;

public sealed class InventoryBalanceWorkspaceReader(OasDbContext db) : IInventoryBalanceWorkspaceReader
{
    private IQueryable<InventoryBalanceWorkspaceRow> Query(PageRequest request, InventoryBalanceFilters f)
    {
        var term = request.Normalize().Search;
        var productTerm = f.ProductSearch?.Trim();
        var query = from v in db.ProductVariants.AsNoTracking()
                    join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
                    join c in db.ProductCategories.AsNoTracking() on p.CategoryId equals c.Id
                    join u0 in db.Units.AsNoTracking() on v.UnitId equals (Guid?)u0.Id into units
                    from u in units.DefaultIfEmpty()
                    from w in db.Warehouses.AsNoTracking()
                    join b0 in db.InventoryBalances.AsNoTracking() on new { WarehouseId = w.Id, ProductVariantId = v.Id } equals new { b0.WarehouseId, b0.ProductVariantId } into balances
                    from b in balances.DefaultIfEmpty()
                    where (!f.WarehouseId.HasValue || w.Id == f.WarehouseId)
                        && (!f.ProductTypeId.HasValue || c.ProductTypeId == f.ProductTypeId)
                        && (!f.CategoryId.HasValue || c.Id == f.CategoryId)
                        && (!f.BrandId.HasValue || p.BrandId == f.BrandId)
                        && (!f.ProductVariantId.HasValue || v.Id == f.ProductVariantId)
                        && ((p.IsStockItem && p.IsActive && v.IsActive && w.IsActive)
                            || (b != null && (b.OnHandQuantity != 0 || b.ReservedQuantity != 0 || b.OnOrderQuantity != 0 || b.InventoryValue != 0)))
                        && (term == null || p.Name.Contains(term) || p.ProductCode.Contains(term) || v.SKU.Contains(term) || (v.VariantName != null && v.VariantName.Contains(term)) || (v.Barcode != null && v.Barcode.Contains(term)))
                        && (productTerm == null || productTerm == "" || p.Name.Contains(productTerm) || p.ProductCode.Contains(productTerm))
                    select new InventoryBalanceWorkspaceRow
                    {
                        WarehouseId = w.Id, WarehouseName = w.Name, WarehouseActive = w.IsActive,
                        ProductId = p.Id, ProductName = p.Name, ProductCode = p.ProductCode,
                        VariantId = v.Id, VariantName = v.VariantName, SKU = v.SKU, Barcode = v.Barcode,
                        IsActive = p.IsActive && v.IsActive, UnitId = v.UnitId, UnitName = u == null ? null : u.Name,
                        OnHand = b == null ? 0 : b.OnHandQuantity, Reserved = b == null ? 0 : b.ReservedQuantity,
                        Available = b == null ? 0 : b.OnHandQuantity - b.ReservedQuantity, OnOrder = b == null ? 0 : b.OnOrderQuantity,
                        AverageCost = b == null ? 0 : b.AverageUnitCost, Value = b == null ? 0 : b.InventoryValue,
                        LastMovement = b == null ? null : b.LastMovementAtUtc
                    };
        query = f.BalanceStatus?.ToLowerInvariant() switch
        {
            "available" => query.Where(r => r.Available > 0),
            "zero" => query.Where(r => r.OnHand == 0),
            "reserved" => query.Where(r => r.OnHand > 0 && r.Available == 0),
            _ => query
        };
        query = f.PurchaseStatus?.ToLowerInvariant() switch { "ordered" => query.Where(r=>r.OnOrder>0), "notordered" => query.Where(r=>r.OnOrder==0), _=>query };
        return f.ActivityStatus?.ToLowerInvariant() switch { "active" => query.Where(r=>r.IsActive), "inactive" => query.Where(r=>!r.IsActive), _=>query };
    }
    private static IOrderedQueryable<InventoryBalanceWorkspaceRow> Sort(IQueryable<InventoryBalanceWorkspaceRow> q, PageRequest p)
    {
        IOrderedQueryable<InventoryBalanceWorkspaceRow> Order<T>(Expression<Func<InventoryBalanceWorkspaceRow,T>> key) => p.SortDirection == SortDirection.Descending ? q.OrderByDescending(key) : q.OrderBy(key);
        var ordered = p.SortBy?.Trim().ToLowerInvariant() switch
        {
            "sku" => Order(r=>r.SKU), "warehouse" => Order(r=>r.WarehouseName), "unit" => Order(r=>r.UnitName),
            "onhand" => Order(r=>r.OnHand), "reserved" => Order(r=>r.Reserved), "available" => Order(r=>r.Available),
            "onorder" => Order(r=>r.OnOrder), "cost" => Order(r=>r.AverageCost), "value" => Order(r=>r.Value),
            "lastmovement" => Order(r=>r.LastMovement), _=>Order(r=>r.ProductName)
        };
        return ordered.ThenBy(r=>r.VariantId).ThenBy(r=>r.WarehouseId);
    }
    public async Task<InventoryBalanceWorkspaceDto> ReadAsync(PageRequest request, InventoryBalanceFilters filters, CancellationToken ct)
    {
        var p = request.Normalize();
        var query = Query(p, filters);
        var count = await query.LongCountAsync(ct);
        var variantCount = await query.Select(r=>r.VariantId).Distinct().LongCountAsync(ct);
        var value = await query.SumAsync(r=>r.Value, ct);
        var units = await query.GroupBy(r=>new {r.UnitId,r.UnitName}).Select(g=>new BalanceUnitTotal(g.Key.UnitId,g.Key.UnitName,g.Sum(r=>r.OnHand),g.Sum(r=>r.Reserved),g.Sum(r=>r.Available),g.Sum(r=>r.OnOrder))).ToListAsync(ct);
        var page = (int)Math.Min(p.PageNumber, Math.Max(1L, (count+p.PageSize-1)/p.PageSize));
        var items = await Sort(query,p).Skip(checked((page-1)*p.PageSize)).Take(p.PageSize).ToListAsync(ct);
        var currency = await CurrencyAsync(ct);
        var lookups = new BalanceWorkspaceLookups(
            await db.Warehouses.AsNoTracking().OrderByDescending(w=>w.IsDefault).ThenBy(w=>w.Name).Select(w=>new BalanceFilterOption(w.Id,w.IsActive?w.Name:w.Name+" (غير نشط)",null)).ToListAsync(ct),
            await db.ProductTypes.AsNoTracking().OrderBy(t=>t.Name).Select(t=>new BalanceFilterOption(t.Id,t.Name,null)).ToListAsync(ct),
            await db.ProductCategories.AsNoTracking().OrderBy(c=>c.Name).Select(c=>new BalanceFilterOption(c.Id,c.Name,c.ProductTypeId)).ToListAsync(ct),
            await db.Brands.AsNoTracking().OrderBy(b=>b.Name).Select(b=>new BalanceFilterOption(b.Id,b.Name,null)).ToListAsync(ct));
        return new(new PagedResult<InventoryBalanceWorkspaceRow>{Items=items,PageNumber=page,PageSize=p.PageSize,TotalCount=count},new BalanceWorkspaceSummary(variantCount,count,value,units),currency.Code,currency.Name,lookups);
    }
    public async Task<IReadOnlyList<InventoryBalanceWorkspaceRow>> ExportRowsAsync(PageRequest request, InventoryBalanceFilters filters, CancellationToken ct) =>
        await Sort(Query(request,filters),request).ToListAsync(ct);
    public async Task<(string? Code,string? Name)> CurrencyAsync(CancellationToken ct)
    {
        var currency = await (from s in db.Set<AccountingSettings>().AsNoTracking() join c in db.Currencies.AsNoTracking() on s.BaseCurrencyId equals c.Id where s.Id==AccountingSettings.SingletonId select new {c.Code,c.NameAr}).SingleOrDefaultAsync(ct);
        return (currency?.Code,currency?.NameAr);
    }
}
