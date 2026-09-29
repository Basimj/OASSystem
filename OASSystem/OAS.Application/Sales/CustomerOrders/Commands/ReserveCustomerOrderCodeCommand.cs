using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Common;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed record ReserveCustomerOrderCodeCommand(DateOnly OrderDate) : ICommand<SalesCodeReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.Create];
}
