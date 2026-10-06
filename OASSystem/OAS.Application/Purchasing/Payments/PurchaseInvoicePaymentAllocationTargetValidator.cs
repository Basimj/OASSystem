using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.Payments;

/// <summary>
/// Protects supplier-invoice allocations from overpayment and from being funded by
/// a payment source that belongs to another party (or by a receipt voucher).
/// </summary>
public sealed class PurchaseInvoicePaymentAllocationTargetValidator(
    IReadRepository<PurchaseInvoice, Guid> invoices,
    IReadRepository<PaymentAllocation, Guid> allocations) :
    IPaymentAllocationTargetValidator,
    IPaymentAllocationSourceTargetValidator
{
    public AllocationTargetDocumentType TargetDocumentType => AllocationTargetDocumentType.PurchaseInvoice;

    public async Task<PaymentAllocationTargetValidation> ValidateAsync(
        Guid targetDocumentId,
        Guid sourceCurrencyId,
        decimal allocatedAmount,
        decimal sourceBaseAllocatedAmount,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var invoice = await GetPostedInvoiceAsync(targetDocumentId, cancellationToken);

        if (allocatedAmount <= 0m || sourceBaseAllocatedAmount <= 0m)
            throw new ConflictException(
                "purchase_invoice_payment_amount_invalid",
                "يجب أن تكون قيمة التخصيص لفاتورة المورد أكبر من صفر.");

        var existing = await allocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice &&
                x.TargetDocumentId == targetDocumentId),
            cancellationToken);

        if (excludingAllocationId.HasValue)
            existing = existing.Where(x => x.Id != excludingAllocationId.Value).ToArray();

        // Base currency is the authoritative ceiling when allocations use different
        // settlement currencies.  This mirrors the accounting allocation convention
        // already used for sales invoices.
        var alreadyAllocatedBase = existing.Sum(GetTargetBaseAllocatedAmount);
        var baseOutstanding = Math.Max(0m, invoice.BaseTotalAmount - alreadyAllocatedBase);
        if (sourceBaseAllocatedAmount > baseOutstanding)
            throw new ConflictException(
                "purchase_invoice_payment_allocation_exceeds_outstanding",
                "القيمة الأساسية للمبلغ المخصص تتجاوز الرصيد المتبقي على فاتورة المورد.");

        // When payment and invoice use the same currency, also preserve the exact
        // document-currency ceiling shown to the user.
        if (sourceCurrencyId == invoice.CurrencyId)
        {
            var alreadyAllocatedInInvoiceCurrency = existing
                .Where(x => x.CurrencyId == invoice.CurrencyId)
                .Sum(x => x.AllocatedAmount);
            var transactionOutstanding = Math.Max(0m, invoice.TotalAmount - alreadyAllocatedInInvoiceCurrency);
            if (allocatedAmount > transactionOutstanding)
                throw new ConflictException(
                    "purchase_invoice_payment_allocation_exceeds_outstanding",
                    "المبلغ المخصص يتجاوز الرصيد المتبقي على فاتورة المورد.");
        }

        return new PaymentAllocationTargetValidation(sourceBaseAllocatedAmount);
    }

    public async Task ValidateSourceAsync(
        Guid targetDocumentId,
        PaymentAllocationSourceContext source,
        CancellationToken cancellationToken = default)
    {
        var invoice = await GetPostedInvoiceAsync(targetDocumentId, cancellationToken);

        if (source.SourceType != PaymentSourceType.PaymentVoucher)
            throw new ConflictException(
                "purchase_invoice_payment_source_invalid",
                "يمكن تخصيص سداد فاتورة المورد من سند صرف مرحل فقط.");

        if (source.PartyType != SettlementPartyType.Supplier || !source.SupplierId.HasValue)
            throw new ConflictException(
                "purchase_invoice_payment_supplier_required",
                "يجب أن يكون سطر سند الصرف مرتبطًا بمورد قبل تخصيصه على فاتورة مشتريات.");

        if (source.SupplierId.Value != invoice.SupplierId)
            throw new ConflictException(
                "purchase_invoice_payment_supplier_mismatch",
                "المورد في سند الصرف لا يطابق المورد في فاتورة المشتريات.");
    }

    private async Task<PurchaseInvoice> GetPostedInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), id);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
            throw new ConflictException(
                "purchase_invoice_payment_invoice_not_posted",
                "لا يمكن تخصيص دفعة إلا لفاتورة مورد مرحلة.");

        return invoice;
    }

    private static decimal GetTargetBaseAllocatedAmount(PaymentAllocation allocation)
    {
        if (allocation.TargetBaseAllocatedAmount.HasValue)
            return allocation.TargetBaseAllocatedAmount.Value;
        if (allocation.BaseAllocatedAmount.HasValue)
            return allocation.BaseAllocatedAmount.Value;
        if (allocation.ExchangeRate.HasValue)
            return Math.Round(allocation.AllocatedAmount * allocation.ExchangeRate.Value, 4, MidpointRounding.AwayFromZero);
        return allocation.AllocatedAmount;
    }
}
