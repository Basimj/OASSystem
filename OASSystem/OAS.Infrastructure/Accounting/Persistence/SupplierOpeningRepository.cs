using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence;

public sealed class SupplierOpeningRepository(OasDbContext db) : ISupplierOpeningRepository
{
    public Task<SupplierOpeningBatch?> GetBatchAsync(Guid id, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var query = db.Set<SupplierOpeningBatch>().Include(x => x.Items).AsQueryable();
        if (!forUpdate) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<SupplierOpeningItem?> GetItemAsync(Guid id, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var query = db.Set<SupplierOpeningItem>().AsQueryable();
        if (!forUpdate) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<SupplierOpeningApplication?> GetApplicationAsync(Guid id, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var query = db.Set<SupplierOpeningApplication>().AsQueryable();
        if (!forUpdate) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<SupplierOpeningApplication?> GetApplicationByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        db.Set<SupplierOpeningApplication>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<SupplierOpeningApplication>> GetApplicationsForItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        await db.Set<SupplierOpeningApplication>().AsNoTracking()
            .Where(x => x.SupplierOpeningItemId == itemId)
            .OrderBy(x => x.AppliedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SupplierOpeningApplication>> GetApplicationsForPaymentLineAsync(Guid paymentVoucherLineId, CancellationToken cancellationToken = default) =>
        await db.Set<SupplierOpeningApplication>().AsNoTracking()
            .Where(x => x.PaymentVoucherLineId == paymentVoucherLineId)
            .OrderBy(x => x.AppliedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> BatchCodeExistsAsync(string batchCode, CancellationToken cancellationToken = default) =>
        db.Set<SupplierOpeningBatch>().AsNoTracking().AnyAsync(x => x.BatchCode == batchCode, cancellationToken);

    public Task AddBatchAsync(SupplierOpeningBatch batch, CancellationToken cancellationToken = default) =>
        db.Set<SupplierOpeningBatch>().AddAsync(batch, cancellationToken).AsTask();

    public Task AddApplicationAsync(SupplierOpeningApplication application, CancellationToken cancellationToken = default) =>
        db.Set<SupplierOpeningApplication>().AddAsync(application, cancellationToken).AsTask();

    public void UpdateBatch(SupplierOpeningBatch batch) => db.Entry(batch).State = EntityState.Modified;
    public void UpdateApplication(SupplierOpeningApplication application) => db.Entry(application).State = EntityState.Modified;
}
