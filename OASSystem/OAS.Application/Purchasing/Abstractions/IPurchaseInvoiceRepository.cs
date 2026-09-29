using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseInvoiceRepository
{
    Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseInvoice?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedData<PurchaseInvoice>> GetPageAsync(PageRequest request, PurchaseInvoiceStatus? status, Guid? supplierId, CancellationToken cancellationToken = default);
    Task<bool> SupplierInvoiceCodeExistsAsync(Guid supplierId, string supplierInvoiceCode, Guid? exceptInvoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseInvoiceReceiptAllocation>> GetAllocationsAsync(Guid purchaseInvoiceId, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceReceiptAllocation?> GetAllocationForUpdateAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);
    Task ReplaceLinesAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);
    Task ReplaceAllocationsAsync(Guid purchaseInvoiceId, IReadOnlyList<PurchaseInvoiceReceiptAllocation> allocations, CancellationToken cancellationToken = default);
    void Update(PurchaseInvoice invoice);
    void UpdateAllocation(PurchaseInvoiceReceiptAllocation allocation);
}
