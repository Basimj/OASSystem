using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.Application.Purchasing.Abstractions;

public interface ICustomerDemandTrackingQueryService
{
    Task<PagedResult<CustomerDemandTrackingItemDto>> GetPageAsync(
        CustomerDemandTrackingQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<CustomerDemandTrackingSummaryDto> GetSummaryAsync(
        CustomerDemandTrackingQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerDemandTrackingDetailsDto> GetDetailsAsync(
        Guid purchaseRequestLineId,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SupplierPurchaseHistoryItemDto>> GetSupplierHistoryAsync(
        SupplierPurchaseHistoryQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);
}
