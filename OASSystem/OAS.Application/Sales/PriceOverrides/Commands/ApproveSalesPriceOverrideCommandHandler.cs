using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.PriceOverrides;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.PriceOverrides.Commands;

public sealed class ApproveSalesPriceOverrideCommandHandler(
    IRepository<SalesPriceOverride, Guid> repository,
    ISalesInvoiceAggregateRepository invoices,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveSalesPriceOverrideCommand, SalesPriceOverrideDto>
{
    public async Task<SalesPriceOverrideDto> Handle(ApproveSalesPriceOverrideCommand request, CancellationToken ct)
    {
        var priceOverride = await repository.GetForUpdateAsync(request.OverrideId, ct)
            ?? throw new NotFoundException(nameof(SalesPriceOverride), request.OverrideId);
        SalesConcurrency.Ensure(request.Request.RowVersion, priceOverride.RowVersion, "طلب تغيير السعر");

        var invoice = await invoices.GetAggregateAsync(priceOverride.SalesInvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), priceOverride.SalesInvoiceId);
        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new ConflictException(SalesErrorCodes.InvoiceInvalidStatus, "لا يمكن اعتماد تغيير سعر بعد خروج الفاتورة من حالة المسودة.");

        var line = invoice.Lines.SingleOrDefault(x => x.Id == priceOverride.SalesInvoiceLineId)
            ?? throw new NotFoundException(nameof(SalesInvoiceLine), priceOverride.SalesInvoiceLineId);

        // أي اعتماد سابق للسعر على نفس السطر يصبح غير فعال عند اعتماد سعر جديد.
        var previousApprovals = await repository.ListAsync(
            new Specification<SalesPriceOverride>().Where(x =>
                x.SalesInvoiceLineId == line.Id &&
                x.Id != priceOverride.Id &&
                x.IsActive &&
                x.Status == SalesPriceOverrideStatus.Approved)
            .Tracking(),
            ct);

        foreach (var previous in previousApprovals)
        {
            previous.Cancel();
            repository.Update(previous);
        }

        // السعر لا يُكتب مباشرة في شاشة الفاتورة. يُطبق فقط هنا بعد الموافقة.
        invoice.UpdateLinePricing(
            line.Id,
            line.Quantity,
            line.BaseUnitPrice,
            priceOverride.OverridePrice,
            line.DiscountType,
            line.DiscountValue,
            line.TaxRate);

        priceOverride.Approve(currentUser.UserId ?? throw new ForbiddenException(), timeProvider.GetUtcNow());
        repository.Update(priceOverride);
        invoices.Update(invoice);
        await unitOfWork.SaveChangesAsync(ct);
        return SalesContractMapping.PriceOverride(priceOverride);
    }
}
