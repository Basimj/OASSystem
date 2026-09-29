using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed record ConfirmCustomerOrderCommand(Guid OrderId, ConfirmCustomerOrderRequest Request)
    : ICommand<CustomerOrderDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.Confirm];
}
