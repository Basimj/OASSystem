using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class CustomerOrderFulfillmentService(
    ICustomerOrderAggregateRepository orders,
    ISalesStockReservationService stock,
    ISalesInvoiceFromOrderService invoiceFromOrder,
    ISalesInvoiceConfirmationService invoiceConfirmation,
    ISalesInvoicePostingWorkflow invoicePosting,
    ISalesSettlementService settlement,
    IOpticalProductionPort opticalProduction,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICustomerOrderFulfillmentService
{
    // Compatibility constructor for existing isolated reservation tests and staged module delivery.
    // The production DI path always uses the full constructor and therefore completes the commercial workflow.
    public CustomerOrderFulfillmentService(
        ICustomerOrderAggregateRepository orders,
        ISalesStockReservationService stock)
        : this(orders, stock, null!, null!, null!, null!, null!, null!, TimeProvider.System)
    {
        _completeCommercialWorkflow = false;
    }

    private bool _completeCommercialWorkflow = true;

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

            // During a staged/legacy reservation-only invocation, keep the historical behavior.
            if (!_completeCommercialWorkflow)
                continue;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Still waiting for one or more missing materials.
            if (status is CustomerOrderStatus.AwaitingStock or CustomerOrderStatus.PartiallyAvailable)
                continue;

            // Once all materials are available, finalize the commercial document exactly once.
            // SalesInvoiceFromOrderService is the single idempotent entry point: it creates a new
            // tracked aggregate or loads the existing invoice aggregate with tracking. Avoid a
            // detached header query followed by Update, which can break graph state/order.
            var invoice = await invoiceFromOrder.CreateAsync(
                order,
                DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
                DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
                null,
                $"فاتورة طلب العميل {order.OrderCode}",
                cancellationToken);

            if (invoice.Status == SalesInvoiceStatus.Draft)
            {
                await invoiceConfirmation.ConfirmAsync(invoice, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (invoice.Status == SalesInvoiceStatus.Confirmed)
            {
                await invoicePosting.PostAsync(invoice, cancellationToken);
            }

            if (invoice.Status != SalesInvoiceStatus.Posted)
                throw new ConflictException("customer_order_fulfillment_invoice_not_posted", "تعذر ترحيل فاتورة طلب العميل بعد اكتمال المواد.");

            await settlement.ApplyAdvancesToInvoiceAsync(order, invoice, cancellationToken);

            if (order.RequiresProduction)
            {
                await opticalProduction.EnsureJobAsync(order, invoice.Id, cancellationToken);
            }
            else if (order.Status == CustomerOrderStatus.Confirmed)
            {
                order.MarkReadyForDelivery();
                orders.Update(order);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

}
