using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.PurchaseInvoices.Services;

/// <summary>
/// Explains the same saved-allocation prerequisites enforced during invoice posting.
/// Does not relax any accounting or matching rules.
/// </summary>
public static class PurchaseInvoiceMatchingReadiness
{
    public static PurchaseInvoiceMatchingReadinessDto Evaluate(
        PurchaseInvoiceStatus status,
        IReadOnlyList<PurchaseInvoiceReceiptAllocationDto> savedAllocations)
    {
        var statuses = savedAllocations.Select(x => (PurchaseMatchStatus)(byte)x.MatchStatus).ToArray();
        var unresolved = statuses.Count(x => x is PurchaseMatchStatus.Pending
            or PurchaseMatchStatus.RequiresApproval or PurchaseMatchStatus.Rejected);

        if (status == PurchaseInvoiceStatus.PendingMatchApproval)
            return new(false, "purchasing_invoice_variance_approval_required",
                "توجد فروقات مطابقة تحتاج إلى اعتماد. راجع الفروقات واضغط «اعتماد الفروقات» ثم أعد الترحيل.",
                statuses.Length, unresolved, savedAllocations);

        if (statuses.Length == 0)
            return new(false, "purchasing_invoice_matching_required",
                "لم تتم مطابقة فاتورة المورد مع استلامات مشتريات مرحّلة. اضغط «تشغيل المطابقة»، ثم راجع النتيجة قبل الترحيل.",
                0, 0, savedAllocations);

        if (unresolved > 0)
        {
            var rejected = statuses.Any(x => x == PurchaseMatchStatus.Rejected);
            var message = rejected
                ? "توجد فروقات مطابقة مرفوضة. راجع بنود الفاتورة والاستلامات، ثم شغّل المطابقة من جديد."
                : statuses.Any(x => x == PurchaseMatchStatus.RequiresApproval)
                    ? "توجد فروقات تحتاج إلى اعتماد. راجعها واضغط «اعتماد الفروقات» قبل ترحيل الفاتورة."
                    : "لا تزال المطابقة غير مكتملة. اضغط «تشغيل المطابقة» وراجع البنود قبل الترحيل.";
            return new(false, "purchasing_invoice_matching_incomplete", message, statuses.Length, unresolved, savedAllocations);
        }

        return new(true, "purchasing_invoice_matching_complete",
            "المطابقة مكتملة. يمكن متابعة الترحيل إذا تحققت بقية الشروط المحاسبية.", statuses.Length, 0, savedAllocations);
    }
}
