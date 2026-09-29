using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class ConfirmSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<SalesPriceOverride, Guid> overrides,
    IRepository<SalesInvoiceLinePrescriptionSnapshot, Guid> snapshots,
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator,
    ISalesStockReservationService stock,
    ISalesCreditExposureService credit,
    IPermissionChecker permissions,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler)
    : IRequestHandler<ConfirmSalesInvoiceCommand, SalesInvoiceDto>
{
    public async Task<SalesInvoiceDto> Handle(ConfirmSalesInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        SalesConcurrency.Ensure(request.Request.RowVersion, invoice.RowVersion, "فاتورة المبيعات");

        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new ConflictException(SalesErrorCodes.InvoiceInvalidStatus, "يمكن تأكيد الفاتورة من حالة المسودة فقط.");

        var customer = await customers.GetByIdAsync(invoice.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        var hasDiscount = invoice.Lines.Any(x => x.IsActive && x.DiscountAmount > 0);
        if (hasDiscount && !await permissions.HasPermissionAsync(SalesPermissions.Discount, ct))
            throw new ForbiddenException("لا توجد صلاحية لتطبيق الخصم.", SalesPermissions.Discount);

        foreach (var line in invoice.Lines.Where(x => x.IsActive))
        {
            var resolved = await lineResolver.ResolveAsync(
                line.LineType, line.ProductVariantId, line.WarehouseId, line.DescriptionSnapshot, ct);
            var detail = await prescriptionValidator.ValidateLineAsync(
                line.PrescriptionRevisionId, line.PrescriptionEye, resolved.PrescriptionRequired, resolved.OpticalPolicy, ct);

            if (line.LineType == SalesLineType.Lens && detail is not null)
            {
                var snapshot = SalesInvoiceLinePrescriptionSnapshot.Create(
                    Guid.NewGuid(), line.Id, detail.PrescriptionRevisionId, detail.Eye,
                    detail.SPH, detail.CYL, detail.Axis, detail.ADD, detail.Prism, detail.PrismBase,
                    detail.PD, detail.MonocularPD, detail.VA, detail.FittingHeight);
                invoice.SetLinePrescriptionSnapshot(line.Id, snapshot);
                await snapshots.AddAsync(snapshot, ct);
            }

            if (line.ActualUnitPrice != line.BaseUnitPrice)
            {
                var spec = new Specification<SalesPriceOverride>()
                    .Where(x => x.SalesInvoiceLineId == line.Id &&
                                x.Status == SalesPriceOverrideStatus.Approved && x.IsActive);
                var approved = (await overrides.ListAsync(spec, ct))
                    .Any(x => x.IsApprovedFor(line.ActualUnitPrice));
                if (!approved)
                    throw new ConflictException(
                        SalesErrorCodes.PriceOverrideNotApproved,
                        "تغيير السعر يحتاج موافقة Price Override فعالة.");
            }
        }

        await stock.EnsureReservationsForInvoiceAsync(invoice, ct);
        invoice.RecalculateTotals();
        await credit.ValidateAsync(customer, invoice, ct);
        invoice.Confirm(timeProvider.GetUtcNow(), currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.InvoiceAsync(invoice, ct);
    }
}
