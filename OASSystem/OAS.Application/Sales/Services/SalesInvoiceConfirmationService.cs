using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesInvoiceConfirmationService(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<SalesPriceOverride, Guid> overrides,
    IRepository<SalesInvoiceLinePrescriptionSnapshot, Guid> snapshots,
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator,
    ISalesStockReservationService stock,
    ISalesCreditExposureService credit,
    IPermissionChecker permissions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ISalesInvoiceConfirmationService
{
    public async Task ConfirmAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new ConflictException(SalesErrorCodes.InvoiceInvalidStatus, "يمكن تأكيد الفاتورة من حالة المسودة فقط.");

        var customer = await customers.GetByIdAsync(invoice.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        if (invoice.PaymentPlan == SalesPaymentPlan.AccountCredit &&
            !await permissions.HasPermissionAsync(SalesPermissions.Credit.Use, cancellationToken))
            throw new ForbiddenException("ليس لديك صلاحية استخدام البيع الآجل.", SalesPermissions.Credit.Use);

        var hasDiscount = invoice.Lines.Any(x => x.IsActive && x.DiscountAmount > 0);
        if (hasDiscount && !await permissions.HasPermissionAsync(SalesPermissions.Discount, cancellationToken))
            throw new ForbiddenException("لا توجد صلاحية لتطبيق الخصم.", SalesPermissions.Discount);

        foreach (var line in invoice.Lines.Where(x => x.IsActive))
        {
            var resolved = await lineResolver.ResolveAsync(
                line.LineType, line.ProductVariantId, line.WarehouseId, line.DescriptionSnapshot, cancellationToken);

            // Invoices created from CustomerOrder copy the immutable order optical snapshot.
            // Direct invoices retain the legacy prescription-based path.
            if (line.PrescriptionSnapshot is null)
            {
                var detail = await prescriptionValidator.ValidateLineAsync(
                    line.PrescriptionRevisionId,
                    line.PrescriptionEye,
                    resolved.PrescriptionRequired,
                    resolved.OpticalPolicy,
                    cancellationToken);

                if (line.LineType == SalesLineType.Lens && detail is not null)
                {
                    var snapshot = SalesInvoiceLinePrescriptionSnapshot.Create(
                        Guid.NewGuid(), line.Id, detail.PrescriptionRevisionId, detail.Eye,
                        detail.SPH, detail.CYL, detail.Axis, detail.ADD, detail.Prism, detail.PrismBase,
                        detail.PD, detail.MonocularPD, detail.VA, detail.FittingHeight);
                    invoice.SetLinePrescriptionSnapshot(line.Id, snapshot);
                    await snapshots.AddAsync(snapshot, cancellationToken);
                }
            }
            else if (resolved.OpticalPolicy is not null)
            {
                try
                {
                    line.PrescriptionSnapshot.ValidateAgainst(resolved.OpticalPolicy);
                }
                catch (DomainException)
                {
                    throw new ConflictException(
                        SalesErrorCodes.PrescriptionOutsideLensRange,
                        "قياسات الوصفة المحفوظة في الفاتورة لم تعد ضمن نطاق العدسة المحددة. راجع نطاق العدسة أو اختر SKU متوافقًا قبل التأكيد.");
                }
            }

            if (line.ActualUnitPrice != line.BaseUnitPrice)
            {
                var spec = new Specification<SalesPriceOverride>()
                    .Where(x => x.SalesInvoiceLineId == line.Id &&
                                x.Status == SalesPriceOverrideStatus.Approved && x.IsActive);
                var approved = (await overrides.ListAsync(spec, cancellationToken))
                    .Any(x => x.IsApprovedFor(line.ActualUnitPrice));
                if (!approved)
                    throw new ConflictException(
                        SalesErrorCodes.PriceOverrideNotApproved,
                        "تغيير السعر يحتاج موافقة Price Override فعالة.");
            }
        }

        await stock.EnsureReservationsForInvoiceAsync(invoice, cancellationToken);
        invoice.RecalculateTotals();
        await credit.ValidateAsync(customer, invoice, cancellationToken);
        invoice.Confirm(timeProvider.GetUtcNow(), currentUser.UserId);
    }
}
