using OAS.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseReturns;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;
using DomainStatus = OAS.Domain.Purchasing.Enums.PurchaseReturnStatus;

namespace OAS.Infrastructure.Purchasing.Persistence.Repositories;

public sealed class PurchaseReturnRepository(OasDbContext dbContext) : IPurchaseReturnRepository
{
    private DbSet<PurchaseReturn> Returns => dbContext.Set<PurchaseReturn>();

    public Task<PurchaseReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Returns.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PurchaseReturn?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Returns.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PagedData<PurchaseReturn>> GetPageAsync(
        PageRequest request,
        PurchaseReturnStatus? status,
        Guid? purchaseReceiptId,
        Guid? purchaseInvoiceId,
        Guid? supplierId,
        CancellationToken cancellationToken = default)
    {
        var query = Returns.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == (DomainStatus)(byte)status.Value);
        if (purchaseReceiptId.HasValue) query = query.Where(x => x.PurchaseReceiptId == purchaseReceiptId.Value);
        if (purchaseInvoiceId.HasValue) query = query.Where(x => x.PurchaseInvoiceId == purchaseInvoiceId.Value);
        if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId.Value);
        query = query.OrderByDescending(x => x.ReturnDate).ThenByDescending(x => x.ReturnCode);
        return PurchasingRepositoryHelpers.PageAsync(query, request,
            (q, term) => q.Where(x => x.ReturnCode.Contains(term) || (x.Reason != null && x.Reason.Contains(term))),
            cancellationToken);
    }

    public Task AddAsync(PurchaseReturn entity, CancellationToken cancellationToken = default) =>
        Returns.AddAsync(entity, cancellationToken).AsTask();

    public void Update(PurchaseReturn entity)
    {
        if (dbContext.Entry(entity).State == EntityState.Detached)
        {
            Returns.Attach(entity);
            dbContext.Entry(entity).State = EntityState.Modified;
        }
    }
}
