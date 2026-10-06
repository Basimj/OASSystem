using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class ConfirmSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
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

        await confirmation.ConfirmAsync(invoice, ct);
        invoices.Update(invoice);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }
}
