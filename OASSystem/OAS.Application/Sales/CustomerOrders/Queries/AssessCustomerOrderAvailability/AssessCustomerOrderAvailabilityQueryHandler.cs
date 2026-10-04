using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Queries.AssessCustomerOrderAvailability;

public sealed class AssessCustomerOrderAvailabilityQueryHandler(
    ICustomerOrderAggregateRepository orders,
    ICustomerOrderAvailabilityService availability)
    : IRequestHandler<AssessCustomerOrderAvailabilityQuery, CustomerOrderAvailabilityDto>
{
    public async Task<CustomerOrderAvailabilityDto> Handle(AssessCustomerOrderAvailabilityQuery request, CancellationToken ct)
    {
        var order = await orders.GetAggregateAsync(request.CustomerOrderId, false, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.CustomerOrderId);
        return await availability.AssessAsync(order, ct);
    }
}
