using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Checkout;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Checkout.Commands;

public sealed class CheckoutCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository orders,
    ISalesCheckoutOrchestrator orchestrator)
    : IRequestHandler<CheckoutCustomerOrderCommand, CheckoutCustomerOrderResultDto>
{
    public async Task<CheckoutCustomerOrderResultDto> Handle(
        CheckoutCustomerOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetAggregateAsync(request.Request.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException("CustomerOrder", request.Request.CustomerOrderId);

        // A repeated checkout after the first transaction has committed is treated as an
        // idempotent read of the already-created workflow. RowVersion is checked only while Draft.
        if (order.Status == CustomerOrderStatus.Draft)
            SalesConcurrency.Ensure(request.Request.RowVersion, order.RowVersion, "طلب العميل");

        return await orchestrator.CheckoutAsync(order, request.Request, cancellationToken);
    }
}
