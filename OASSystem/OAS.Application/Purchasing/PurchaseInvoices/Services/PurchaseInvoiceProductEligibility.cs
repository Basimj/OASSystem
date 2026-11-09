using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;

namespace OAS.Application.Purchasing.PurchaseInvoices.Services;

/// <summary>
/// Separates the rule for starting new purchases from documenting historical receipts.
/// A product deactivated after it was received may still be invoiced, but inactivity
/// does not authorize an invoice for goods that were never received and posted.
/// </summary>
public static class PurchaseInvoiceProductEligibility
{
    public static void Validate(
        PurchasingProductSnapshot? product,
        bool hasLinkedPurchaseOrderLine,
        decimal postedReceivedBaseQuantity)
    {
        if (product is null)
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["ProductVariantId"] = ["product_variant_not_found"]
            });

        if (!product.IsStockItem)
            throw new ConflictException("purchasing_product_not_stock_item",
                "لا يمكن استخدام منتج غير مخزني في دورة المشتريات الحالية.");

        if ((!product.ProductIsActive || !product.VariantIsActive) &&
            (!hasLinkedPurchaseOrderLine || postedReceivedBaseQuantity <= 0m))
            throw new ConflictException("purchasing_invoice_inactive_product_without_posted_receipt",
                "الصنف غير نشط، ولا يوجد له استلام مرحّل مرتبط ببند أمر الشراء. " +
                "لإنشاء فاتورة عن مشتريات سابقة، اختر سند استلام مرحّلًا لهذا الصنف. " +
                "أما المشتريات الجديدة فتتطلب صنفًا نشطًا.");
    }
}
