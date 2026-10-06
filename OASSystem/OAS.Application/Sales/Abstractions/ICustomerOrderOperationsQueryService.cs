using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OrderOperations;

namespace OAS.Application.Sales.Abstractions;

public interface ICustomerOrderOperationsQueryService
{
    Task<PagedResult<CustomerOrderOperationsItemDto>> GetPageAsync(
        CustomerOrderOperationsQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerOrderOperationsSummaryDto> GetSummaryAsync(
        CustomerOrderOperationsQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerOrderOperationsDetailsDto> GetDetailsAsync(
        Guid customerOrderId,
        CancellationToken cancellationToken = default);
}
