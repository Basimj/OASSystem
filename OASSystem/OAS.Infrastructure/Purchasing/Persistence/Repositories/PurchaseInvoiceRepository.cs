using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;
using DomainStatus = OAS.Domain.Purchasing.Enums.PurchaseInvoiceStatus;

namespace OAS.Infrastructure.Purchasing.Persistence.Repositories;

public sealed class PurchaseInvoiceRepository(OasDbContext dbContext) : IPurchaseInvoiceRepository
{
    private DbSet<PurchaseInvoice> Invoices => dbContext.Set<PurchaseInvoice>();
    private DbSet<PurchaseInvoiceLine> Lines => dbContext.Set<PurchaseInvoiceLine>();
    private DbSet<PurchaseInvoiceReceiptAllocation> Allocations => dbContext.Set<PurchaseInvoiceReceiptAllocation>();

    public Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Invoices.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<PurchaseInvoice?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Invoices.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PagedData<PurchaseInvoice>> GetPageAsync(PageRequest request, PurchaseInvoiceStatus? status, Guid? supplierId, CancellationToken cancellationToken = default)
    {
        var query = Invoices.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == (DomainStatus)(byte)status.Value);
        if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId.Value);
        query = query.OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.PurchaseInvoiceCode);
        return PurchasingRepositoryHelpers.PageAsync(query, request, (q, term) => q.Where(x => x.PurchaseInvoiceCode.Contains(term) || (x.SupplierInvoiceCode != null && x.SupplierInvoiceCode.Contains(term))), cancellationToken);
    }

    public Task<bool> SupplierInvoiceCodeExistsAsync(Guid supplierId, string supplierInvoiceCode, Guid? exceptInvoiceId, CancellationToken cancellationToken = default) =>
        Invoices.AsNoTracking().AnyAsync(x => x.SupplierId == supplierId && x.SupplierInvoiceCode == supplierInvoiceCode && (!exceptInvoiceId.HasValue || x.Id != exceptInvoiceId.Value), cancellationToken);

    public async Task<IReadOnlyList<PurchaseInvoiceReceiptAllocation>> GetAllocationsAsync(Guid purchaseInvoiceId, CancellationToken cancellationToken = default)
    {
        var lineIds = Lines.AsNoTracking().Where(x => x.PurchaseInvoiceId == purchaseInvoiceId).Select(x => x.Id);
        return await Allocations.AsNoTracking().Where(x => lineIds.Contains(x.PurchaseInvoiceLineId)).ToListAsync(cancellationToken);
    }

    public Task<PurchaseInvoiceReceiptAllocation?> GetAllocationForUpdateAsync(Guid allocationId, CancellationToken cancellationToken = default) => Allocations.FirstOrDefaultAsync(x => x.Id == allocationId, cancellationToken);
    public Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default) => Invoices.AddAsync(invoice, cancellationToken).AsTask();

    public async Task ReplaceLinesAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        var desired = invoice.Lines.Select(x => x.Id).ToHashSet();
        var existing = await Lines.Where(x => x.PurchaseInvoiceId == invoice.Id).ToListAsync(cancellationToken);
        var removedIds = existing.Where(x => !desired.Contains(x.Id)).Select(x => x.Id).ToArray();
        if (removedIds.Length > 0)
        {
            var allocations = await Allocations.Where(x => removedIds.Contains(x.PurchaseInvoiceLineId)).ToListAsync(cancellationToken);
            Allocations.RemoveRange(allocations);
            Lines.RemoveRange(existing.Where(x => removedIds.Contains(x.Id)));
        }
        foreach (var line in invoice.Lines)
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

    public async Task ReplaceAllocationsAsync(Guid purchaseInvoiceId, IReadOnlyList<PurchaseInvoiceReceiptAllocation> allocations, CancellationToken cancellationToken = default)
    {
        var lineIds = await Lines.AsNoTracking().Where(x => x.PurchaseInvoiceId == purchaseInvoiceId).Select(x => x.Id).ToListAsync(cancellationToken);
        var existing = await Allocations.Where(x => lineIds.Contains(x.PurchaseInvoiceLineId)).ToListAsync(cancellationToken);
        Allocations.RemoveRange(existing);
        if (allocations.Count > 0) await Allocations.AddRangeAsync(allocations, cancellationToken);
    }

    public void Update(PurchaseInvoice invoice) { if (dbContext.Entry(invoice).State == EntityState.Detached) { Invoices.Attach(invoice); dbContext.Entry(invoice).State = EntityState.Modified; } }
    public void UpdateAllocation(PurchaseInvoiceReceiptAllocation allocation) { if (dbContext.Entry(allocation).State == EntityState.Detached) { Allocations.Attach(allocation); dbContext.Entry(allocation).State = EntityState.Modified; } }
}
