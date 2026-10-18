using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

/// <summary>All writes are SQL-backed. Enqueue is called INSIDE the receipt posting transaction.</summary>
public interface IPurchaseReceiptFollowUpJobs
{
    Task EnqueueAsync(Guid receiptId, IReadOnlyCollection<Guid> orderIds,
        IReadOnlyCollection<Guid> requestLineIds, CancellationToken ct);
    Task<PurchaseReceiptFollowUpPageDto> ListAsync(Guid? receiptId,
        ReceiptFollowUpStatus? status, int pageNumber, int pageSize, CancellationToken ct);
    Task<bool> RetryAsync(Guid jobId, CancellationToken ct);
}
