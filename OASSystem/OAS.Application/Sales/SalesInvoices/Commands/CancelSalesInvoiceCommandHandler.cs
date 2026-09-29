using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class CancelSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ISalesStockReservationService stock,
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
            await stock.ReleaseOrderReservationsAsync(invoice.CustomerOrderId.Value, now, ct);

        invoice.Cancel(now, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }
}
