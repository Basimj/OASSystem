using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Queries;

public sealed record GetCustomerOrdersDashboardSummaryQuery : IQuery<CustomerOrdersDashboardSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.View];
}

public sealed class GetCustomerOrdersDashboardSummaryQueryHandler(ICustomerOrderAggregateRepository orders)
    : IRequestHandler<GetCustomerOrdersDashboardSummaryQuery, CustomerOrdersDashboardSummaryDto>
{
    public async Task<CustomerOrdersDashboardSummaryDto> Handle(GetCustomerOrdersDashboardSummaryQuery request, CancellationToken ct)
    {
        var open = await orders.CountAsync(new Specification<CustomerOrder>()
            .Where(x => x.Status != CustomerOrderStatus.Cancelled && x.Status != CustomerOrderStatus.Completed), ct);
        var stock = await orders.CountAsync(new Specification<CustomerOrder>()
            .Where(x => x.Status == CustomerOrderStatus.AwaitingStock || x.Status == CustomerOrderStatus.PartiallyAvailable), ct);
        var production = await orders.CountAsync(new Specification<CustomerOrder>()
            .Where(x => x.Status == CustomerOrderStatus.InProduction || x.Status == CustomerOrderStatus.ReadyForProduction), ct);
        var ready = await orders.CountAsync(new Specification<CustomerOrder>()
            .Where(x => x.Status == CustomerOrderStatus.ReadyForDelivery), ct);
        return new(open, stock, production, ready);
    }
}
