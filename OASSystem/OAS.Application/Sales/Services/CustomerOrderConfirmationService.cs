using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Checkout;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class CustomerOrderConfirmationService(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<CustomerOrderLineOpticalSnapshot, Guid> snapshots,
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator,
    ISalesStockReservationService stock,
    ICustomerOrderAvailabilityService availability,
    ICustomerDemandProcurementPort procurement,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICustomerOrderConfirmationService
{
    public async Task<CustomerOrderConfirmationResult> ConfirmAsync(
        CustomerOrder order,
        IReadOnlyList<CheckoutSupplierScheduleRequest>? supplierSchedulingDecisions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Status != CustomerOrderStatus.Draft)
            throw new ConflictException("sales_order_invalid_status", "يمكن تأكيد طلب العميل من حالة المسودة فقط.");
        if (order.Lines.Count == 0 || order.Lines.All(x => !x.IsActive))
            throw new ConflictException("sales_order_lines_required", "يجب أن يحتوي الطلب على سطر واحد على الأقل.");

        var customer = await customers.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), order.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");
        if (order.PaymentPlan == SalesPaymentPlan.AccountCredit &&
            (string.IsNullOrWhiteSpace(customer.CustomerCode) || !customer.IsCreditAllowed))
            throw new ConflictException(SalesErrorCodes.CreditNotAllowed, "العميل غير مسموح له بالبيع الآجل.");

        foreach (var line in order.Lines.Where(x => x.IsActive))
        {
            if (line.ProductVariantId.HasValue && line.ActualUnitPrice <= 0m)
                throw new ConflictException(
                    "sales_product_price_required",
                    $"لم يتم تحديد سعر بيع للبند رقم {line.LineNumber}. أدخل سعر البيع قبل إتمام العملية.");

            var resolved = await lineResolver.ResolveAsync(
                line.LineType, line.ProductVariantId, line.WarehouseId, line.DescriptionSnapshot, cancellationToken);
            var snapshot = (await snapshots.ListAsync(
                new Specification<CustomerOrderLineOpticalSnapshot>()
                    .Where(x => x.CustomerOrderLineId == line.Id && x.IsActive), cancellationToken))
                .SingleOrDefault();

            line.EnsureOpticalMeasurementReference(resolved.PrescriptionRequired, snapshot is not null);
            if (snapshot is not null)
            {
                if (resolved.OpticalPolicy is not null)
                    snapshot.ValidateAgainst(resolved.OpticalPolicy);
            }
            else
            {
                await prescriptionValidator.ValidateLineAsync(
                    line.PrescriptionRevisionId,
                    line.PrescriptionEye,
                    resolved.PrescriptionRequired,
                    resolved.OpticalPolicy,
                    cancellationToken);
            }
        }

        // Availability is assessed before new reservations are written. This preserves the
        // shortage amount that must become CustomerDemand in the same transaction.
        var assessment = await availability.AssessAsync(order, cancellationToken);
        var resultingStatus = await stock.ReserveForOrderAsync(order, cancellationToken);
        order.Confirm(resultingStatus, timeProvider.GetUtcNow(), currentUser.UserId);

        var decisions = (supplierSchedulingDecisions ?? [])
            .GroupBy(x => x.CustomerOrderLineId)
            .ToDictionary(x => x.Key, x => x.Last());

        foreach (var shortage in assessment.Lines.Where(x => x.ShortageQuantity > 0m))
        {
            decisions.TryGetValue(shortage.CustomerOrderLineId, out var decision);
            await procurement.CreateOrUpdateShortageAsync(
                new CustomerDemandShortage(
                    order.Id,
                    shortage.CustomerOrderLineId,
                    shortage.WarehouseId,
                    shortage.ProductVariantId,
                    shortage.ShortageQuantity,
                    order.OrderDate,
                    decision?.RequiredDate ?? order.RequiredDate,
                    decision?.PreferredSupplierId,
                    decision?.ScheduledOrderAtUtc,
                    $"Customer order {order.OrderCode}"),
                cancellationToken);
        }

        var demand = await procurement.GetOpenDemandForOrderAsync(order.Id, cancellationToken);
        return new CustomerOrderConfirmationResult(assessment, order.Status, demand);
    }
}
