using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;
using DomainOrderStatus = OAS.Domain.Purchasing.Enums.PurchaseOrderStatus;
using DomainReceiptStatus = OAS.Domain.Purchasing.Enums.PurchaseReceiptStatus;
using DomainInvoiceStatus = OAS.Domain.Purchasing.Enums.PurchaseInvoiceStatus;
using DomainMatchStatus = OAS.Domain.Purchasing.Enums.PurchaseMatchStatus;

namespace OAS.Infrastructure.Purchasing.Persistence.Repositories;

public sealed class PurchaseOrderRepository(OasDbContext dbContext) : IPurchaseOrderRepository
{
    private DbSet<PurchaseOrder> Orders => dbContext.Set<PurchaseOrder>();
    private DbSet<PurchaseOrderLine> Lines => dbContext.Set<PurchaseOrderLine>();
    private DbSet<PurchaseOrderLineSource> Sources => dbContext.Set<PurchaseOrderLineSource>();
    private DbSet<PurchaseReceipt> Receipts => dbContext.Set<PurchaseReceipt>();
    private DbSet<PurchaseReceiptLine> ReceiptLines => dbContext.Set<PurchaseReceiptLine>();
    private DbSet<PurchaseInvoiceLine> InvoiceLines => dbContext.Set<PurchaseInvoiceLine>();
    private DbSet<PurchaseInvoice> Invoices => dbContext.Set<PurchaseInvoice>();
    private DbSet<PurchaseInvoiceReceiptAllocation> Allocations => dbContext.Set<PurchaseInvoiceReceiptAllocation>();

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Orders.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<PurchaseOrder?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Orders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<PurchaseOrder?> GetByLineIdAsync(Guid purchaseOrderLineId, CancellationToken cancellationToken = default) => Orders.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Lines.Any(l => l.Id == purchaseOrderLineId), cancellationToken);

    public Task<PagedData<PurchaseOrder>> GetPageAsync(PageRequest request, PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, CancellationToken cancellationToken = default)
    {
        var query = Orders.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == (DomainOrderStatus)(byte)status.Value);
        if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId.Value);
        if (warehouseId.HasValue) query = query.Where(x => x.DestinationWarehouseId == warehouseId.Value);
        query = query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.PurchaseOrderCode);
        return PurchasingRepositoryHelpers.PageAsync(query, request, (q, term) => q.Where(x => x.PurchaseOrderCode.Contains(term) || (x.Notes != null && x.Notes.Contains(term))), cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrderLineSource>> GetSourcesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var lineIds = Lines.AsNoTracking().Where(x => x.PurchaseOrderId == purchaseOrderId).Select(x => x.Id);
        return await Sources.AsNoTracking().Where(x => lineIds.Contains(x.PurchaseOrderLineId)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPostedReceivedBaseQuantitiesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var query = from rl in ReceiptLines.AsNoTracking()
                    join r in Receipts.AsNoTracking() on rl.PurchaseReceiptId equals r.Id
                    join ol in Lines.AsNoTracking() on rl.PurchaseOrderLineId equals ol.Id
                    where ol.PurchaseOrderId == purchaseOrderId && r.Status == DomainReceiptStatus.Posted
                    group rl by rl.PurchaseOrderLineId into g
                    select new { Id = g.Key, Quantity = g.Sum(x => x.BaseAcceptedQuantity) };
        return await query.ToDictionaryAsync(x => x.Id, x => x.Quantity, cancellationToken);
    }

    public Task<bool> HasPostedReceiptAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default) => Receipts.AsNoTracking().AnyAsync(x => x.PurchaseOrderId == purchaseOrderId && x.Status == DomainReceiptStatus.Posted, cancellationToken);

    public async Task<bool> CanCloseAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var order = await Orders.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken);
        if (order is null || order.Status != DomainOrderStatus.FullyReceived) return false;
        if (await Receipts.AsNoTracking().AnyAsync(x => x.PurchaseOrderId == purchaseOrderId && x.Status != DomainReceiptStatus.Posted && x.Status != DomainReceiptStatus.Cancelled, cancellationToken)) return false;
        var poLineIds = order.Lines.Select(x => x.Id).ToArray();
        var invoiceIds = await InvoiceLines.AsNoTracking().Where(x => x.PurchaseOrderLineId.HasValue && poLineIds.Contains(x.PurchaseOrderLineId.Value)).Select(x => x.PurchaseInvoiceId).Distinct().ToListAsync(cancellationToken);
        if (invoiceIds.Count == 0) return false;
        if (await Invoices.AsNoTracking().AnyAsync(x => invoiceIds.Contains(x.Id) && x.Status != DomainInvoiceStatus.Posted, cancellationToken)) return false;
        var invoiceLineIds = await InvoiceLines.AsNoTracking().Where(x => invoiceIds.Contains(x.PurchaseInvoiceId)).Select(x => x.Id).ToListAsync(cancellationToken);
        return !await Allocations.AsNoTracking().AnyAsync(x => invoiceLineIds.Contains(x.PurchaseInvoiceLineId) && (x.MatchStatus == DomainMatchStatus.Pending || x.MatchStatus == DomainMatchStatus.RequiresApproval || x.MatchStatus == DomainMatchStatus.Rejected), cancellationToken);
    }

    public async Task AddAsync(PurchaseOrder order, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default)
    {
        await Orders.AddAsync(order, cancellationToken);
        if (sources.Count > 0) await Sources.AddRangeAsync(sources, cancellationToken);
    }

    public async Task ReplaceLinesAndSourcesAsync(PurchaseOrder order, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default)
    {
        var existingLines = await Lines.Where(x => x.PurchaseOrderId == order.Id).ToListAsync(cancellationToken);
        var desiredLineIds = order.Lines.Select(x => x.Id).ToHashSet();
        var oldLineIds = existingLines.Select(x => x.Id).ToArray();
        var existingSources = await Sources.Where(x => oldLineIds.Contains(x.PurchaseOrderLineId)).ToListAsync(cancellationToken);
        Sources.RemoveRange(existingSources.Where(x => sources.All(s => s.Id != x.Id)));
        Lines.RemoveRange(existingLines.Where(x => !desiredLineIds.Contains(x.Id)));
        foreach (var line in order.Lines)
        {
            var existing = existingLines.FirstOrDefault(x => x.Id == line.Id);
            if (existing is null)
            {
                if (dbContext.Entry(line).State == EntityState.Detached)
                    await Lines.AddAsync(line, cancellationToken);
                continue;
            }
            if (!ReferenceEquals(existing, line))
            {
                var originalRowVersion = existing.RowVersion;
                dbContext.Entry(existing).State = EntityState.Detached;
                Lines.Attach(line);
                var entry = dbContext.Entry(line);
                entry.State = EntityState.Modified;
                entry.Property(x => x.RowVersion).OriginalValue = originalRowVersion;
                entry.Property(x => x.RowVersion).CurrentValue = originalRowVersion;
            }
        }
        foreach (var source in sources)
        {
            var existing = existingSources.FirstOrDefault(x => x.Id == source.Id);
            if (existing is null)
            {
                await Sources.AddAsync(source, cancellationToken);
                continue;
            }
            dbContext.Entry(existing).Property(x => x.PurchaseOrderLineId).CurrentValue = source.PurchaseOrderLineId;
            dbContext.Entry(existing).Property(x => x.PurchaseRequestLineId).CurrentValue = source.PurchaseRequestLineId;
            dbContext.Entry(existing).Property(x => x.AllocatedQuantity).CurrentValue = source.AllocatedQuantity;
        }
    }

    public void Update(PurchaseOrder order) { if (dbContext.Entry(order).State == EntityState.Detached) { Orders.Attach(order); dbContext.Entry(order).State = EntityState.Modified; } }
}
