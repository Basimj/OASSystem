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

public sealed class CancelCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository repository,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<CustomerAdvance, Guid> advances,
    IReadRepository<OpticalJob, Guid> opticalJobs,
    ICustomerDemandProcurementPort customerDemand,
    ISalesStockReservationService stock,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler) : IRequestHandler<CancelCustomerOrderCommand, CustomerOrderDto>
{
    public async Task<CustomerOrderDto> Handle(CancelCustomerOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetAggregateAsync(request.OrderId, true, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.OrderId);
        SalesConcurrency.Ensure(request.Request.RowVersion, order.RowVersion, "طلب العميل");

        var linkedInvoices = await invoices.ListAsync(
            new Specification<SalesInvoice>().Where(x =>
                x.CustomerOrderId == order.Id && x.Status != SalesInvoiceStatus.Cancelled),
            ct);
        if (linkedInvoices.Count > 0)
        {
            var hasPosted = linkedInvoices.Any(x => x.Status == SalesInvoiceStatus.Posted);
            throw new ConflictException(
                hasPosted ? "sales_order_cancel_posted_invoice" : "sales_order_cancel_invoice_exists",
                hasPosted
                    ? "لا يمكن إلغاء طلب العميل لأن له فاتورة مرحلة. يجب معالجة الفاتورة ماليًا عبر المرتجع/التصحيح قبل إلغاء الطلب."
                    : "لا يمكن إلغاء طلب العميل قبل إلغاء الفاتورة المرتبطة به.");
        }

        var activeAdvances = await advances.ListAsync(
            new Specification<CustomerAdvance>().Where(x =>
                x.CustomerOrderId == order.Id && x.IsActive),
            ct);
        if (activeAdvances.Count > 0)
            throw new ConflictException(
                "sales_order_cancel_advance_exists",
                "لا يمكن إلغاء طلب العميل لوجود عربون فعال. يجب رد العربون أو تسويته أولًا.");

        var activeJobs = await opticalJobs.ListAsync(
            new Specification<OpticalJob>().Where(x =>
                x.CustomerOrderId == order.Id && x.IsActive),
            ct);
        if (activeJobs.Count > 0)
            throw new ConflictException(
                "sales_order_cancel_optical_job_exists",
                "لا يمكن إلغاء طلب العميل لوجود أمر معمل فعال. أغلق أو ألغِ أمر المعمل أولًا.");

        // Cancel only shortage requests that have not been committed to a purchase order.
        // The procurement port blocks cancellation if any customer demand is already committed.
        await customerDemand.CancelUncommittedDemandForOrderAsync(
            order.Id,
            "إلغاء طلب العميل",
            ct);

        var now = timeProvider.GetUtcNow();
        await stock.ReleaseOrderReservationsAsync(order.Id, now, ct);
        order.Cancel(now, currentUser.UserId);
        repository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.OrderAsync(order, ct);
    }
}
