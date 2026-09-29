using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.Application.Sales.CustomerOrders.Queries;

public sealed record GetCustomerOrdersQuery(PageRequest Request) : IQuery<PagedResult<CustomerOrderDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.View];
}
