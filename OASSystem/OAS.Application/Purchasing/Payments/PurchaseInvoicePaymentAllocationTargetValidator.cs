using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.Payments;

/// <summary>
/// Protects supplier-invoice allocations from overpayment and from being funded by
/// a payment source that belongs to another party (or by a receipt voucher).
///
/// AP-02 separates the cash-flow base amount from the historical base carrying
/// amount of the supplier liability. When the payment is made in the invoice
/// currency, the document amount is settled in that currency and the target base
/// amount is calculated using the invoice's historical exchange rate. Any
/// difference between source base and target base is a realized FX difference and
/// is posted separately by the settlement posting flow.
/// </summary>
public sealed class PurchaseInvoicePaymentAllocationTargetValidator(
    IReadRepository<PurchaseInvoice, Guid> invoices,
    IPurchaseInvoiceBalanceService balances) :
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

        var balance = await balances.GetAsync(invoice, excludingAllocationId, cancellationToken);

        // If the voucher line uses the same currency as the invoice, the original
        // document amount is the settlement quantity. The AP carrying amount must
        // therefore be released at the invoice historical rate, not at the payment
        // day's rate. Example: Invoice USD 100 @ 1500 and payment USD 100 @ 1600:
        // source cash base = 160,000, target AP base = 150,000, realized FX = 10,000.
        if (sourceCurrencyId == invoice.CurrencyId)
        {
            if (invoice.ExchangeRate <= 0m)
                throw new ConflictException(
                    "purchase_invoice_exchange_rate_invalid",
                    "سعر الصرف التاريخي لفاتورة المورد غير صالح.");

            if (allocatedAmount > balance.OutstandingAmount)
                throw new ConflictException(
                    "purchase_invoice_payment_allocation_exceeds_outstanding",
                    "المبلغ المخصص يتجاوز الرصيد المتبقي على فاتورة المورد بعد المرتجعات والائتمانات.");

            var targetBaseAmount = Round(allocatedAmount * invoice.ExchangeRate);
            if (targetBaseAmount > balance.OutstandingBaseAmount)
                throw new ConflictException(
                    "purchase_invoice_payment_allocation_exceeds_outstanding_base",
                    "القيمة الدفترية التاريخية للمبلغ المخصص تتجاوز الرصيد الأساسي المتبقي على فاتورة المورد.");

            return new PaymentAllocationTargetValidation(targetBaseAmount);
        }

        // Third-currency settlement is intentionally kept on the existing base-value
        // rule in this step. The next AP-02 slice adds explicit target-document amount
        // capture and FX posting for cross-currency allocations.
        if (sourceBaseAllocatedAmount > balance.OutstandingBaseAmount)
            throw new ConflictException(
                "purchase_invoice_payment_allocation_exceeds_outstanding",
                "القيمة الأساسية للمبلغ المخصص تتجاوز الرصيد المتبقي على فاتورة المورد بعد المرتجعات والائتمانات.");

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

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
