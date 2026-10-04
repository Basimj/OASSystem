using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class CustomerOrderFulfillmentService(
    ICustomerOrderAggregateRepository orders,
    ISalesStockReservationService stock) : ICustomerOrderFulfillmentService
{
    public async Task ReconcileOrdersAsync(
        IReadOnlyCollection<Guid> customerOrderIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var orderId in customerOrderIds.Where(x => x != Guid.Empty).Distinct())
        {
            var order = await orders.GetAggregateAsync(orderId, true, cancellationToken)
                ?? throw new NotFoundException("CustomerOrder", orderId);

            if (order.Status is CustomerOrderStatus.Draft or CustomerOrderStatus.Cancelled or CustomerOrderStatus.Completed or CustomerOrderStatus.InProduction or CustomerOrderStatus.ReadyForDelivery)
                continue;

            var status = await stock.ReserveForOrderAsync(order, cancellationToken);
            order.SetAvailabilityStatus(status);
            orders.Update(order);
        }
    }
}
