using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ICustomerOrderAvailabilityService
{
    Task<CustomerOrderAvailabilityDto> AssessAsync(
        CustomerOrder order,
        CancellationToken cancellationToken = default);
}
