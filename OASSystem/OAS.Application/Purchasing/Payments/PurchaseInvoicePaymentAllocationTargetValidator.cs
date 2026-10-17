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
/// AP-02 separates three values:
/// 1) the source cash amount/base value at the settlement-date rate,
/// 2) the amount settled in the invoice document currency, and
/// 3) the historical AP carrying amount released at the invoice rate.
/// The difference between source base and target base is realized FX and is posted
/// separately by the supplier-settlement posting flow.
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
        if (invoice.ExchangeRate <= 0m)
            throw new ConflictException(
                "purchase_invoice_exchange_rate_invalid",
                "سعر الصرف التاريخي لفاتورة المورد غير صالح.");

        var balance = await balances.GetAsync(invoice, excludingAllocationId, cancellationToken);

        decimal targetDocumentAmount;
        if (sourceCurrencyId == invoice.CurrencyId)
        {
            // Same document currency: the paid foreign-currency quantity is the
            // invoice quantity being extinguished, regardless of today's base rate.
            targetDocumentAmount = Round(allocatedAmount);
        }
        else
        {
            // Third-currency policy: the source cash base value determines the AP
            // carrying value released. Translate that carrying value to invoice
            // currency at the invoice historical rate. This is deterministic and
            // prevents an unexplained residual in either currency.
            targetDocumentAmount = Round(sourceBaseAllocatedAmount / invoice.ExchangeRate);
        }

        if (targetDocumentAmount <= 0m || targetDocumentAmount > balance.OutstandingAmount)
            throw new ConflictException(
                "purchase_invoice_payment_allocation_exceeds_outstanding",
                "المبلغ المسوى بعملة فاتورة المورد يتجاوز الرصيد المتبقي.");

        var targetBaseAmount = Round(targetDocumentAmount * invoice.ExchangeRate);
        if (targetBaseAmount > balance.OutstandingBaseAmount)
            throw new ConflictException(
                "purchase_invoice_payment_allocation_exceeds_outstanding_base",
                "القيمة الدفترية التاريخية للمبلغ المسوى تتجاوز الرصيد الأساسي المتبقي على فاتورة المورد.");

        return new PaymentAllocationTargetValidation(targetBaseAmount, targetDocumentAmount);
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
