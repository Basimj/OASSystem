using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseReturns;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseReturnRepository
{
    Task<PurchaseReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseReturn?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedData<PurchaseReturn>> GetPageAsync(
        PageRequest request,
        PurchaseReturnStatus? status,
        Guid? purchaseReceiptId,
        Guid? purchaseInvoiceId,
        Guid? supplierId,
        CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseReturn entity, CancellationToken cancellationToken = default);
    void Update(PurchaseReturn entity);
}
