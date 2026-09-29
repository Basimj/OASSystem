using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.Application.Sales.CustomerOrders.Queries;

public sealed record GetCustomerOrderByIdQuery(Guid Id) : IQuery<CustomerOrderDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.View];
}
