using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Services;

/// <summary>
/// SA-02 decorator for the existing settlement workflow. It serializes invoice
/// allocation decisions, persists target-document settlement quantities and posts
/// realized FX exactly once per PaymentAllocation.
/// </summary>
public sealed class SalesSettlementFxDecorator(
    SalesSettlementService inner,
    IRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<ReceiptVoucherLine, Guid> receiptLines,
    IReadRepository<ReceiptVoucher, Guid> receiptVouchers,
    ISalesPaymentAllocationTargetValidator allocationValidator,
    ISalesPaymentAllocationConcurrencyPort concurrency,
    ICustomerSettlementFxPostingService fxPosting,
    IExchangeRateResolver exchangeRates,
    ICurrencyRoundingService rounding,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ISalesSettlementService
{
    public Task<decimal> CalculatePaymentBaseAmountAsync(
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        DateOnly documentDate,
        CancellationToken cancellationToken = default) =>
        inner.CalculatePaymentBaseAmountAsync(paymentLines, documentDate, cancellationToken);

    public Task<decimal> CalculateOrderBaseAmountAsync(
        CustomerOrder order,
        decimal amount,
        CancellationToken cancellationToken = default) =>
        inner.CalculateOrderBaseAmountAsync(order, amount, cancellationToken);

    public Task<decimal> CalculateOrderPaymentTargetBaseAmountAsync(
        CustomerOrder order,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default) =>
        inner.CalculateOrderPaymentTargetBaseAmountAsync(order, paymentLines, cancellationToken);

    public async Task<decimal> CalculateInvoicePaymentTargetBaseAmountAsync(
        SalesInvoice invoice,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (paymentLines.Count == 0)
            return 0m;

        var allocationDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        ExchangeRateResolution? targetCurrentRate = null;

        decimal totalHistoricalBase = 0m;
        foreach (var line in paymentLines)
        {
            if (line.Amount <= 0m)
                throw new ConflictException(
                    "sales_payment_amount_invalid",
                    "مبلغ سطر الدفع يجب أن يكون أكبر من صفر.");

            decimal targetDocumentAmount;
            if (line.CurrencyId == invoice.CurrencyId)
            {
                targetDocumentAmount = rounding.Round(
                    line.Amount,
                    invoice.CurrencyDecimalPlacesSnapshot);
            }
            else
            {
                var sourceRate = await exchangeRates.ResolveAsync(
                    line.CurrencyId,
                    allocationDate,
                    ExchangeRateType.Accounting,
                    cancellationToken: cancellationToken);

                targetCurrentRate ??= await exchangeRates.ResolveAsync(
                    invoice.CurrencyId,
                    allocationDate,
                    ExchangeRateType.Accounting,
                    cancellationToken: cancellationToken);
                if (targetCurrentRate.Rate <= 0m)
                    throw new ConflictException(
                        "sales_target_currency_rate_invalid",
                        "تعذر تحديد سعر صرف صالح لعملة فاتورة المبيعات.");

                var sourceBase = rounding.CalculateBaseAmount(
                    line.Amount,
                    sourceRate.Rate,
                    sourceRate.CurrencyDecimalPlaces,
                    invoice.BaseCurrencyDecimalPlacesSnapshot);

                targetDocumentAmount = rounding.Round(
                    sourceBase / targetCurrentRate.Rate,
                    invoice.CurrencyDecimalPlacesSnapshot);
            }

            totalHistoricalBase += rounding.CalculateBaseAmount(
                targetDocumentAmount,
                invoice.ExchangeRate,
                invoice.CurrencyDecimalPlacesSnapshot,
                invoice.BaseCurrencyDecimalPlacesSnapshot);
        }

        return rounding.Round(
            totalHistoricalBase,
            invoice.BaseCurrencyDecimalPlacesSnapshot);
    }

    public async Task<SalesPaymentCollectionResult> CreateReceiptForInvoiceAsync(
        SalesInvoice invoice,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        // The command handler also locks the invoice before its pre-check. This second
        // lock is deliberate defense-in-depth and is re-entrant in the same SQL transaction.
        await concurrency.LockInvoiceAsync(invoice.Id, cancellationToken);

        var result = await inner.CreateReceiptForInvoiceAsync(
            invoice,
            paymentLines,
            cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = GetCurrentUserId();

        foreach (var allocationId in result.PaymentAllocationIds)
        {
            var allocation = await allocations.GetForUpdateAsync(allocationId, cancellationToken)
                ?? throw new NotFoundException(nameof(PaymentAllocation), allocationId);

            if (allocation.ReceiptVoucherLineId is not Guid receiptLineId || receiptLineId == Guid.Empty)
                throw new ConflictException(
                    "sales_invoice_allocation_receipt_line_missing",
                    "تخصيص فاتورة المبيعات لا يحتوي سطر سند قبض صالحًا.");

            var line = await receiptLines.GetByIdAsync(receiptLineId, cancellationToken)
                ?? throw new NotFoundException(nameof(ReceiptVoucherLine), receiptLineId);
            var voucher = await receiptVouchers.GetByIdAsync(line.ReceiptVoucherId, cancellationToken)
                ?? throw new NotFoundException(nameof(ReceiptVoucher), line.ReceiptVoucherId);

            if (line.CustomerId is not Guid customerId || customerId == Guid.Empty ||
                line.CounterpartyAccountId is not Guid customerAccountId || customerAccountId == Guid.Empty ||
                allocation.CurrencyId is not Guid sourceCurrencyId || sourceCurrencyId == Guid.Empty)
            {
                throw new ConflictException(
                    "sales_invoice_fx_source_incomplete",
                    "بيانات العميل أو حساب الذمم أو عملة التحصيل غير مكتملة لإنشاء قيد فرق الصرف.");
            }

            var sourceBase = allocation.BaseAllocatedAmount
                ?? line.BaseAmount
                ?? throw new ConflictException(
                    "sales_invoice_fx_source_base_missing",
                    "القيمة الأساسية لسطر التحصيل غير متاحة.");

            var validation = await allocationValidator.ValidateAsync(
                invoice.Id,
                sourceCurrencyId,
                allocation.AllocatedAmount,
                sourceBase,
                allocation.Id,
                cancellationToken);

            allocation.UpdateAllocatedAmount(
                allocation.AllocatedAmount,
                sourceBase,
                validation.TargetBaseAllocatedAmount,
                validation.TargetDocumentAmount);
            allocations.Update(allocation);

            await fxPosting.PostAsync(
                new CustomerSettlementFxPostingRequest(
                    allocation.Id,
                    customerId,
                    customerAccountId,
                    voucher.VoucherDate,
                    sourceBase,
                    validation.TargetBaseAllocatedAmount,
                    $"فرق صرف محقق لتحصيل فاتورة مبيعات {invoice.InvoiceCode}"),
                userId,
                now,
                cancellationToken);
        }

        return result;
    }

    public Task<SalesPaymentCollectionResult> CreateAdvanceForOrderAsync(
        CustomerOrder order,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default) =>
        inner.CreateAdvanceForOrderAsync(order, paymentLines, cancellationToken);

    public Task<IReadOnlyList<Guid>> ApplyAdvancesToInvoiceAsync(
        CustomerOrder order,
        SalesInvoice invoice,
        CancellationToken cancellationToken = default) =>
        inner.ApplyAdvancesToInvoiceAsync(order, invoice, cancellationToken);

    public Task<SalesPaymentSummaryDto> GetPaymentSummaryAsync(
        CustomerOrder order,
        SalesInvoice? invoice,
        decimal paidNowAmount = 0m,
        CancellationToken cancellationToken = default) =>
        inner.GetPaymentSummaryAsync(order, invoice, paidNowAmount, cancellationToken);

    public Task<SalesSettlementReferences> GetReferencesAsync(
        CustomerOrder order,
        SalesInvoice? invoice,
        CancellationToken cancellationToken = default) =>
        inner.GetReferencesAsync(order, invoice, cancellationToken);

    private Guid GetCurrentUserId()
    {
        if (!Guid.TryParse(currentUser.UserId, out var id) || id == Guid.Empty)
            throw new ForbiddenException();
        return id;
    }
}
