using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;
using DomainStatus = OAS.Domain.Purchasing.Enums.PurchaseRequestStatus;

namespace OAS.Infrastructure.Purchasing.Persistence.Repositories;

public sealed class PurchaseRequestRepository(OasDbContext dbContext) : IPurchaseRequestRepository
{
    private DbSet<PurchaseRequest> Requests => dbContext.Set<PurchaseRequest>();
    private DbSet<PurchaseRequestLine> Lines => dbContext.Set<PurchaseRequestLine>();
    private DbSet<PurchaseOrderLineSource> Sources => dbContext.Set<PurchaseOrderLineSource>();
    private DbSet<PurchaseOrderLine> OrderLines => dbContext.Set<PurchaseOrderLine>();

    public Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Requests.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PurchaseRequest?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Requests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PagedData<PurchaseRequest>> GetPageAsync(PageRequest request, PurchaseRequestStatus? status, Guid? warehouseId, CancellationToken cancellationToken = default)
    {
        var query = Requests.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == (DomainStatus)(byte)status.Value);
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
        query = query.OrderByDescending(x => x.RequestDate).ThenByDescending(x => x.RequestCode);
        return PurchasingRepositoryHelpers.PageAsync(query, request, (q, term) => q.Where(x => x.RequestCode.Contains(term) || (x.Reason != null && x.Reason.Contains(term))), cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesAsync(IReadOnlyCollection<Guid> purchaseRequestLineIds, CancellationToken cancellationToken = default)
    {
        if (purchaseRequestLineIds.Count == 0) return new Dictionary<Guid, decimal>();
        return await Sources.AsNoTracking().Where(x => purchaseRequestLineIds.Contains(x.PurchaseRequestLineId))
            .GroupBy(x => x.PurchaseRequestLineId).Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.AllocatedQuantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Quantity, cancellationToken);
    }

    public Task<PurchaseRequest?> GetByLineIdAsync(Guid purchaseRequestLineId, CancellationToken cancellationToken = default) =>
        Requests.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Lines.Any(l => l.Id == purchaseRequestLineId), cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesExcludingPurchaseOrderAsync(IReadOnlyCollection<Guid> purchaseRequestLineIds, Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        if (purchaseRequestLineIds.Count == 0) return new Dictionary<Guid, decimal>();
        var query = from source in Sources.AsNoTracking()
                    join line in OrderLines.AsNoTracking() on source.PurchaseOrderLineId equals line.Id
                    where purchaseRequestLineIds.Contains(source.PurchaseRequestLineId) && line.PurchaseOrderId != purchaseOrderId
                    group source by source.PurchaseRequestLineId into g
                    select new { Id = g.Key, Quantity = g.Sum(x => x.AllocatedQuantity) };
        return await query.ToDictionaryAsync(x => x.Id, x => x.Quantity, cancellationToken);
    }

    public Task AddAsync(PurchaseRequest request, CancellationToken cancellationToken = default) => Requests.AddAsync(request, cancellationToken).AsTask();

    public async Task ReplaceLinesAsync(PurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var desired = request.Lines.Select(x => x.Id).ToHashSet();
        var existing = await Lines.Where(x => x.PurchaseRequestId == request.Id).ToListAsync(cancellationToken);
        Lines.RemoveRange(existing.Where(x => !desired.Contains(x.Id)));
        foreach (var line in request.Lines)
            if (dbContext.Entry(line).State == EntityState.Detached)
                await Lines.AddAsync(line, cancellationToken);
    }

    public void Update(PurchaseRequest request)
    {
        if (dbContext.Entry(request).State == EntityState.Detached) { Requests.Attach(request); dbContext.Entry(request).State = EntityState.Modified; }
    }
}
