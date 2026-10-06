using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;
using DomainPlan = OAS.Domain.Sales.Enums.SalesPaymentPlan;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class PostSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ISalesInvoicePostingWorkflow posting,
    ISalesSettlementService settlement)
    : IRequestHandler<PostSalesInvoiceCommand, SalesInvoicePostingResultDto>
{
    public async Task<SalesInvoicePostingResultDto> Handle(PostSalesInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        SalesConcurrency.Ensure(request.Request.RowVersion, invoice.RowVersion, "فاتورة المبيعات");

        var result = await posting.PostAsync(invoice, ct);

        SalesPaymentCollectionResult? collection = null;
        // Compatibility for the old standalone Post Invoice flow. The new Checkout flow calls the
        // posting workflow directly and controls payment according to PaymentPlan.
        if (invoice.PaymentPlan == DomainPlan.FullNow)
        {
            var legacyMethod = request.Request.ImmediatePaymentMethod
                ?? throw new ConflictException("sales_immediate_payment_method_required", "يجب تحديد طريقة التحصيل للفواتير ذات خطة الدفع الكامل الآن.");
            var method = legacyMethod == SalesImmediatePaymentMethod.Cash
                ? PaymentMethod.Cash
                : PaymentMethod.BankTransfer;
            collection = await settlement.CreateReceiptForInvoiceAsync(
                invoice,
                [new CheckoutPaymentLineRequest(
                    method,
                    invoice.CurrencyId,
                    invoice.TotalAmount,
                    request.Request.CashAccountId,
                    request.Request.BankAccountId,
                    null,
                    invoice.InvoiceDate,
                    $"تحصيل كامل للفاتورة {invoice.InvoiceCode}")],
                ct);
        }

        return new SalesInvoicePostingResultDto(
            invoice.Id,
            invoice.InvoiceCode,
            result.JournalEntryId,
            result.InventoryTransactionIds,
            Convert.ToBase64String(invoice.RowVersion),
            collection?.ReceiptVoucherId,
            collection?.ReceiptVoucherNumber);
    }
}
