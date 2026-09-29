using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesInventoryLineCostResult(Guid SalesInvoiceLineId, decimal UnitCost, decimal TotalCost, Guid InventoryTransactionId);
public sealed record SalesInventoryPostingResult(IReadOnlyList<Guid> InventoryTransactionIds, IReadOnlyList<SalesInventoryLineCostResult> LineCosts);

public interface ISalesInventoryPostingService
{
    Task<SalesInventoryPostingResult> PostAsync(SalesInvoice invoice, string postedBy, DateTimeOffset postedAtUtc, CancellationToken cancellationToken = default);
}
