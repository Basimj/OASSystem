using OAS.Application.Abstractions.Messaging;
using OAS.Application.CRUD.Commands;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed record UpdateCustomerOrderCommand(Guid Id, UpdateCustomerOrderRequest Data)
    : UpdateEntityCommand<CustomerOrder, Guid, UpdateCustomerOrderRequest>(Id, Data), IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.Edit];
}
