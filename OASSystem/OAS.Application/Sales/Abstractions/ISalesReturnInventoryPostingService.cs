using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesReturnInventoryPostingResult(IReadOnlyList<Guid> InventoryTransactionIds);

public interface ISalesReturnInventoryPostingService
{
    Task<SalesReturnInventoryPostingResult> PostAsync(
        SalesReturn salesReturn,
        string postedBy,
        DateTimeOffset postedAtUtc,
        CancellationToken cancellationToken = default);
}
