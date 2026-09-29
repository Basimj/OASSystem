using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.PriceOverrides;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.PriceOverrides.Commands;

public sealed class RequestSalesPriceOverrideCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    IRepository<SalesPriceOverride, Guid> overrides,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RequestSalesPriceOverrideCommand, SalesPriceOverrideDto>
{
    public async Task<SalesPriceOverrideDto> Handle(RequestSalesPriceOverrideCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        SalesConcurrency.Ensure(request.Request.InvoiceRowVersion, invoice.RowVersion, "فاتورة المبيعات");

        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new ConflictException(SalesErrorCodes.InvoiceInvalidStatus, "يمكن طلب تغيير السعر في المسودة فقط.");

        var line = invoice.Lines.SingleOrDefault(x => x.Id == request.Request.SalesInvoiceLineId)
            ?? throw new NotFoundException(nameof(SalesInvoiceLine), request.Request.SalesInvoiceLineId);

        var pending = await overrides.CountAsync(
            new Specification<SalesPriceOverride>().Where(x =>
                x.SalesInvoiceLineId == line.Id &&
                x.Status == SalesPriceOverrideStatus.Pending &&
                x.IsActive),
            ct);
        if (pending > 0)
            throw new ConflictException("sales_price_override_pending", "يوجد طلب تغيير سعر معلق لهذا السطر.");

        var user = currentUser.UserId ?? throw new ForbiddenException();
        var originalPrice = line.ActualUnitPrice == request.Request.OverridePrice
            ? line.BaseUnitPrice
            : line.ActualUnitPrice;

        var entity = SalesPriceOverride.Request(
            Guid.NewGuid(),
            invoice.Id,
            line.Id,
            originalPrice,
            request.Request.OverridePrice,
            request.Request.Reason,
            user,
            timeProvider.GetUtcNow());

        await overrides.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return SalesContractMapping.PriceOverride(entity);
    }
}
