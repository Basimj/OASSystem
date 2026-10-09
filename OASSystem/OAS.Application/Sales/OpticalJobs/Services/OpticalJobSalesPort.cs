using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.OpticalJobs.Services;

public sealed class OpticalJobSalesPort(
    ICustomerOrderAggregateRepository orders,
    IReadRepository<SalesInvoice, Guid> invoices,
    ISalesInvoiceBalanceService balances) : IOpticalJobSalesPort
{
    private const decimal MoneyTolerance = 0.005m;

    public async Task MarkReadyForDeliveryAsync(OpticalJob job, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetAggregateAsync(job.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), job.CustomerOrderId);
        if (order.Status == CustomerOrderStatus.InProduction)
        {
            order.MarkReadyForDelivery();
            orders.Update(order);
        }
    }

    public async Task EnsureDeliveryEligibleAsync(OpticalJob job, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetAggregateAsync(job.CustomerOrderId, false, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), job.CustomerOrderId);
        if (order.Status != CustomerOrderStatus.ReadyForDelivery && order.Status != CustomerOrderStatus.Completed)
            throw new ConflictException("optical_delivery_order_not_ready", "طلب العميل ليس جاهزًا للتسليم.");
        if (!job.SalesInvoiceId.HasValue)
            throw new ConflictException("optical_delivery_invoice_required", "لا توجد فاتورة مرتبطة بأمر المعمل.");
        var invoice = await invoices.GetByIdAsync(job.SalesInvoiceId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesInvoice), job.SalesInvoiceId.Value);
        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException("optical_delivery_invoice_not_posted", "يجب ترحيل فاتورة المبيعات قبل التسليم.");
        var balance = await balances.GetAsync(invoice, null, cancellationToken);
        if (order.PaymentPlan != SalesPaymentPlan.AccountCredit && balance.OutstandingAmount > MoneyTolerance)
            throw new ConflictException("optical_delivery_payment_required", "يجب تسوية المبلغ المتبقي قبل تسليم الطلب.");
    }

    public async Task MarkDeliveredAsync(OpticalJob job, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetAggregateAsync(job.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), job.CustomerOrderId);
        if (order.Status == CustomerOrderStatus.ReadyForDelivery)
        {
            order.Complete();
            orders.Update(order);
        }
    }
}
