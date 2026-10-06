using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Checkout;

namespace OAS.Application.Sales.Checkout.Commands;

public sealed record DeliverCustomerOrderCommand(Guid CustomerOrderId, DeliverCustomerOrderRequest Request)
    : ICommand<CheckoutCustomerOrderResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Checkout.Deliver];
}
