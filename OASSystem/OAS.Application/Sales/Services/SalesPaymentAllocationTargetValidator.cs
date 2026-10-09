using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Accounting.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesPaymentAllocationTargetValidator(
    IReadRepository<SalesInvoice, Guid> invoices,
    ISalesInvoiceBalanceService invoiceBalances) : ISalesPaymentAllocationTargetValidator
{
    public AllocationTargetDocumentType TargetDocumentType => AllocationTargetDocumentType.SalesInvoice;

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
            throw new ConflictException("sales_payment_allocation_invoice_not_posted", "لا يمكن تخصيص دفعة إلا لفاتورة مبيعات مرحلة.");

        var balance = await invoiceBalances.GetAsync(invoice, excludingAllocationId, cancellationToken);

        // TargetBaseAllocatedAmount represents how much of the invoice's historical base
        // balance is settled. When collecting in the invoice currency, use the invoice's
        // immutable exchange-rate snapshot rather than today's collection rate. This keeps
        // a 300-unit payment equal to exactly 300 invoice-currency units even if FX changed.
        var targetBaseAllocatedAmount = sourceCurrencyId == invoice.CurrencyId
            ? Math.Round(
                allocatedAmount * invoice.ExchangeRate,
                invoice.BaseCurrencyDecimalPlacesSnapshot,
                MidpointRounding.AwayFromZero)
            : baseAllocatedAmount;

        if (targetBaseAllocatedAmount > balance.OutstandingBaseAmount)
            throw new ConflictException("sales_payment_allocation_exceeds_outstanding", "القيمة الأساسية للمبلغ المخصص تتجاوز الرصيد المتبقي على فاتورة المبيعات بعد المرتجعات المرحلة.");

        if (sourceCurrencyId == invoice.CurrencyId && allocatedAmount > balance.OutstandingAmount)
            throw new ConflictException("sales_payment_allocation_exceeds_outstanding", "المبلغ المخصص يتجاوز الرصيد المتبقي على فاتورة المبيعات بعد المرتجعات المرحلة.");

        return new PaymentAllocationTargetValidation(targetBaseAllocatedAmount);
    }

}
