using MediatR;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class CollectSalesInvoicePaymentCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ISalesInvoiceBalanceService balances,
    ISalesSettlementService settlement,
    ISalesInvoiceCollectionIdempotencyStore idempotency)
    : IRequestHandler<CollectSalesInvoicePaymentCommand, CollectSalesInvoicePaymentResultDto>
{
    private const decimal MoneyTolerance = 0.0001m;

    public async Task<CollectSalesInvoicePaymentResultDto> Handle(
        CollectSalesInvoicePaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Request.OperationId == Guid.Empty)
            throw new ConflictException("sales_invoice_collection_operation_id_required",
                "تعذر تحديد معرف التحصيل. حدّث شاشة الفاتورة قبل تنفيذ العملية.");

        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request.Request.PaymentLines))));
        // The SQL app lock remains held by TransactionBehavior until receipt + result commit.
        var previous = await idempotency.GetExistingAsync(
            request.InvoiceId, request.Request.OperationId, fingerprint, cancellationToken);
        if (previous is not null) return previous;

        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, false, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);

        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException(
                "sales_invoice_collection_requires_posted",
                "لا يمكن تحصيل دفعة على الفاتورة إلا بعد ترحيلها.");

        if (request.Request.PaymentLines.Count == 0)
            throw new ConflictException(
                "sales_invoice_collection_payment_required",
                "أدخل دفعة واحدة على الأقل لإنشاء سند القبض.");

        var current = await balances.GetAsync(invoice, cancellationToken: cancellationToken);
        if (current.OutstandingAmount <= MoneyTolerance || current.OutstandingBaseAmount <= MoneyTolerance)
            throw new ConflictException(
                "sales_invoice_already_settled",
                "فاتورة المبيعات مسددة بالكامل ولا يوجد رصيد متبقٍ للتحصيل.");

        var requestedTargetBase = await settlement.CalculateInvoicePaymentTargetBaseAmountAsync(
            invoice,
            request.Request.PaymentLines,
            cancellationToken);

        if (requestedTargetBase <= MoneyTolerance)
            throw new ConflictException(
                "sales_invoice_collection_amount_invalid",
                "مبلغ التحصيل يجب أن يكون أكبر من صفر.");

        if (requestedTargetBase > current.OutstandingBaseAmount + MoneyTolerance)
            throw new ConflictException(
                "sales_invoice_collection_exceeds_outstanding",
                "مبلغ التحصيل يتجاوز الرصيد المتبقي على فاتورة المبيعات.");

        var collection = await settlement.CreateReceiptForInvoiceAsync(
            invoice,
            request.Request.PaymentLines,
            cancellationToken);

        var updated = await balances.GetAsync(invoice, cancellationToken: cancellationToken);
        var summary = new SalesInvoicePaymentSummaryDto(
            invoice.Id,
            invoice.TotalAmount,
            updated.AllocatedAmount,
            updated.OutstandingAmount,
            updated.ReturnedAmount);

        var result = new CollectSalesInvoicePaymentResultDto(
            invoice.Id,
            collection.ReceiptVoucherId,
            collection.ReceiptVoucherNumber,
            summary);
        await idempotency.StoreAsync(request.InvoiceId, request.Request.OperationId,
            fingerprint, result, cancellationToken);
        return result;
    }
}
