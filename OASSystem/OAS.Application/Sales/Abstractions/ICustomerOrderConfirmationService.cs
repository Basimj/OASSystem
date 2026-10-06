using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Abstractions;

public sealed record CustomerOrderConfirmationResult(
    CustomerOrderAvailabilityDto Availability,
    CustomerOrderStatus Status,
    IReadOnlyList<CustomerDemandLine> DemandLines);

public interface ICustomerOrderConfirmationService
{
    Task<CustomerOrderConfirmationResult> ConfirmAsync(
        CustomerOrder order,
        IReadOnlyList<CheckoutSupplierScheduleRequest>? supplierSchedulingDecisions = null,
        CancellationToken cancellationToken = default);
}
