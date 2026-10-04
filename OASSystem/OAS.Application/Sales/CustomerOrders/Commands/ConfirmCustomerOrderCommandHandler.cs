using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed class ConfirmCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository repository,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<CustomerOrderLineOpticalSnapshot, Guid> snapshots,
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator,
    ISalesStockReservationService stock,
    ICustomerOrderAvailabilityService availability,
    ICustomerDemandProcurementPort procurement,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler) : IRequestHandler<ConfirmCustomerOrderCommand, CustomerOrderDto>
{
    public async Task<CustomerOrderDto> Handle(ConfirmCustomerOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetAggregateAsync(request.OrderId, true, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.OrderId);
        SalesConcurrency.Ensure(request.Request.RowVersion, order.RowVersion, "طلب العميل");

        if (order.Status != CustomerOrderStatus.Draft)
            throw new ConflictException("sales_order_invalid_status", "يمكن تأكيد طلب العميل من حالة المسودة فقط.");
        if (order.Lines.Count == 0 || order.Lines.All(x => !x.IsActive))
            throw new ConflictException("sales_order_lines_required", "يجب أن يحتوي الطلب على سطر واحد على الأقل.");

        var customer = await customers.GetByIdAsync(order.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), order.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");
        if (order.PaymentTermType == SalesPaymentTermType.Credit && !customer.IsCreditAllowed)
            throw new ConflictException(SalesErrorCodes.CreditNotAllowed, "البيع الآجل غير مسموح لهذا العميل.");

        foreach (var line in order.Lines.Where(x => x.IsActive))
        {
            var resolved = await lineResolver.ResolveAsync(
                line.LineType, line.ProductVariantId, line.WarehouseId, line.DescriptionSnapshot, ct);
            var snapshot = (await snapshots.ListAsync(
                new Specification<CustomerOrderLineOpticalSnapshot>().Where(x => x.CustomerOrderLineId == line.Id && x.IsActive), ct))
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
                    ct);
            }
        }

        // Assess shortage before creating new reservations. InventoryBalance.AvailableQuantity
        // excludes reservations, while newly-added reservation rows are not guaranteed to be
        // visible to a database query until SaveChanges. Assessing first keeps shortage demand
        // deterministic and prevents creating a PurchaseRequest for stock that was just reserved.
        var assessment = await availability.AssessAsync(order, ct);
        var status = await stock.ReserveForOrderAsync(order, ct);
        order.Confirm(status, timeProvider.GetUtcNow(), currentUser.UserId);

        foreach (var shortage in assessment.Lines.Where(x => x.ShortageQuantity > 0m))
        {
            await procurement.CreateOrUpdateShortageAsync(
                new CustomerDemandShortage(
                    order.Id,
                    shortage.CustomerOrderLineId,
                    shortage.WarehouseId,
                    shortage.ProductVariantId,
                    shortage.ShortageQuantity,
                    order.OrderDate,
                    order.RequiredDate,
                    Notes: $"Customer order {order.OrderCode}"),
                ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.OrderAsync(order, ct);
    }
}
