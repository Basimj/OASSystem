using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseRequestRepository
{
    Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseRequest?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedData<PurchaseRequest>> GetPageAsync(PageRequest request, PurchaseRequestStatus? status, Guid? warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesAsync(IReadOnlyCollection<Guid> purchaseRequestLineIds, CancellationToken cancellationToken = default);
    Task<PurchaseRequest?> GetByLineIdAsync(Guid purchaseRequestLineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetAllocatedQuantitiesExcludingPurchaseOrderAsync(IReadOnlyCollection<Guid> purchaseRequestLineIds, Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseRequest request, CancellationToken cancellationToken = default);
    Task ReplaceLinesAsync(PurchaseRequest request, CancellationToken cancellationToken = default);
    void Update(PurchaseRequest request);
}
