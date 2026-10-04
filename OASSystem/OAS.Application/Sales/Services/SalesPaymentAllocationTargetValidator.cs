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
    IReadRepository<PaymentAllocation, Guid> allocations) : ISalesPaymentAllocationTargetValidator
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

        var spec = new Specification<PaymentAllocation>()
            .Where(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice &&
                        x.TargetDocumentId == salesInvoiceId);
        var existing = await allocations.ListAsync(spec, cancellationToken);
        if (excludingAllocationId.HasValue)
            existing = existing.Where(x => x.Id != excludingAllocationId.Value).ToArray();

        // Base currency is the authoritative cross-currency ceiling. This prevents an invoice
        // being over-allocated by mixing receipts in different currencies.
        var alreadyAllocatedBase = existing.Sum(GetBaseAllocatedAmount);
        var baseOutstanding = Math.Max(0m, invoice.BaseTotalAmount - alreadyAllocatedBase);
        if (baseAllocatedAmount > baseOutstanding)
            throw new ConflictException("sales_payment_allocation_exceeds_outstanding", "القيمة الأساسية للمبلغ المخصص تتجاوز الرصيد المتبقي على فاتورة المبيعات.");

        // For allocations in the same currency as the invoice, also preserve the exact
        // transaction-currency ceiling shown to the user.
        if (sourceCurrencyId == invoice.CurrencyId)
        {
            var alreadyAllocatedInInvoiceCurrency = existing
                .Where(x => x.CurrencyId == invoice.CurrencyId)
                .Sum(x => x.AllocatedAmount);
            var transactionOutstanding = Math.Max(0m, invoice.TotalAmount - alreadyAllocatedInInvoiceCurrency);
            if (allocatedAmount > transactionOutstanding)
                throw new ConflictException("sales_payment_allocation_exceeds_outstanding", "المبلغ المخصص يتجاوز الرصيد المتبقي على فاتورة المبيعات.");
        }

        return new PaymentAllocationTargetValidation(baseAllocatedAmount);
    }

    private static decimal GetBaseAllocatedAmount(PaymentAllocation allocation)
    {
        if (allocation.BaseAllocatedAmount.HasValue)
            return allocation.BaseAllocatedAmount.Value;
        if (allocation.ExchangeRate.HasValue)
            return Math.Round(allocation.AllocatedAmount * allocation.ExchangeRate.Value, 4, MidpointRounding.AwayFromZero);
        return allocation.AllocatedAmount;
    }
}
