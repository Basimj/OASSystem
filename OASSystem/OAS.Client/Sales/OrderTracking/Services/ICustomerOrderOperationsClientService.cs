using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OrderOperations;

namespace OAS.Client.Sales.OrderTracking.Services;

public interface ICustomerOrderOperationsClientService
{
    Task<PagedResult<CustomerOrderOperationsItemDto>> GetPageAsync(CustomerOrderOperationsQueryRequest request, CancellationToken ct = default);
    Task<CustomerOrderOperationsSummaryDto?> GetSummaryAsync(CustomerOrderOperationsQueryRequest request, CancellationToken ct = default);
    Task<CustomerOrderOperationsDetailsDto?> GetDetailsAsync(Guid orderId, CancellationToken ct = default);
}
