using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.Client.Purchasing.CustomerDemand.Services;

public interface ICustomerDemandTrackingClientService
{
    Task<PagedResult<CustomerDemandTrackingItemDto>> GetPageAsync(CustomerDemandTrackingQueryRequest request, CancellationToken ct = default);
    Task<CustomerDemandTrackingSummaryDto?> GetSummaryAsync(CustomerDemandTrackingQueryRequest request, CancellationToken ct = default);
    Task<CustomerDemandTrackingDetailsDto?> GetDetailsAsync(Guid lineId, CancellationToken ct = default);
    Task<CustomerDemandTrackingDetailsDto?> AssignSupplierAsync(Guid lineId, AssignCustomerDemandSupplierRequest request, CancellationToken ct = default);
    Task<CustomerDemandTrackingDetailsDto?> ScheduleAsync(Guid lineId, ScheduleCustomerDemandRequest request, CancellationToken ct = default);
    Task<Guid?> CreatePurchaseOrderAsync(Guid lineId, CreateCustomerDemandPurchaseOrderRequest request, CancellationToken ct = default);
    Task<Guid?> ResourceRemainingAsync(Guid lineId, ResourceCustomerDemandRemainingRequest request, CancellationToken ct = default);
    Task<PagedResult<SupplierPurchaseHistoryItemDto>> GetSupplierHistoryAsync(SupplierPurchaseHistoryQueryRequest request, CancellationToken ct = default);
}
