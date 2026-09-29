using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetByLineIdAsync(Guid purchaseOrderLineId, CancellationToken cancellationToken = default);
    Task<PagedData<PurchaseOrder>> GetPageAsync(PageRequest request, PurchaseOrderStatus? status, Guid? supplierId, Guid? warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrderLineSource>> GetSourcesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetPostedReceivedBaseQuantitiesAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<bool> HasPostedReceiptAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<bool> CanCloseAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseOrder order, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default);
    Task ReplaceLinesAndSourcesAsync(PurchaseOrder order, IReadOnlyList<PurchaseOrderLineSource> sources, CancellationToken cancellationToken = default);
    void Update(PurchaseOrder order);
}
