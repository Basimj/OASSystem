using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;
using DomainSalesInvoiceStatus = OAS.Domain.Sales.Enums.SalesInvoiceStatus;
using DomainCustomerOrderStatus = OAS.Domain.Sales.Enums.CustomerOrderStatus;
using DomainPlan = OAS.Domain.Sales.Enums.SalesPaymentPlan;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class PostSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ISalesInvoicePostingWorkflow posting,
    ISalesSettlementService settlement,
    ICustomerOrderAggregateRepository orders,
    IOpticalProductionPort opticalProduction,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PostSalesInvoiceCommand, SalesInvoicePostingResultDto>
{
    private const decimal MoneyTolerance = 0.0001m;

    public async Task<SalesInvoicePostingResultDto> Handle(PostSalesInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        SalesConcurrency.Ensure(request.Request.RowVersion, invoice.RowVersion, "فاتورة المبيعات");
        // Guard BEFORE computing/creating payment: PostAsync is idempotent for inventory
        // but this handler must not create a second receipt against an already posted invoice.
        if (invoice.Status == DomainSalesInvoiceStatus.Posted)
            throw new ConflictException("sales_invoice_already_posted_use_collection",
                "الفاتورة مرحلة بالفعل. راجع سند القبض الأصلي، واستخدم تحصيل دفعة فقط للمبلغ المتبقي.");

        var paymentLines = BuildEffectivePaymentLines(invoice, request.Request);
        var paymentBaseAmount = await settlement.CalculateInvoicePaymentTargetBaseAmountAsync(invoice, paymentLines, ct);
        ValidatePaymentPlan(invoice.PaymentPlan, paymentLines.Count, paymentBaseAmount, invoice.BaseTotalAmount);

        var result = await posting.PostAsync(invoice, ct);

        SalesPaymentCollectionResult? collection = null;
        if (paymentLines.Count > 0)
            collection = await settlement.CreateReceiptForInvoiceAsync(invoice, paymentLines, ct);

        if (invoice.CustomerOrderId.HasValue)
        {
            var order = await orders.GetAggregateAsync(invoice.CustomerOrderId.Value, true, ct)
                ?? throw new NotFoundException(nameof(CustomerOrder), invoice.CustomerOrderId.Value);
            if (order.RequiresProduction)
                await opticalProduction.EnsureJobAsync(order, invoice.Id, ct);
            else if (order.Status == DomainCustomerOrderStatus.Confirmed)
            {
                order.MarkReadyForDelivery();
                orders.Update(order);
            }
            await unitOfWork.SaveChangesAsync(ct);
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

    private static IReadOnlyList<CheckoutPaymentLineRequest> BuildEffectivePaymentLines(
        SalesInvoice invoice,
        PostSalesInvoiceRequest request)
    {
        if (request.PaymentLines.Count > 0)
            return request.PaymentLines;

        // Backward compatibility for an older standalone invoice client that only sent Cash/Bank.
        if (invoice.PaymentPlan != DomainPlan.FullNow || !request.ImmediatePaymentMethod.HasValue)
            return [];

        var method = request.ImmediatePaymentMethod == SalesImmediatePaymentMethod.Cash
            ? PaymentMethod.Cash
            : PaymentMethod.BankTransfer;
        return
        [
            new CheckoutPaymentLineRequest(
                method, invoice.CurrencyId, invoice.TotalAmount, request.CashAccountId, request.BankAccountId,
                null, invoice.InvoiceDate, $"تحصيل كامل للفاتورة {invoice.InvoiceCode}")
        ];
    }

    private static void ValidatePaymentPlan(
        DomainPlan plan,
        int paymentLineCount,
        decimal paymentBaseAmount,
        decimal requiredBaseAmount)
    {
        switch (plan)
        {
            case DomainPlan.FullNow:
                if (paymentLineCount == 0 || Math.Abs(paymentBaseAmount - requiredBaseAmount) > MoneyTolerance)
                    throw new ConflictException("sales_direct_full_payment_required", "خطة الدفع الكامل تتطلب دفعات تغطي كامل قيمة الفاتورة.");
                break;
            case DomainPlan.PartialNow:
                if (paymentLineCount == 0 || paymentBaseAmount <= MoneyTolerance || paymentBaseAmount >= requiredBaseAmount - MoneyTolerance)
                    throw new ConflictException("sales_direct_partial_payment_invalid", "الدفع الجزئي يجب أن يكون أكبر من صفر وأقل من كامل قيمة الفاتورة.");
                break;
            case DomainPlan.AccountCredit:
                if (paymentLineCount != 0)
                    throw new ConflictException("sales_direct_credit_payment_not_allowed", "البيع الآجل لا يقبل دفعات عند الترحيل الأولي في الإصدار الحالي.");
                break;
            default:
                throw new ConflictException("sales_direct_payment_plan_invalid", "خطة السداد غير صالحة.");
        }
    }
}
