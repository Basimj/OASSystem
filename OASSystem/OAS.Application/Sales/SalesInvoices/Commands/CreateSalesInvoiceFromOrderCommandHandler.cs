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

/// <summary>
/// Creates a real invoice draft linked to an unconfirmed operational order.
/// Deliberately does not reserve stock, collect payment or post the invoice.
/// A retry on the same order returns the original draft.
/// </summary>
public sealed class CreateSalesInvoiceFromOrderCommandHandler(
    ICustomerOrderAggregateRepository orders,
    ISalesInvoiceFromOrderService fromOrder,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler)
    : IRequestHandler<CreateSalesInvoiceFromOrderCommand, SalesInvoiceDto>
{
    public async Task<SalesInvoiceDto> Handle(CreateSalesInvoiceFromOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetAggregateAsync(request.OrderId, false, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.OrderId);
        // Idempotent retry must work after a committed response was lost; the unique
        // CustomerOrderId link, not a human-facing invoice number, identifies it.
        if (order.Status == CustomerOrderStatus.Draft)
            SalesConcurrency.Ensure(request.Request.OrderRowVersion, order.RowVersion, "طلب العميل");

        var invoice = await fromOrder.CreateDraftAsync(
            order, request.Request.InvoiceDate, request.Request.PostingDate,
            request.Request.InvoiceCode, request.Request.Description, ct,
            request.Request.SalesEmployeeId);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }
}
