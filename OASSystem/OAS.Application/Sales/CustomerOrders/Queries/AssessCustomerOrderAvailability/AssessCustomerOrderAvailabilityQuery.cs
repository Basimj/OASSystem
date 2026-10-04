using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.Application.Sales.CustomerOrders.Queries.AssessCustomerOrderAvailability;

public sealed record AssessCustomerOrderAvailabilityQuery(Guid CustomerOrderId)
    : IQuery<CustomerOrderAvailabilityDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.ViewStockAvailability];
}
