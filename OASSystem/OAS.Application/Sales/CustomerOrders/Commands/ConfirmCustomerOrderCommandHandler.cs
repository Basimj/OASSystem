using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.Application.Sales.CustomerOrders.Commands;

/// <summary>
/// Compatibility endpoint only. Customer-order confirmation is owned by Sales Checkout so that
/// availability, reservation, shortage procurement, payment-plan and idempotency rules execute atomically.
/// Existing orders that were confirmed by older versions are still supported by the Checkout orchestrator.
/// </summary>
public sealed class ConfirmCustomerOrderCommandHandler
    : IRequestHandler<ConfirmCustomerOrderCommand, CustomerOrderDto>
{
    public Task<CustomerOrderDto> Handle(ConfirmCustomerOrderCommand request, CancellationToken ct) =>
        throw new ConflictException(
            "sales_order_confirmation_requires_checkout",
            "تم نقل تأكيد طلب العميل إلى بيع جديد / Checkout. افتح الطلب من شاشة إتمام البيع لتنفيذ الحجز والتوريد والفوترة والتحصيل بصورة صحيحة.");
}
