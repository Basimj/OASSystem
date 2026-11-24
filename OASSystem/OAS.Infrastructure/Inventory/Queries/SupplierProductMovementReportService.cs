using Microsoft.EntityFrameworkCore;
using OAS.Application.Inventory.Reports;
using OAS.Contracts.Inventory.Reports;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Inventory.Queries;

public sealed class SupplierProductMovementReportService(OasDbContext db) : ISupplierProductMovementReportService
{
    public async Task<SupplierProductMovementReportDto> GetAsync(GetSupplierProductMovementReportQuery q, CancellationToken ct)
    {
        // MovementDate is the ledger's effective date; use UTC calendar boundaries
        // consistently with the purchasing inventory posting service.
        var start = new DateTimeOffset(q.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(q.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var suppliers = await db.Suppliers.AsNoTracking().OrderBy(x=>x.SupplierCode)
            .Select(x=>new MovementReportOption(x.Id,x.SupplierCode,x.NameAr)).ToListAsync(ct);
        var categories = await db.ProductCategories.AsNoTracking().OrderBy(x=>x.Name)
            .Select(x=>new MovementReportOption(x.Id,x.Code,x.Name)).ToListAsync(ct);
        var warehouses = await db.Warehouses.AsNoTracking().OrderBy(x=>x.Name)
            .Select(x=>new MovementReportOption(x.Id,x.Code,x.Name)).ToListAsync(ct);
        var units = await db.Units.AsNoTracking().Select(x=>new {x.Id,x.Name}).ToDictionaryAsync(x=>x.Id,x=>x.Name,ct);
        var definitions = await (from v in db.ProductVariants.AsNoTracking()
                                 join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
                                 where p.IsStockItem
                                 orderby v.SKU
                                 select new {v.Id,v.ProductId,p.CategoryId,v.SKU,v.VariantName,p.ProductCode,p.Name,v.UnitId}).ToListAsync(ct);
        var items = definitions.Select(x=>new MovementReportItem(x.Id,x.ProductId,x.CategoryId,x.SKU,
            string.IsNullOrWhiteSpace(x.VariantName)?x.Name:x.VariantName!,x.ProductCode,x.Name,x.UnitId,
            x.UnitId is Guid u && units.TryGetValue(u,out var n)?n:"غير محددة")).ToArray();

        var ledger = from l in db.InventoryLedger.AsNoTracking()
                     join t in db.InventoryTransactions.AsNoTracking() on l.TransactionId equals t.Id
                     join v in db.ProductVariants.AsNoTracking() on l.ProductVariantId equals v.Id
                     join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
                     where p.IsStockItem && t.Status == InventoryTransactionStatus.Posted && l.MovementDate < end
                         && (!q.VariantId.HasValue || l.ProductVariantId == q.VariantId.Value)
                         && (!q.ProductId.HasValue || p.Id == q.ProductId.Value)
                         && (!q.CategoryId.HasValue || p.CategoryId == q.CategoryId.Value)
                         && (!q.WarehouseId.HasValue || l.WarehouseId == q.WarehouseId.Value)
                     select new { l.ProductVariantId,l.MovementDate,l.QuantityIn,l.QuantityOut,l.UnitCost,
                         t.TransactionType,t.ReferenceType,t.ReferenceId };
        // SQL aggregates scalar movement values before materialization. No nested
        // GroupBy over nullable joined entities and no per-row database requests.
        var all = await ledger.GroupBy(x=>new {x.ProductVariantId,x.TransactionType,PurchaseReceipt=x.ReferenceType=="PurchaseReceipt",Before=x.MovementDate<start})
            .Select(g=>new {g.Key.ProductVariantId,g.Key.TransactionType,g.Key.PurchaseReceipt,g.Key.Before,
                In=g.Sum(x=>x.QuantityIn),Out=g.Sum(x=>x.QuantityOut),
                InCost=g.Sum(x=>x.QuantityIn*x.UnitCost),OutCost=g.Sum(x=>x.QuantityOut*x.UnitCost)}).ToListAsync(ct);
        var receipts = await (from l in ledger
            join r in db.PurchaseReceipts.AsNoTracking() on l.ReferenceId equals (Guid?)r.Id
            where l.MovementDate>=start && l.TransactionType==InventoryTransactionType.Receipt && l.ReferenceType=="PurchaseReceipt"
            group l by new {l.ProductVariantId,r.SupplierId} into g
            select new {g.Key.ProductVariantId,g.Key.SupplierId,Quantity=g.Sum(x=>x.QuantityIn),Cost=g.Sum(x=>x.QuantityIn*x.UnitCost)}).ToListAsync(ct);
        var returns = await (from l in ledger
            join r in db.Set<PurchaseReturn>().AsNoTracking() on l.ReferenceId equals (Guid?)r.Id
            where l.MovementDate>=start && l.TransactionType==InventoryTransactionType.PurchaseReturn && l.ReferenceType=="PurchaseReturn"
            group l by new {l.ProductVariantId,r.SupplierId} into g
            select new {g.Key.ProductVariantId,g.Key.SupplierId,Quantity=g.Sum(x=>x.QuantityOut),Cost=g.Sum(x=>x.QuantityOut*x.UnitCost)}).ToListAsync(ct);

        var totals = all.GroupBy(x=>x.ProductVariantId).ToDictionary(g=>g.Key,g=>
        {
            var qty = new Flow(); var cost = new Flow();
            foreach(var x in g) { qty.Add(x.TransactionType,x.PurchaseReceipt,x.Before,x.In,x.Out); cost.Add(x.TransactionType,x.PurchaseReceipt,x.Before,x.InCost,x.OutCost); }
            return (Qty:qty, Cost:cost);
        });
        var attributed = new Dictionary<(Guid Variant,Guid Supplier),(Flow Qty,Flow Cost)>();
        (Flow Qty,Flow Cost) Get(Guid v,Guid s)
        {
            if(!attributed.TryGetValue((v,s),out var f)) attributed[(v,s)]=f=(new(),new());
            return f;
        }
        foreach(var x in receipts){var f=Get(x.ProductVariantId,x.SupplierId);f.Qty.Purchases+=x.Quantity;f.Cost.Purchases+=x.Cost;}
        foreach(var x in returns){var f=Get(x.ProductVariantId,x.SupplierId);f.Qty.PurchaseReturns+=x.Quantity;f.Cost.PurchaseReturns+=x.Cost;}
        // Catalog associations without transactions may be shown when the user
        // disables "hide empty". They are not proof of stock provenance.
        var catalog = await (from c in db.SupplierCatalogItems.AsNoTracking()
                             join v in db.ProductVariants.AsNoTracking() on c.ProductVariantId equals v.Id
                             join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
                             where p.IsStockItem && (!q.VariantId.HasValue || v.Id==q.VariantId.Value)
                                 && (!q.ProductId.HasValue || p.Id==q.ProductId.Value)
                                 && (!q.CategoryId.HasValue || p.CategoryId==q.CategoryId.Value)
                             select new {c.ProductVariantId,c.SupplierId}).Distinct().ToListAsync(ct);
        foreach(var x in catalog) Get(x.ProductVariantId,x.SupplierId);
        var attributedByVariant=attributed.ToLookup(x=>x.Key.Variant);
        var supplierRows=attributed.Select(x=>new SupplierProductMovementRow(x.Key.Variant,x.Key.Supplier,x.Value.Qty.Output(false),x.Value.Cost.Output(false))).ToList();
        var productRows=new List<SupplierProductMovementRow>();
        foreach(var item in items.Where(x=>(!q.VariantId.HasValue||x.Id==q.VariantId)&&(!q.ProductId.HasValue||x.ProductId==q.ProductId)&&(!q.CategoryId.HasValue||x.CategoryId==q.CategoryId)))
        {
            var f=totals.TryGetValue(item.Id,out var found)?found:(Qty:new Flow(),Cost:new Flow());
            productRows.Add(new(item.Id,null,f.Qty.Output(true),f.Cost.Output(true)));
            // Subtract only verified supplier receipts/returns. Sales, transfers,
            // and opening/closing stock cannot be allocated to suppliers here.
            foreach(var a in attributedByVariant[item.Id])
            {
                f.Qty.Purchases-=a.Value.Qty.Purchases; f.Cost.Purchases-=a.Value.Cost.Purchases;
                f.Qty.PurchaseReturns-=a.Value.Qty.PurchaseReturns; f.Cost.PurchaseReturns-=a.Value.Cost.PurchaseReturns;
            }
            if(f.Qty.HasMovement||f.Cost.HasMovement)
                supplierRows.Add(new(item.Id,null,f.Qty.Output(false),f.Cost.Output(false)));
        }
        return new(suppliers,categories,warehouses,items,productRows,supplierRows);
    }

    private sealed class Flow
    {
        public decimal Opening,Purchases,OtherIn,PurchaseReturns,OtherOut,Sales,SalesReturns;
        public bool HasMovement => Purchases!=0||OtherIn!=0||PurchaseReturns!=0||OtherOut!=0||Sales!=0||SalesReturns!=0;
        public void Add(InventoryTransactionType kind,bool purchaseReceipt,bool before,decimal inbound,decimal outbound)
        {
            if(before){Opening+=inbound-outbound;return;}
            if(kind==InventoryTransactionType.Receipt && purchaseReceipt) Purchases+=inbound;
            else if(kind==InventoryTransactionType.SalesReturn) SalesReturns+=inbound;
            else OtherIn+=inbound;
            if(kind==InventoryTransactionType.PurchaseReturn) PurchaseReturns+=outbound;
            else if(kind==InventoryTransactionType.Sale) Sales+=outbound;
            else OtherOut+=outbound;
        }
        public MovementReportAmounts Output(bool balances)=>new(balances?Opening:null,Purchases,OtherIn,PurchaseReturns,OtherOut,Sales,SalesReturns,
            balances?Opening+Purchases+OtherIn+SalesReturns-PurchaseReturns-OtherOut-Sales:null);
    }
}
