using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesPaymentAllocationTargetValidator(
    IReadRepository<SalesInvoice, Guid> invoices,
    ISalesInvoiceBalanceService invoiceBalances,
    IExchangeRateResolver? exchangeRates = null,
    TimeProvider? timeProvider = null) : ISalesPaymentAllocationTargetValidator
{
    private const decimal MoneyTolerance = 0.0001m;

    public AllocationTargetDocumentType TargetDocumentType =>
        AllocationTargetDocumentType.SalesInvoice;

    public async Task<PaymentAllocationTargetValidation> ValidateAsync(
        Guid salesInvoiceId,
        Guid sourceCurrencyId,
        decimal allocatedAmount,
        decimal baseAllocatedAmount,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetByIdAsync(salesInvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesInvoice), salesInvoiceId);

        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException(
                "sales_payment_allocation_invoice_not_posted",
                "لا يمكن تخصيص دفعة إلا لفاتورة مبيعات مرحلة.");

        var balance = await invoiceBalances.GetAsync(
            invoice,
            excludingAllocationId,
            cancellationToken);

        decimal targetDocumentAmount;

        if (sourceCurrencyId == invoice.CurrencyId)
        {
            // Same-currency collection extinguishes the exact invoice-currency amount.
            targetDocumentAmount = Math.Round(
                allocatedAmount,
                invoice.CurrencyDecimalPlacesSnapshot,
                MidpointRounding.AwayFromZero);
        }
        else
        {
            if (exchangeRates is null)
                throw new ConflictException(
                    "sales_cross_currency_rate_resolver_missing",
                    "خدمة أسعار الصرف مطلوبة لتخصيص دفعة بعملة مختلفة عن عملة الفاتورة.");

            var clock = timeProvider ?? TimeProvider.System;
            var allocationDate = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            var targetCurrentRate = await exchangeRates.ResolveAsync(
                invoice.CurrencyId,
                allocationDate,
                ExchangeRateType.Accounting,
                cancellationToken: cancellationToken);

            if (targetCurrentRate.Rate <= 0m)
                throw new ConflictException(
                    "sales_target_currency_rate_invalid",
                    "تعذر تحديد سعر صرف صالح لعملة فاتورة المبيعات.");

            // Third-currency policy: value source cash in base currency, translate that
            // base value to invoice currency at the allocation-date rate, then extinguish
            // historical AR using the invoice's immutable posting rate.
            targetDocumentAmount = Math.Round(
                baseAllocatedAmount / targetCurrentRate.Rate,
                invoice.CurrencyDecimalPlacesSnapshot,
                MidpointRounding.AwayFromZero);
        }

        if (targetDocumentAmount <= 0m)
            throw new ConflictException(
                "sales_payment_allocation_target_amount_invalid",
                "قيمة التسوية بعملة الفاتورة يجب أن تكون أكبر من صفر.");

        var targetBaseAllocatedAmount = Math.Round(
            targetDocumentAmount * invoice.ExchangeRate,
            invoice.BaseCurrencyDecimalPlacesSnapshot,
            MidpointRounding.AwayFromZero);

        if (targetDocumentAmount > balance.OutstandingAmount + MoneyTolerance ||
            targetBaseAllocatedAmount > balance.OutstandingBaseAmount + MoneyTolerance)
        {
            throw new ConflictException(
                "sales_payment_allocation_exceeds_outstanding",
                "المبلغ المخصص يتجاوز الرصيد المتبقي على فاتورة المبيعات بعد المرتجعات المرحلة.");
        }

        return new PaymentAllocationTargetValidation(
            targetBaseAllocatedAmount,
            targetDocumentAmount);
    }
}
