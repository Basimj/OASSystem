using OAS.Application.Abstractions.Messaging;
using OAS.Application.CRUD.Commands;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed record CreateCustomerOrderCommand(CreateCustomerOrderRequest Data)
    : CreateEntityCommand<CustomerOrder, Guid, CreateCustomerOrderRequest>(Data), IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.Create];
}
