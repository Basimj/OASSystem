using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.Application.Sales.SalesInvoices.Commands;

/// <summary>
/// Compatibility endpoint only. Invoices linked to Customer Orders are created exclusively by
/// Sales Checkout / Customer Order Fulfillment to preserve availability, shortage, payment and idempotency rules.
/// </summary>
public sealed class CreateSalesInvoiceFromOrderCommandHandler
    : IRequestHandler<CreateSalesInvoiceFromOrderCommand, SalesInvoiceDto>
{
    public Task<SalesInvoiceDto> Handle(CreateSalesInvoiceFromOrderCommand request, CancellationToken ct) =>
        throw new ConflictException(
            "sales_invoice_from_order_requires_checkout",
            "لم يعد إنشاء فاتورة طلب العميل يدويًا مسموحًا. استخدم بيع جديد / Checkout؛ وسيتم إنشاء الفاتورة تلقائيًا عندما تسمح حالة الطلب والمخزون بذلك.");
}
