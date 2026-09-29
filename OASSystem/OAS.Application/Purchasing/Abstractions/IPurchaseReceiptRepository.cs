using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseReceiptRepository
{
    Task<PurchaseReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseReceipt?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedData<PurchaseReceipt>> GetPageAsync(PageRequest request, PurchaseReceiptStatus? status, Guid? purchaseOrderId, Guid? supplierId, CancellationToken cancellationToken = default);
    Task<decimal> GetPostedAcceptedQuantityAsync(Guid purchaseOrderLineId, CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseReceipt receipt, CancellationToken cancellationToken = default);
    Task ReplaceLinesAsync(PurchaseReceipt receipt, CancellationToken cancellationToken = default);
    void Update(PurchaseReceipt receipt);
}
