using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.Application.Purchasing.Abstractions;

public interface ICustomerDemandSourcingService
{
    Task AssignSupplierAsync(Guid purchaseRequestLineId, AssignCustomerDemandSupplierRequest request, CancellationToken cancellationToken = default);
    Task ScheduleAsync(Guid purchaseRequestLineId, ScheduleCustomerDemandRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CreatePurchaseOrderAsync(Guid purchaseRequestLineId, CreateCustomerDemandPurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<Guid> ResourceRemainingAsync(Guid purchaseRequestLineId, ResourceCustomerDemandRemainingRequest request, CancellationToken cancellationToken = default);
}
