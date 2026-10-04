using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public sealed record PurchaseReturnInventoryPostingResult(IReadOnlyList<Guid> InventoryTransactionIds);

public interface IPurchaseReturnInventoryPostingService
{
    Task<PurchaseReturnInventoryPostingResult> PostAsync(
        PurchaseReturn purchaseReturn,
        string postedBy,
        DateTimeOffset postedAtUtc,
        CancellationToken cancellationToken = default);
}
