using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Returns.Commands;

public sealed class CancelSalesReturnCommandHandler(
    ISalesReturnAggregateRepository returns,
    ISalesInvoiceAggregateRepository invoices,
    ISalesReturnQueryService query,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<CancelSalesReturnCommand, SalesReturnDto>
{
    public async Task<SalesReturnDto> Handle(CancelSalesReturnCommand request, CancellationToken ct)
    {
        var entity = await returns.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(SalesReturn), request.Id);
        SalesConcurrency.Ensure(request.Request.RowVersion, entity.RowVersion, "مرتجع المبيعات");
        var invoice = await invoices.GetAggregateAsync(entity.SalesInvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), entity.SalesInvoiceId);

        foreach (var line in entity.Lines.Where(x => x.IsActive))
        {
            var source = invoice.Lines.SingleOrDefault(x => x.Id == line.SalesInvoiceLineId)
                ?? throw new ConflictException("sales_return_source_line_missing", "تعذر العثور على سطر الفاتورة الأصلي أثناء إلغاء المرتجع.");
            try { source.ReleaseReturnQuantity(line.Quantity); }
            catch (DomainException ex) { throw new ConflictException("sales_return_release_invalid", ex.Message); }
        }

        entity.Cancel(timeProvider.GetUtcNow(), currentUser.UserId, request.Request.Reason);
        returns.Update(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return await query.GetByIdAsync(entity.Id, ct)
            ?? throw new NotFoundException(nameof(SalesReturn), entity.Id);
    }
}
