using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseMatchingService
{
    Task<PurchaseMatchEvaluation> EvaluateAsync(PurchaseInvoice invoice, IReadOnlyList<PurchaseInvoiceMatchAllocationRequest> allocations, CancellationToken cancellationToken = default);
}
