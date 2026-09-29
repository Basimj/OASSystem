using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;
using DomainStatus = OAS.Domain.Purchasing.Enums.PurchaseReceiptStatus;

namespace OAS.Infrastructure.Purchasing.Persistence.Repositories;

public sealed class PurchaseReceiptRepository(OasDbContext dbContext) : IPurchaseReceiptRepository
{
    private DbSet<PurchaseReceipt> Receipts => dbContext.Set<PurchaseReceipt>();
    private DbSet<PurchaseReceiptLine> Lines => dbContext.Set<PurchaseReceiptLine>();

    public Task<PurchaseReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Receipts.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<PurchaseReceipt?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Receipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PagedData<PurchaseReceipt>> GetPageAsync(PageRequest request, PurchaseReceiptStatus? status, Guid? purchaseOrderId, Guid? supplierId, CancellationToken cancellationToken = default)
    {
        var query = Receipts.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == (DomainStatus)(byte)status.Value);
        if (purchaseOrderId.HasValue) query = query.Where(x => x.PurchaseOrderId == purchaseOrderId.Value);
        if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId.Value);
        query = query.OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.ReceiptCode);
        return PurchasingRepositoryHelpers.PageAsync(query, request, (q, term) => q.Where(x => x.ReceiptCode.Contains(term) || (x.SupplierDeliveryCode != null && x.SupplierDeliveryCode.Contains(term))), cancellationToken);
    }

    public async Task<decimal> GetPostedAcceptedQuantityAsync(Guid purchaseOrderLineId, CancellationToken cancellationToken = default) =>
        await (from line in Lines.AsNoTracking()
               join receipt in Receipts.AsNoTracking() on line.PurchaseReceiptId equals receipt.Id
               where line.PurchaseOrderLineId == purchaseOrderLineId && receipt.Status == DomainStatus.Posted
               select (decimal?)line.AcceptedQuantity).SumAsync(cancellationToken) ?? 0m;

    public Task AddAsync(PurchaseReceipt receipt, CancellationToken cancellationToken = default) => Receipts.AddAsync(receipt, cancellationToken).AsTask();

    public async Task ReplaceLinesAsync(PurchaseReceipt receipt, CancellationToken cancellationToken = default)
    {
        var desired = receipt.Lines.Select(x => x.Id).ToHashSet();
        var existing = await Lines.Where(x => x.PurchaseReceiptId == receipt.Id).ToListAsync(cancellationToken);
        Lines.RemoveRange(existing.Where(x => !desired.Contains(x.Id)));
        foreach (var line in receipt.Lines)
        {
            var current = existing.FirstOrDefault(x => x.Id == line.Id);
            if (current is null)
            {
                if (dbContext.Entry(line).State == EntityState.Detached)
                    await Lines.AddAsync(line, cancellationToken);
                continue;
            }
            if (!ReferenceEquals(current, line))
            {
                var originalRowVersion = current.RowVersion;
                dbContext.Entry(current).State = EntityState.Detached;
                Lines.Attach(line);
                var entry = dbContext.Entry(line);
                entry.State = EntityState.Modified;
                entry.Property(x => x.RowVersion).OriginalValue = originalRowVersion;
                entry.Property(x => x.RowVersion).CurrentValue = originalRowVersion;
            }
        }
    }

    public void Update(PurchaseReceipt receipt) { if (dbContext.Entry(receipt).State == EntityState.Detached) { Receipts.Attach(receipt); dbContext.Entry(receipt).State = EntityState.Modified; } }
}
