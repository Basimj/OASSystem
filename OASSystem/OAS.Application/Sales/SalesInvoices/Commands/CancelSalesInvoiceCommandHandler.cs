using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class CancelSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ISalesStockReservationService stock,
    ICustomerOrderAggregateRepository orders,
    IReadRepository<CustomerAdvance, Guid> advances,
    IReadRepository<OpticalJob, Guid> jobs,
    ICustomerDemandProcurementPort demand,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler)
    : IRequestHandler<CancelSalesInvoiceCommand, SalesInvoiceDto>
{
    public async Task<SalesInvoiceDto> Handle(CancelSalesInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        SalesConcurrency.Ensure(request.Request.RowVersion, invoice.RowVersion, "فاتورة المبيعات");

        var now = timeProvider.GetUtcNow();
        await stock.ReleaseInvoiceReservationsAsync(invoice.Id, now, ct);
        if (invoice.CustomerOrderId.HasValue)
        {
            var order = await orders.GetAggregateAsync(invoice.CustomerOrderId.Value, true, ct)
                ?? throw new NotFoundException(nameof(CustomerOrder), invoice.CustomerOrderId.Value);
            if (await advances.CountAsync(new Specification<CustomerAdvance>().Where(x =>
                    x.CustomerOrderId == order.Id && x.IsActive), ct) > 0 ||
                await jobs.CountAsync(new Specification<OpticalJob>().Where(x =>
                    x.CustomerOrderId == order.Id && x.IsActive), ct) > 0)
                throw new ConflictException("sales_invoice_cancel_order_committed",
                    "تعذر إلغاء الفاتورة والطلب لوجود عربون أو مهمة معمل نشطة. راجع المستندات أولًا.");
            await demand.CancelUncommittedDemandForOrderAsync(order.Id, "إلغاء مسودة الفاتورة المرتبطة", ct);
            await stock.ReleaseOrderReservationsAsync(order.Id, now, ct);
            order.Cancel(now, currentUser.UserId);
            orders.Update(order);
        }

        invoice.Cancel(now, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }
}
