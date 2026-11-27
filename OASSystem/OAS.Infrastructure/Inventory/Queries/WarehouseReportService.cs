using Microsoft.EntityFrameworkCore;
using OAS.Application.Inventory.Reports;
using OAS.Contracts.Inventory.Reports;
using OAS.Domain.Enums.Inventory;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Inventory.Queries;

public sealed class WarehouseReportService(OasDbContext db) : IWarehouseReportService
{
    public async Task<WarehouseReportDto> GetAsync(GetWarehouseReportQuery q, CancellationToken ct)
    {
        var start = new DateTimeOffset(q.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(q.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var warehouses = await db.Warehouses.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new WarehouseReportWarehouse(x.Id, x.Code, x.Name, x.IsActive)).ToListAsync(ct);
        var categories = await db.ProductCategories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new MovementReportOption(x.Id, x.Code, x.Name)).ToListAsync(ct);
        var units = await db.Units.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var definitions = from v in db.ProductVariants.AsNoTracking()
                          join p in db.Products.AsNoTracking() on v.ProductId equals p.Id
                          where p.IsStockItem
                          select new { v.Id, v.ProductId, p.CategoryId, v.SKU, v.VariantName, p.ProductCode, p.Name, v.UnitId };
        var all = await definitions.OrderBy(x => x.SKU).ToListAsync(ct);
        var items = all.Select(x => new MovementReportItem(x.Id,x.ProductId,x.CategoryId,x.SKU,
            string.IsNullOrWhiteSpace(x.VariantName)?x.Name:x.VariantName!,x.ProductCode,x.Name,x.UnitId,
            x.UnitId is Guid u && units.TryGetValue(u,out var n)?n:"غير محددة")).ToArray();
        var selected = definitions.Where(x => (!q.VariantId.HasValue || x.Id == q.VariantId.Value) &&
            (!q.ProductId.HasValue || x.ProductId == q.ProductId.Value) && (!q.CategoryId.HasValue || x.CategoryId == q.CategoryId.Value)).Select(x => x.Id);
        var stocks = new List<WarehouseReportStock>();
        var movements = new List<WarehouseReportMovement>();
        var transfers = new List<WarehouseReportTransfer>();
        var counts = new List<WarehouseReportCount>();
        if ((q.Mode is "summary" or "details") && q.Current)
        {
            stocks = await db.InventoryBalances.AsNoTracking().Where(x => selected.Contains(x.ProductVariantId) &&
                (!q.WarehouseId.HasValue || x.WarehouseId == q.WarehouseId.Value))
                .Select(x => new WarehouseReportStock(x.WarehouseId,x.ProductVariantId,x.OnHandQuantity,x.ReservedQuantity,
                    x.OnOrderQuantity,x.AverageUnitCost,x.InventoryValue,x.LastMovementAtUtc)).ToListAsync(ct);
        }
        else if (q.Mode == "transfers")
        {
            var lines = await (from t in db.InventoryTransactions.AsNoTracking()
                join l in db.InventoryTransactionLines.AsNoTracking() on t.Id equals l.TransactionId
                where t.TransactionType == InventoryTransactionType.Transfer && t.TransactionDate >= start && t.TransactionDate < end &&
                    (!q.WarehouseId.HasValue || t.SourceWarehouseId == q.WarehouseId || t.DestinationWarehouseId == q.WarehouseId) && selected.Contains(l.ProductVariantId)
                select new {t.Id,t.TransactionNumber,t.TransactionDate,t.SourceWarehouseId,t.DestinationWarehouseId,t.Status,LineId=l.Id,l.ProductVariantId,l.Quantity}).ToListAsync(ct);
            // Only the source leg is used to value a posted transfer; draft cost is unknown.
            var costs = await (from l in db.InventoryLedger.AsNoTracking()
                join t in db.InventoryTransactions.AsNoTracking() on l.TransactionId equals t.Id
                where t.TransactionType == InventoryTransactionType.Transfer && t.Status == InventoryTransactionStatus.Posted &&
                    t.TransactionDate >= start && t.TransactionDate < end && l.WarehouseId == t.SourceWarehouseId && l.QuantityOut > 0 &&
                    (!q.WarehouseId.HasValue || t.SourceWarehouseId == q.WarehouseId || t.DestinationWarehouseId == q.WarehouseId) && selected.Contains(l.ProductVariantId)
                group l by l.TransactionLineId into g select new {Id=g.Key,Cost=g.Sum(x=>x.QuantityOut*x.UnitCost)}).ToDictionaryAsync(x=>x.Id,x=>x.Cost,ct);
            transfers = lines.Select(x=>new WarehouseReportTransfer(x.Id,x.TransactionNumber,x.TransactionDate,x.SourceWarehouseId,x.DestinationWarehouseId,
                x.ProductVariantId,x.Quantity,x.Status==InventoryTransactionStatus.Posted && costs.TryGetValue(x.LineId,out var cost)?cost:null,(int)x.Status)).ToList();
        }
        else if (q.Mode == "counts")
        {
            counts = await (from h in db.StockCounts.AsNoTracking()
                join l in db.StockCountLines.AsNoTracking() on h.Id equals l.StockCountId
                where h.CountDate >= q.FromDate && h.CountDate <= q.ToDate && (!q.WarehouseId.HasValue || h.WarehouseId == q.WarehouseId.Value) && selected.Contains(l.ProductVariantId)
                select new WarehouseReportCount(h.Id,h.CountNumber,h.CountDate,h.WarehouseId,l.ProductVariantId,l.SystemQuantity,
                    l.CountedAtUtc.HasValue?(decimal?)l.CountedQuantity:null,l.CountedAtUtc.HasValue?(decimal?)l.DifferenceQuantity:null,
                    l.AverageCostSnapshot,l.CountedAtUtc.HasValue?(decimal?)l.VarianceValue:null,(int)h.Status)).ToListAsync(ct);
        }
        else
        {
            var ledger = from l in db.InventoryLedger.AsNoTracking()
                         join t in db.InventoryTransactions.AsNoTracking() on l.TransactionId equals t.Id
                         where t.Status == InventoryTransactionStatus.Posted && selected.Contains(l.ProductVariantId) &&
                             (!q.WarehouseId.HasValue || l.WarehouseId == q.WarehouseId.Value)
                         select new { l.WarehouseId,l.ProductVariantId,l.SequenceNumber,l.MovementDate,l.QuantityIn,l.QuantityOut,l.UnitCost,
                             l.InventoryValueAfter,l.BalanceAfter,t.TransactionType,t.ReferenceType };
            // Snapshot values are trustworthy only when effective-date cutoff does not split posting order.
            var summaries = await ledger.GroupBy(x=>new {x.WarehouseId,x.ProductVariantId})
                .Select(g=>new {g.Key.WarehouseId,g.Key.ProductVariantId,
                    BeforeSeq=g.Max(x=>x.MovementDate<start?(long?)x.SequenceNumber:null),
                    ClosingSeq=g.Max(x=>x.MovementDate<end?(long?)x.SequenceNumber:null),
                    AfterStart=g.Min(x=>x.MovementDate>=start?(long?)x.SequenceNumber:null),
                    AfterEnd=g.Min(x=>x.MovementDate>=end?(long?)x.SequenceNumber:null),
                    Last=g.Max(x=>x.MovementDate<end?(DateTimeOffset?)x.MovementDate:null)}).ToListAsync(ct);
            // Sequence numbers are globally unique; query the grouped key maxima without large IN lists.
            var closingKeys = ledger.Where(x=>x.MovementDate<end).GroupBy(x=>new{x.WarehouseId,x.ProductVariantId}).Select(g=>g.Max(x=>x.SequenceNumber));
            var openingKeys = ledger.Where(x=>x.MovementDate<start).GroupBy(x=>new{x.WarehouseId,x.ProductVariantId}).Select(g=>g.Max(x=>x.SequenceNumber));
            var snapshots = await db.InventoryLedger.AsNoTracking().Where(x=>closingKeys.Contains(x.SequenceNumber)||openingKeys.Contains(x.SequenceNumber))
                .Select(x=>new{x.SequenceNumber,x.InventoryValueAfter}).ToDictionaryAsync(x=>x.SequenceNumber,x=>x.InventoryValueAfter,ct);
            var groups = await ledger.Where(x=>x.MovementDate<end).GroupBy(x=>new{x.WarehouseId,x.ProductVariantId,Before=x.MovementDate<start,x.TransactionType,Purchase=x.ReferenceType=="PurchaseReceipt"})
                .Select(g=>new{g.Key.WarehouseId,g.Key.ProductVariantId,g.Key.Before,g.Key.TransactionType,g.Key.Purchase,
                    In=g.Sum(x=>x.QuantityIn),Out=g.Sum(x=>x.QuantityOut),InCost=g.Sum(x=>x.QuantityIn*x.UnitCost),OutCost=g.Sum(x=>x.QuantityOut*x.UnitCost)}).ToListAsync(ct);
            var byPair=groups.ToLookup(x=>(x.WarehouseId,x.ProductVariantId));
            decimal? Snapshot(long? seq,long? excluded) => !seq.HasValue?0m:excluded.HasValue&&excluded.Value<seq.Value?null:snapshots.TryGetValue(seq.Value,out var v)?v:null;
            foreach(var s in summaries.Where(x=>x.ClosingSeq.HasValue))
            {
                var qty=new Flow();var cost=new Flow();decimal opening=0,closing=0;
                foreach(var g in byPair[(s.WarehouseId,s.ProductVariantId)])
                {
                    closing+=g.In-g.Out;
                    if(g.Before){opening+=g.In-g.Out;continue;}
                    qty.Add(g.TransactionType,g.Purchase,g.In,g.Out);cost.Add(g.TransactionType,g.Purchase,g.InCost,g.OutCost);
                }
                var closingCost=Snapshot(s.ClosingSeq,s.AfterEnd);
                // Include cutoff stock with movement reports too; both views share the same snapshots.
                stocks.Add(new(s.WarehouseId,s.ProductVariantId,closing,null,null,closing>0?closingCost/closing:null,closingCost,s.Last));
                if(q.Mode is not ("summary" or "details")) movements.Add(new(s.WarehouseId,s.ProductVariantId,qty.Output(opening,closing),cost.Output(Snapshot(s.BeforeSeq,s.AfterStart),closingCost)));
            }
        }
        return new(warehouses,categories,items,stocks,movements,transfers,counts);
    }
    private sealed class Flow
    {
        decimal purchases,otherIn,transferIn,salesReturns,sales,purchaseReturns,transferOut,otherOut;
        public void Add(InventoryTransactionType kind,bool purchase,decimal inbound,decimal outbound)
        {
            if(kind==InventoryTransactionType.Receipt && purchase) purchases+=inbound;
            else if(kind==InventoryTransactionType.Transfer) transferIn+=inbound;
            else if(kind==InventoryTransactionType.SalesReturn) salesReturns+=inbound;
            else otherIn+=inbound;
            if(kind==InventoryTransactionType.Transfer) transferOut+=outbound;
            else if(kind==InventoryTransactionType.Sale) sales+=outbound;
            else if(kind==InventoryTransactionType.PurchaseReturn) purchaseReturns+=outbound;
            else otherOut+=outbound;
        }
        public WarehouseFlow Output(decimal? opening,decimal? closing)=>new(opening,purchases,otherIn,transferIn,salesReturns,sales,purchaseReturns,transferOut,otherOut,closing);
    }
}
