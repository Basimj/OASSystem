using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Checkout;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Checkout.Commands;

public sealed class DeliverCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository orders,
    ISalesCheckoutOrchestrator orchestrator)
    : IRequestHandler<DeliverCustomerOrderCommand, CheckoutCustomerOrderResultDto>
{
    public async Task<CheckoutCustomerOrderResultDto> Handle(
        DeliverCustomerOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetAggregateAsync(request.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException("CustomerOrder", request.CustomerOrderId);

        if (order.Status != CustomerOrderStatus.Completed)
            SalesConcurrency.Ensure(request.Request.RowVersion, order.RowVersion, "طلب العميل");

        return await orchestrator.DeliverAsync(order, request.Request, cancellationToken);
    }
}
