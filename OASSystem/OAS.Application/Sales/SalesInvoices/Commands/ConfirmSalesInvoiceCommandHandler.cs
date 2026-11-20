using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class ConfirmSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ICustomerOrderAggregateRepository orders,
    ICustomerOrderConfirmationService orderConfirmation,
    ISalesInvoiceConfirmationService confirmation,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler)
    : IRequestHandler<ConfirmSalesInvoiceCommand, SalesInvoiceDto>
{
    public async Task<SalesInvoiceDto> Handle(ConfirmSalesInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        SalesConcurrency.Ensure(request.Request.RowVersion, invoice.RowVersion, "فاتورة المبيعات");

        if (invoice.CustomerOrderId is Guid orderId)
        {
            var order = await orders.GetAggregateAsync(orderId, true, ct)
                ?? throw new NotFoundException(nameof(CustomerOrder), orderId);
            if (order.Status == CustomerOrderStatus.Draft)
            {
                await orderConfirmation.ConfirmAsync(order, cancellationToken: ct);
                orders.Update(order);
                await unitOfWork.SaveChangesAsync(ct);
            }
            // No phantom confirmation when materials are missing. Shortage-demand and
            // reservations were persisted, but invoice remains Draft and has no entry/payment.
            if (order.Status is CustomerOrderStatus.AwaitingStock or CustomerOrderStatus.PartiallyAvailable)
                return await assembler.InvoiceAsync(invoice, ct);
            if (order.Status is not (CustomerOrderStatus.Confirmed or CustomerOrderStatus.ReadyForProduction or CustomerOrderStatus.ReadyForDelivery))
                throw new ConflictException("sales_order_not_ready_for_invoice", "طلب العميل غير جاهز لتأكيد الفاتورة.");
        }
        await confirmation.ConfirmAsync(invoice, ct);
        invoices.Update(invoice);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }
}
