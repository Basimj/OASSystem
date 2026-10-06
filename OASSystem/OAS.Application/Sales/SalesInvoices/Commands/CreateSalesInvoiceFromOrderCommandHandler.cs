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

public sealed class CreateSalesInvoiceFromOrderCommandHandler(
    ICustomerOrderAggregateRepository orders,
    ISalesInvoiceFromOrderService invoiceFromOrder,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler)
    : IRequestHandler<CreateSalesInvoiceFromOrderCommand, SalesInvoiceDto>
{
    public async Task<SalesInvoiceDto> Handle(CreateSalesInvoiceFromOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetAggregateAsync(request.OrderId, true, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.OrderId);
        SalesConcurrency.Ensure(request.Request.OrderRowVersion, order.RowVersion, "طلب العميل");
        if (order.Status is not (CustomerOrderStatus.Confirmed or CustomerOrderStatus.ReadyForProduction))
            throw new ConflictException("sales_order_invalid_status", "يمكن إنشاء الفاتورة فقط من طلب مؤكد أو جاهز للإنتاج.");

        var invoice = await invoiceFromOrder.CreateAsync(
            order,
            request.Request.InvoiceDate,
            request.Request.PostingDate,
            request.Request.InvoiceCode,
            request.Request.Description,
            ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }
}
