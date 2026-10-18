using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

public interface ISupplierOpeningRepository
{
    Task<SupplierOpeningBatch?> GetBatchAsync(Guid id, bool forUpdate, CancellationToken cancellationToken = default);
    Task<SupplierOpeningItem?> GetItemAsync(Guid id, bool forUpdate, CancellationToken cancellationToken = default);
    Task<SupplierOpeningApplication?> GetApplicationAsync(Guid id, bool forUpdate, CancellationToken cancellationToken = default);
    Task<SupplierOpeningApplication?> GetApplicationByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierOpeningApplication>> GetApplicationsForItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierOpeningApplication>> GetApplicationsForPaymentLineAsync(Guid paymentVoucherLineId, CancellationToken cancellationToken = default);
    Task<bool> BatchCodeExistsAsync(string batchCode, CancellationToken cancellationToken = default);
    Task AddBatchAsync(SupplierOpeningBatch batch, CancellationToken cancellationToken = default);
    Task AddApplicationAsync(SupplierOpeningApplication application, CancellationToken cancellationToken = default);
    void UpdateBatch(SupplierOpeningBatch batch);
    void UpdateApplication(SupplierOpeningApplication application);
}
