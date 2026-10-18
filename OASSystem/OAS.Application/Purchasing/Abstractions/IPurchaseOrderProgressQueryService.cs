using OAS.Contracts.Purchasing.PurchaseOrders;

namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseOrderProgressQueryService
{
    Task<PurchaseOrderProgressDto> GetAsync(Guid orderId, CancellationToken ct = default);
}
